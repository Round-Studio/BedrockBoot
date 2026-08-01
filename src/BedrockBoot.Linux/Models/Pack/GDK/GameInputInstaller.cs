using BedrockBoot.Models.Pack.Wine;

namespace BedrockBoot.Models.Pack.GDK;

public static class GameInputInstaller
{
    public static void Install(string prefix, string gameDir)
    {
        var msiPath = Path.Combine(gameDir, "Installers", "GameInputRedist.msi");
        if (!File.Exists(msiPath))
        {
            Console.WriteLine("GameInputRedist.msi not found");
            return;
        }

        Console.WriteLine("Installing Microsoft GameInput...");
        try
        {
            var cab = ExtractEmbeddedCab(msiPath);
            if (cab == null) { Console.WriteLine("No CAB found in MSI"); return; }

            var files = DecompressCab(cab);
            var dlls = files.Where(f => IsPeDll(f.data)).OrderByDescending(f => f.data.Length).ToList();
            var exes = files.Where(f => IsPeExe(f.data)).OrderByDescending(f => f.data.Length).ToList();

            if (dlls.Count == 0 || exes.Count == 0) { Console.WriteLine("No PE files found in CAB"); return; }

            var x64 = Path.Combine(prefix, "drive_c", "Program Files", "Microsoft GameInput", "x64");
            var x86 = Path.Combine(prefix, "drive_c", "Program Files", "Microsoft GameInput", "x86");
            var sys32 = Path.Combine(prefix, "drive_c", "windows", "system32");

            Directory.CreateDirectory(x64);
            File.WriteAllBytes(Path.Combine(x64, "GameInputRedist.dll"), dlls[0].data);
            File.WriteAllBytes(Path.Combine(sys32, "GameInputRedist.dll"), dlls[0].data);
            File.WriteAllBytes(Path.Combine(x64, "GameInputRedistService.exe"), exes[0].data);
            if (dlls.Count >= 2)
                File.WriteAllBytes(Path.Combine(x64, "GameInputBridge.dll"), dlls[1].data);
            if (exes.Count >= 2)
                File.WriteAllBytes(Path.Combine(x64, "GameInputRawInputProxy.exe"), exes[1].data);
            if (dlls.Count >= 3)
            {
                Directory.CreateDirectory(x86);
                File.WriteAllBytes(Path.Combine(x86, "GameInputRedist.dll"), dlls[2].data);
            }

            SetRegistry(prefix);
            Console.WriteLine("Microsoft GameInput installed");
        }
        catch (Exception e) { Console.WriteLine($"GameInput install failed: {e.Message}"); }
    }

    static void SetRegistry(string prefix)
    {
        var redist = @"C:\Program Files\Microsoft GameInput\x64";
        var service = @"System\CurrentControlSet\Services\GameInputRedistService";
        var changes = new RegChange[]
        {
            WineRegistry.RegSz(@"Software\Microsoft\GameInput", "RedistDir", redist),
            WineRegistry.RegSz(@"Software\Wow6432Node\Microsoft\GameInput", "RedistDir", redist),
            WineRegistry.RegSz(service, "DisplayName", "GameInput Redist Service"),
            WineRegistry.RegSz(service, "Description", "GameInput Redist Service"),
            WineRegistry.RegSz(service, "ImagePath", redist + @"\GameInputRedistService.exe"),
            WineRegistry.RegSz(service, "ObjectName", "LocalSystem"),
            WineRegistry.RegDword(service, "ErrorControl", 0),
            WineRegistry.RegDword(service, "Start", 3),
            WineRegistry.RegDword(service, "Type", 0x10),
        };
        WineRegistry.UpdatePrefix(prefix, machine: changes);
    }

    static byte[]? ExtractEmbeddedCab(string msiPath)
    {
        var msi = File.ReadAllBytes(msiPath);
        if (msi.Length < 8 || msi[0] != 0xD0 || msi[1] != 0xCF) return null;

        var ssz = 1 << BitConverter.ToUInt16(msi, 0x1E);
        var dir0 = BitConverter.ToInt32(msi, 0x30);
        var minicut = BitConverter.ToInt32(msi, 0x38);

        const int FREE = -1, ENDC = -2;

        byte[] Sector(int n) => msi.AsSpan((n + 1) * ssz, ssz).ToArray();

        var difat = new List<int>();
        for (int i = 0; i < 109; i++) difat.Add(BitConverter.ToInt32(msi, 0x4C + i * 4));

        var nxt = BitConverter.ToInt32(msi, 0x44);
        var ndifat = BitConverter.ToInt32(msi, 0x48);
        for (int _ = 0; _ < ndifat && nxt != FREE && nxt != ENDC; _++)
        {
            var vals = Sector(nxt);
            for (int i = 0; i < ssz / 4 - 1; i++) difat.Add(BitConverter.ToInt32(vals, i * 4));
            nxt = BitConverter.ToInt32(vals, ssz - 4);
        }

        var fat = new List<int>();
        foreach (var fs in difat.Where(d => d != FREE))
        {
            var sec = Sector(fs);
            for (int i = 0; i < ssz / 4; i++) fat.Add(BitConverter.ToInt32(sec, i * 4));
        }

        List<int> Chain(int start)
        {
            var outList = new List<int>();
            var seen = new HashSet<int>();
            var n = start;
            while (n != ENDC && n != FREE && n >= 0 && n < fat.Count && seen.Add(n))
            {
                outList.Add(n);
                n = fat[n];
            }
            return outList;
        }

        byte[] ReadBig(int start, int size)
        {
            var chains = Chain(start);
            var result = new byte[size];
            int off = 0;
            foreach (var s in chains)
            {
                var chunk = Sector(s);
                var take = Math.Min(chunk.Length, size - off);
                Buffer.BlockCopy(chunk, 0, result, off, take);
                off += take;
            }
            return result;
        }

        var dirData = ReadBig(dir0, Chain(dir0).Count * ssz);
        for (int i = 0; i < dirData.Length; i += 128)
        {
            if (i + 128 > dirData.Length) break;
            var type = dirData[i + 66];
            if (type == 2)
            {
                var start = BitConverter.ToInt32(dirData, i + 116);
                var size = (int)BitConverter.ToInt64(dirData, i + 120);
                if (size >= 4)
                {
                    var head = size >= minicut ? ReadBig(start, size) : null;
                    if (head != null && head[0] == 'M' && head[1] == 'S' && head[2] == 'C' && head[3] == 'F')
                        return head;
                }
            }
        }
        return null;
    }

