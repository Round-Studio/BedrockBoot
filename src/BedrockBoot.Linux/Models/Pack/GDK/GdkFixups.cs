namespace BedrockBoot.Models.Pack.GDK;

public static class GdkFixups
{
    public static void PatchLhcXcurlGate(string gameDir)
    {
        var dllPath = Path.Combine(gameDir, "libHttpClient.GDK.dll");
        if (!File.Exists(dllPath)) return;
        var data = File.ReadAllBytes(dllPath);

        var pattern = new byte[] { 0x83, 0xC0, 0xFE, 0xBA, 0x04, 0x00, 0x00, 0x00, 0x48, 0x8D, 0x0D };
        var cmpPattern = new byte[] { 0x83, 0xF8, 0x06 };

        var idx = IndexOfBytes(data, pattern);
        if (idx < 0) { Console.WriteLine("LHC provider gate not found"); return; }

        var cmpIdx = IndexOfBytes(data, cmpPattern, idx + pattern.Length);
        if (cmpIdx < 0) return;

        var jaOff = cmpIdx + cmpPattern.Length;
        if (jaOff + 6 > data.Length) return;

        if (data[jaOff] == 0x90 && data[jaOff + 5] == 0x90) return;

        for (int i = 0; i < 6; i++) data[jaOff + i] = 0x90;
        File.WriteAllBytes(dllPath, data);
        Console.WriteLine("libHttpClient forced to XCurl provider");
    }

    public static void BumpStackReserve(string exePath, long target = 0x1000000)
    {
        if (!File.Exists(exePath)) return;
        try
        {
            using var fs = File.Open(exePath, FileMode.Open, FileAccess.ReadWrite);
            var head = new byte[0x400];
            fs.Read(head, 0, head.Length);
            if (head[0] != 'M' || head[1] != 'Z') return;
            var e = BitConverter.ToInt32(head, 0x3C);
            if (head[e] != 'P' || head[e + 1] != 'E') return;
            var opt = e + 4 + 20;
            if (BitConverter.ToUInt16(head, opt) != 0x20B) return;
            var field = opt + 72;
            var cur = BitConverter.ToInt64(head, field);
            if (cur >= target) return;
            fs.Seek(field, SeekOrigin.Begin);
            fs.Write(BitConverter.GetBytes(target));
            Console.WriteLine($"Stack reserve raised to {target >> 10} KB");
        }
        catch (Exception e) { Console.WriteLine($"Could not bump stack reserve: {e.Message}"); }
    }

    static int IndexOfBytes(byte[] haystack, byte[] needle)
    {
        return IndexOfBytes(haystack, needle, 0);
    }

    static int IndexOfBytes(byte[] haystack, byte[] needle, int start)
    {
        int end = haystack.Length - needle.Length;
        for (int i = start; i <= end; i++)
        {
            bool found = true;
            for (int j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j]) { found = false; break; }
            if (found) return i;
        }
        return -1;
    }
}