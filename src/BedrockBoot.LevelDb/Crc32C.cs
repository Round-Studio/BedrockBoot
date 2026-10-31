using System;

namespace BedrockBoot.LevelDb;

// 与 leveldb util/crc32c.cc 完全一致的 CRC-32C（Castagnoli）实现
public static class Crc32C
{
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var j = 0; j < 8; j++) c = (c & 1) == 1 ? 0x82F63B78u ^ (c >> 1) : c >> 1;
            table[i] = c;
        }
        return table;
    }

    public static uint Value(ReadOnlySpan<byte> data) => Extend(0, data);

    public static uint Extend(uint crc, ReadOnlySpan<byte> data)
    {
        var l = crc ^ 0xFFFFFFFFu;
        foreach (var b in data) l = Table[(b ^ l) & 0xFF] ^ (l >> 8);
        return l ^ 0xFFFFFFFFu;
    }

    public static uint Mask(uint crc) => ((crc >> 15) | (crc << 17)) + 0xa282ead8u;

    public static uint Unmask(uint maskedCrc)
    {
        var rot = maskedCrc - 0xa282ead8u;
        return (rot >> 17) | (rot << 15);
    }
}