    static List<(string name, byte[] data)> DecompressCab(byte[] cab)
    {
        var result = new List<(string, byte[])>();
        if (cab.Length < 40 || cab[0] != 'M' || cab[1] != 'S' || cab[2] != 'C' || cab[3] != 'F')
            return result;

        var coffFiles = BitConverter.ToInt32(cab, 16);
        var cfolders = BitConverter.ToUInt16(cab, 26);
        var cFiles = BitConverter.ToUInt16(cab, 28);
        var flags = BitConverter.ToUInt16(cab, 30);
        int o = 36, cbHeader = 0, cbFolder = 0, cbData = 0;

        if ((flags & 4) != 0)
        {
            cbHeader = BitConverter.ToUInt16(cab, o);
            cbFolder = cab[o + 2];
            cbData = cab[o + 3];
            o += 4 + cbHeader;
        }

        var folders = new List<(int coff, int ndata)>();
        for (int i = 0; i < cfolders; i++)
        {
            var coff = BitConverter.ToInt32(cab, o);
            var nd = BitConverter.ToUInt16(cab, o + 4);
            o += 8 + cbFolder;
            folders.Add((coff, nd));
        }

        if (coffFiles <= 0 || coffFiles >= cab.Length)
        {
            Console.WriteLine($"Invalid coffFiles offset: {coffFiles}");
            return result;
        }

        var fileEntries = new List<(string name, int size, int uoff, int folder)>();
        int p = coffFiles;
        for (int i = 0; i < cFiles; i++)
        {
            if (p + 16 > cab.Length) break;

            var size = BitConverter.ToInt32(cab, p);
            var uoff = BitConverter.ToInt32(cab, p + 4);
            var folder = BitConverter.ToUInt16(cab, p + 8);
            int nameEnd = -1;
            for (int j = p + 16; j < cab.Length && j < p + 16 + 256; j++)
            {
                if (cab[j] == 0) { nameEnd = j; break; }
            }
            if (nameEnd == -1) break;

            var name = System.Text.Encoding.ASCII.GetString(cab, p + 16, nameEnd - (p + 16));
            fileEntries.Add((name, size, uoff, folder));
            p = nameEnd + 1;
        }

        var folderData = new List<byte[]>();
        foreach (var (coff, ndata) in folders)
        {
            if (coff < 0 || coff >= cab.Length)
            {
                Console.WriteLine($"Invalid folder offset: {coff}");
                continue;
            }

            int q = coff;
            var inflate = MsZipInflate.CreateFolder();
            for (int j = 0; j < ndata; j++)
            {
                if (q + 8 > cab.Length) break;

                var cbComp = BitConverter.ToUInt16(cab, q + 4);
                q += 8 + cbData;

                if (q + cbComp > cab.Length) break;

                var blk = cab.AsSpan(q, cbComp);
                q += cbComp;

                try
                {
                    inflate.PushBlock(blk);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Decompression error: {ex.Message}");
                    return result;
                }
            }
            folderData.Add(inflate.Result);
        }

        foreach (var (name, size, uoff, folder) in fileEntries)
        {
            if (folder < 0 || folder >= folderData.Count) continue;
            var data = folderData[folder];
            if (uoff < 0 || size < 0 || uoff + size > data.Length) continue;
            result.Add((name, data.AsSpan(uoff, size).ToArray()));
        }
        return result;
    }

    static bool IsPeDll(byte[] data) => PeKind(data) == "dll";
    static bool IsPeExe(byte[] data) => PeKind(data) == "exe";

    static string? PeKind(byte[] data)
    {
        if (data.Length < 0x40 || data[0] != 'M' || data[1] != 'Z') return null;
        var pe = BitConverter.ToInt32(data, 0x3C);
        if (pe + 24 > data.Length || data[pe] != 'P' || data[pe + 1] != 'E') return null;
        return (BitConverter.ToUInt16(data, pe + 22) & 0x2000) != 0 ? "dll" : "exe";
    }
}