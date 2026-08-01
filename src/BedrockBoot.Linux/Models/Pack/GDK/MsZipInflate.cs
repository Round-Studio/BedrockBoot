namespace BedrockBoot.Models.Pack.GDK;

public static class MsZipInflate
{
    public sealed class FolderInflate
    {
        private readonly InflateState _state = new(Array.Empty<byte>());

        public void PushBlock(ReadOnlySpan<byte> block)
        {
            if (block.Length < 2 || block[0] != 'C' || block[1] != 'K')
                throw new InvalidDataException("Invalid MSZIP block header");
            Inflate(_state, block[2..]);
        }

        public byte[] Result => _state.ToOutput();
    }

    public static FolderInflate CreateFolder() => new();

    private static readonly int[] LengthBase = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
    private static readonly int[] LengthExtra = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
    private static readonly int[] DistBase = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
    private static readonly int[] DistExtra = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
    private static readonly int[] ClOrder = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

    private static void Inflate(InflateState s, ReadOnlySpan<byte> data)
    {
        var r = new BitReader(data);
        while (true)
        {
            var bfinal = r.Read(1);
            var btype = r.Read(2);
            if (btype == 0)
            {
                r.AlignByte();
                var len = r.Read(16);
                var nlen = r.Read(16);
                if ((len ^ 0xFFFF) != nlen) throw new InvalidDataException("Stored block length mismatch");
                for (var i = 0; i < len; i++) s.PushLiteral(r.ReadByte());
            }
            else
            {
                Huffman hlit, hdist;
                if (btype == 1)
                {
                    var ll = new int[288];
                    for (var i = 0; i < 144; i++) ll[i] = 8;
                    for (var i = 144; i < 256; i++) ll[i] = 9;
                    for (var i = 256; i < 280; i++) ll[i] = 7;
                    for (var i = 280; i < 288; i++) ll[i] = 8;
                    var dl = new int[30];
                    for (var i = 0; i < 30; i++) dl[i] = 5;
                    hlit = new Huffman(ll);
                    hdist = new Huffman(dl);
                }
                else if (btype == 2)
                {
                    var hlitc = r.Read(5) + 257;
                    var hdistc = r.Read(5) + 1;
                    var hclenc = r.Read(4) + 4;
                    var clLengths = new int[19];
                    for (var i = 0; i < hclenc; i++) clLengths[ClOrder[i]] = r.Read(3);
                    var clHuff = new Huffman(clLengths);
                    var lengths = new int[hlitc + hdistc];
                    int pos = 0;
                    while (pos < lengths.Length)
                    {
                        var sym = clHuff.Decode(r);
                        if (sym < 16) lengths[pos++] = sym;
                        else if (sym == 16)
                        {
                            if (pos == 0) throw new InvalidDataException("Length repeat with no previous value");
                            var v = lengths[pos - 1];
                            var rep = 3 + r.Read(2);
                            while (rep-- > 0 && pos < lengths.Length) lengths[pos++] = v;
                        }
                        else if (sym == 17)
                        {
                            var rep = 3 + r.Read(3);
                            while (rep-- > 0 && pos < lengths.Length) lengths[pos++] = 0;
                        }
                        else
                        {
                            var rep = 11 + r.Read(7);
                            while (rep-- > 0 && pos < lengths.Length) lengths[pos++] = 0;
                        }
                    }
                    hlit = new Huffman(lengths.AsSpan(0, hlitc).ToArray());
                    hdist = new Huffman(lengths.AsSpan(hlitc).ToArray());
                }
                else throw new InvalidDataException("Invalid block type");

                while (true)
                {
                    var sym = hlit.Decode(r);
                    if (sym < 256)
                    {
                        s.PushLiteral((byte)sym);
                        continue;
                    }
                    if (sym == 256) break;
                    var li = sym - 257;
                    var length = LengthBase[li] + r.Read(LengthExtra[li]);
                    var dsym = hdist.Decode(r);
                    var dist = DistBase[dsym] + r.Read(DistExtra[dsym]);
                    s.CopyMatch(length, dist);
                }
            }
            if (bfinal == 1) break;
        }
    }

    private sealed class BitReader
    {
        private readonly ReadOnlyMemory<byte> _data;
        private int _pos;
        private uint _acc;
        private int _nbits;

        public BitReader(ReadOnlySpan<byte> data)
        {
            _data = data.ToArray();
        }

        public int Read(int n)
        {
            while (_nbits < n)
            {
                if (_pos >= _data.Length) throw new InvalidDataException("Unexpected end of deflate data");
                _acc |= (uint)_data.Span[_pos++] << _nbits;
                _nbits += 8;
            }
            var v = (int)(_acc & ((1u << n) - 1));
            _acc >>= n;
            _nbits -= n;
            return v;
        }

        public void AlignByte()
        {
            var drop = _nbits & 7;
            if (drop != 0)
            {
                _acc >>= drop;
                _nbits -= drop;
            }
        }

        public byte ReadByte() => (byte)Read(8);
    }

    private sealed class Huffman
    {
        private readonly int[] _count = new int[16];
        private readonly int[] _symbol;
        private readonly int[] _firstCode = new int[16];

        public Huffman(int[] lengths)
        {
            foreach (var l in lengths)
                if (l > 0) _count[l]++;
            var total = 0;
            for (var l = 1; l <= 15; l++)
            {
                _firstCode[l] = total;
                total = (total + _count[l]) << 1;
            }
            _symbol = new int[total];
            var offsets = new int[16];
            for (var l = 1; l <= 15; l++) offsets[l] = _firstCode[l];
            for (var s = 0; s < lengths.Length; s++)
            {
                var l = lengths[s];
                if (l > 0) _symbol[offsets[l]++] = s;
            }
        }

        public int Decode(BitReader r)
        {
            var code = 0;
            for (var len = 1; len <= 15; len++)
            {
                code = (code << 1) | r.Read(1);
                var first = _firstCode[len];
                if (code - first < _count[len])
                    return _symbol[first + (code - first)];
            }
            throw new InvalidDataException("Invalid Huffman code");
        }
    }

    private sealed class InflateState
    {
        private const int WindowSize = 1 << 15;
        private readonly byte[] _window = new byte[WindowSize];
        private readonly MemoryStream _out = new();
        private readonly int _dictLen;
        private int _fill;

        public InflateState(byte[] dict)
        {
            _dictLen = dict.Length;
            if (_dictLen > 0)
            {
                Buffer.BlockCopy(dict, 0, _window, 0, _dictLen);
                _fill = _dictLen;
            }
        }

        public void PushLiteral(byte b)
        {
            _window[_fill & (WindowSize - 1)] = b;
            _out.WriteByte(b);
            _fill++;
        }

        public void CopyMatch(int length, int distance)
        {
            for (var i = 0; i < length; i++)
            {
                var b = _window[(_fill - distance) & (WindowSize - 1)];
                _window[_fill & (WindowSize - 1)] = b;
                _out.WriteByte(b);
                _fill++;
            }
        }

        public byte[] ToOutput() => _out.ToArray();
    }
}
