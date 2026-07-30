namespace BedrockBoot.Models.Pack.Pe;

public class PeFile
{
    readonly byte[] _data;
    readonly List<(uint va, uint vsz, uint raw, uint rsz)> _secs = new();

    public PeFile(string path)
    {
        _data = File.ReadAllBytes(path);
        if (_data[0] != 'M' || _data[1] != 'Z') throw new InvalidOperationException("Not a PE");
        var peOff = BitConverter.ToInt32(_data, 0x3C);
        if (_data[peOff] != 'P' || _data[peOff + 1] != 'E') throw new InvalidOperationException("Bad PE signature");
        var coff = peOff + 4;
        var nsec = BitConverter.ToUInt16(_data, coff + 2);
        var opt = coff + 20;
        if (BitConverter.ToUInt16(_data, opt) != 0x20B) throw new InvalidOperationException("Not PE32+");
        var sect = opt + BitConverter.ToUInt16(_data, coff + 16);
        for (int i = 0; i < nsec; i++)
        {
            var b = sect + i * 40;
            _secs.Add((
                BitConverter.ToUInt32(_data, b + 12),
                BitConverter.ToUInt32(_data, b + 8),
                BitConverter.ToUInt32(_data, b + 20),
                BitConverter.ToUInt32(_data, b + 16)
            ));
        }
    }

    public int? RvaToOffset(uint rva)
    {
        foreach (var (va, vsz, raw, rsz) in _secs)
            if (va <= rva && rva < va + Math.Max(vsz, rsz))
                return (int)(raw + (rva - va));
        return null;
    }

    public int? ExportRva(string name)
    {
        var eo = RvaToOffset(BitConverter.ToUInt32(_data, 0x18 + 0x70)); // From optional header
        // Re-read properly
        var peOff = BitConverter.ToInt32(_data, 0x3C);
        var opt = peOff + 4 + 20;
        var expRva = BitConverter.ToUInt32(_data, opt + 112);
        eo = RvaToOffset(expRva);
        if (eo == null) return null;
        var nn = BitConverter.ToInt32(_data, eo.Value + 24);
        var af = RvaToOffset(BitConverter.ToUInt32(_data, eo.Value + 28));
        var an = RvaToOffset(BitConverter.ToUInt32(_data, eo.Value + 32));
        var ao = RvaToOffset(BitConverter.ToUInt32(_data, eo.Value + 36));
        if (af == null || an == null || ao == null) return null;
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
        for (int i = 0; i < nn; i++)
        {
            var no = RvaToOffset(BitConverter.ToUInt32(_data, an.Value + 4 * i));
            if (no == null) continue;
            var end = Array.IndexOf<byte>(_data, 0, no.Value);
            if (end < 0) continue;
            if (_data.AsSpan(no.Value, end - no.Value).SequenceEqual(nameBytes))
            {
                var od = BitConverter.ToUInt16(_data, ao.Value + 2 * i);
                return BitConverter.ToInt32(_data, af.Value + 4 * od);
            }
        }
        return null;
    }

    public int? ExportOffset(string name)
    {
        var r = ExportRva(name);
        return r.HasValue ? RvaToOffset((uint)r.Value) : null;
    }
}