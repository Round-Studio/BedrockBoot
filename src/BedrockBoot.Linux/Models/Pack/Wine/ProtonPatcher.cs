using BedrockBoot.Models.Pack.Pe;

namespace BedrockBoot.Models.Pack.Wine;

public static class ProtonPatcher
{
    public static void Patch(string protonRoot)
    {
        var wineDir = Path.Combine(protonRoot, "files", "lib", "wine", "x86_64-windows");
        var combasePath = Path.Combine(wineDir, "combase.dll");
        var ntdllPath = Path.Combine(wineDir, "ntdll.dll");

        if (!File.Exists(combasePath) || !File.Exists(ntdllPath))
        {
            Console.WriteLine("Wine DLLs not found for patching");
            return;
        }

        // Patch combase.RoOriginateErrorW
        try
        {
            var pe = new PeFile(combasePath);
            var off = pe.ExportOffset("RoOriginateErrorW");
            if (off.HasValue)
            {
                var data = File.ReadAllBytes(combasePath);
                var patch = new byte[] { 0x31, 0xC0, 0xC3, 0x90 };
                Array.Copy(patch, 0, data, off.Value, patch.Length);
                File.WriteAllBytes(combasePath, data);
                Console.WriteLine("combase.RoOriginateErrorW patched");
            }
        }
        catch (Exception e) { Console.WriteLine($"combase patch failed: {e.Message}"); }

        // Patch ntdll stubs
        try
        {
            var pe = new PeFile(ntdllPath);
            var rre = pe.ExportRva("RtlRaiseException");
            var data = File.ReadAllBytes(ntdllPath);
            var sig = new byte[] { 0x55, 0x53, 0x48, 0x81, 0xEC, 0xC8, 0x00, 0x00, 0x00, 0x48, 0x8D, 0xAC, 0x24, 0xC0, 0x00, 0x00, 0x00 };
            var newStub = new byte[] { 0xB8, 0x02, 0x00, 0x00, 0xC0, 0xC3, 0x90, 0x90 };
            var funnels = new List<int>();

            if (rre.HasValue)
            {
                int i = 0;
                while (true)
                {
                    i = IndexOfBytes(data, sig, i);
                    if (i < 0) break;
                    var callOff = i + sig.Length;
                    // Check for call RtlRaiseException (e8 rel32 or ff 15 ...)
                    if (callOff + 5 < data.Length)
                    {
                        if (data[callOff] == 0x48 && data[callOff + 1] == 0x89 && data[callOff + 2] == 0xD9 && data[callOff + 3] == 0xE8)
                        {
                            if (callOff + 7 < data.Length && data[callOff + 7] == 0xEB && data[callOff + 8] == 0xF6)
                                funnels.Add(i);
                        }
                    }
                    i += sig.Length;
                }
            }

            if (funnels.Count > 0)
            {
                var raw = new byte[data.Length];
                Array.Copy(data, raw, data.Length);
                foreach (var o in funnels)
                    Array.Copy(newStub, 0, raw, o, newStub.Length);
                File.WriteAllBytes(ntdllPath, raw);
                Console.WriteLine($"ntdll: {funnels.Count} stub(s) neutralised");
            }
            else
            {
                Console.WriteLine("ntdll: already patched or no stubs found");
            }
        }
        catch (Exception e) { Console.WriteLine($"ntdll patch failed: {e.Message}"); }
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