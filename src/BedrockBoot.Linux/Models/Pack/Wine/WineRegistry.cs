using System.Text.RegularExpressions;
using BedrockBoot.Models.Helper;

namespace BedrockBoot.Models.Pack.Wine;

public enum RegKind { String, Dword, Delete }

public record RegChange(string Key, string Name, RegKind Kind, object? Value = null);

public static class WineRegistry
{
    static readonly Regex SectionRx = new(@"^\[([^]]+)\](?:\s+\d+)?\s*$");
    static readonly Regex DwordRx = new(@"^""Current""=dword:([0-9a-f]{1,8})\s*$", RegexOptions.IgnoreCase);

    public static RegChange RegSz(string key, string name, string value) => new(key, name, RegKind.String, value);
    public static RegChange RegDword(string key, string name, int value) => new(key, name, RegKind.Dword, value);
    public static RegChange RegDelete(string key, string name) => new(key, name, RegKind.Delete);

    static string Escape(string text)
    {
        if (text.Contains('\0')) throw new ArgumentException("NUL in registry text");
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    static string ValueLine(RegChange c)
    {
        var name = Escape(c.Name);
        return c.Kind switch
        {
            RegKind.String => $"\"{name}\"=\"{Escape((string)c.Value!)}\"\n",
            RegKind.Dword => $"\"{name}\"=dword:{(int)c.Value!:x08}\n",
            RegKind.Delete => "",
            _ => throw new ArgumentException("unsupported kind"),
        };
    }

    static string? SectionKey(string line)
    {
        var m = SectionRx.Match(line.TrimEnd('\r', '\n'));
        return m.Success ? m.Groups[1].Value : null;
    }

    static string[] ApplyChanges(string[] lines, RegChange[] changes)
    {
        foreach (var change in changes)
        {
            var escapedKey = Escape(change.Key);
            var escapedName = Escape(change.Name);
            int header = -1, end = lines.Length;

            for (int i = 0; i < lines.Length; i++)
            {
                if (SectionKey(lines[i]) == escapedKey) { header = i; break; }
            }

            if (header == -1)
            {
                if (change.Kind == RegKind.Delete) continue;
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var ft = (long)((DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 11644473600) * 10000000);
                var newLines = new List<string>(lines);
                if (newLines.Count > 0 && !string.IsNullOrEmpty(newLines[^1].Trim()))
                    newLines.Add("\n");
                newLines.Add($"[{escapedKey}] {now}\n");
                newLines.Add($"#time={ft:x}\n");
                newLines.Add(ValueLine(change));
                newLines.Add("\n");
                lines = newLines.ToArray();
                continue;
            }

            for (int i = header + 1; i < end; i++)
            {
                if (SectionKey(lines[i]) != null) { end = i; break; }
            }

            var prefix = $"\"{escapedName}\"=";
            int found = -1;
            for (int i = header + 1; i < end; i++)
            {
                if (lines[i].StartsWith(prefix)) { found = i; break; }
            }

            if (found != -1)
            {
                var valEnd = found + 1;
                while (valEnd < end && lines[valEnd - 1].TrimEnd('\r', '\n').EndsWith('\\'))
                    valEnd++;
                var list = lines.ToList();
                if (change.Kind == RegKind.Delete)
                    list.RemoveRange(found, valEnd - found);
                else
                {
                    list[found] = ValueLine(change);
                    if (valEnd > found + 1)
                        list.RemoveRange(found + 1, valEnd - found - 1);
                }
                lines = list.ToArray();
            }
            else if (change.Kind != RegKind.Delete)
            {
                var insert = end;
                while (insert > header + 1 && string.IsNullOrEmpty(lines[insert - 1].Trim()))
                    insert--;
                var list = lines.ToList();
                list.Insert(insert, ValueLine(change));
                lines = list.ToArray();
            }
        }
        return lines;
    }

    static int ResolveCurrentControlSet(string text)
    {
        var target = Escape(@"System\Select").ToLowerInvariant();
        bool inSelect = false;
        foreach (var line in text.Split('\n'))
        {
            var sk = SectionKey(line);
            if (sk != null) { inSelect = sk.ToLowerInvariant() == target; continue; }
            if (!inSelect) continue;
            var m = DwordRx.Match(line);
            if (m.Success) return Convert.ToInt32(m.Groups[1].Value, 16);
        }
        throw new InvalidOperationException("No System\\Select\\Current");
    }

    static RegChange[] ResolveControlSetChanges(string text, RegChange[] changes)
    {
        bool needsResolution = false;
        foreach (var c in changes)
        {
            var parts = c.Key.Split('\\');
            if (parts.Length >= 2 && parts[0].Equals("System", StringComparison.OrdinalIgnoreCase)
                                 && parts[1].Equals("CurrentControlSet", StringComparison.OrdinalIgnoreCase))
            { needsResolution = true; break; }
        }
        if (!needsResolution) return changes;

        var current = ResolveCurrentControlSet(text);
        var resolved = new List<RegChange>();
        foreach (var c in changes)
        {
            var parts = c.Key.Split('\\');
            if (parts.Length >= 2 && parts[0].Equals("System", StringComparison.OrdinalIgnoreCase)
                                 && parts[1].Equals("CurrentControlSet", StringComparison.OrdinalIgnoreCase))
            {
                resolved.Add(c with { Key = $"System\\ControlSet{current:D3}\\" + string.Join("\\", parts.Skip(2)) });
            }
            else resolved.Add(c);
        }
        return resolved.ToArray();
    }

    static string Render(string original, RegChange[] changes)
    {
        if (!original.StartsWith("WINE REGISTRY Version"))
            throw new InvalidOperationException("Not a Wine registry file");
        var normalized = original.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var result = ApplyChanges(lines, changes);
        return string.Join("\n", result);
    }

    static void AtomicWrite(string path, string content, UnixFileMode mode)
    {
        var dir = Path.GetDirectoryName(path)!;
        var tmp = Path.Combine(dir, "." + Path.GetFileName(path) + ".bol-" + Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(tmp, content);
        try { File.SetUnixFileMode(tmp, mode); } catch { }
        File.Move(tmp, path, true);
    }

    static void RequirePrefixIdle(string prefix)
    {
        foreach (var procDir in Directory.GetDirectories("/proc"))
        {
            var pidStr = Path.GetFileName(procDir);
            if (!int.TryParse(pidStr, out var pid) || pid <= 1) continue;
            try
            {
                var environ = File.ReadAllText(Path.Combine(procDir, "environ"));
                if (environ.Contains($"WINEPREFIX={prefix}\0"))
                    throw new InvalidOperationException($"Wine process {pid} still running for prefix {prefix}");
            }
            catch (UnauthorizedAccessException) { }
            catch (FileNotFoundException) { }
        }
    }

    public static bool UpdatePrefix(string prefix, RegChange[]? machine = null, RegChange[]? user = null)
    {
        var systemReg = Path.Combine(prefix, "system.reg");
        var userReg = Path.Combine(prefix, "user.reg");
        machine ??= [];
        user ??= [];

        if (machine.Length == 0 && user.Length == 0) return false;

        var lockPath = Path.Combine(prefix, ".bol-offline-registry.lock");
        using var _ = FileLock.Acquire(lockPath);
        RequirePrefixIdle(prefix);

        var changed = false;

        if (machine.Length > 0 && File.Exists(systemReg))
        {
            var orig = File.ReadAllText(systemReg);
            var resolved = ResolveControlSetChanges(orig, machine);
            var rendered = Render(orig, resolved);
            if (rendered != orig)
            {
                AtomicWrite(systemReg, rendered, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                changed = true;
            }
        }

        if (user.Length > 0 && File.Exists(userReg))
        {
            var orig = File.ReadAllText(userReg);
            var rendered = Render(orig, user);
            if (rendered != orig)
            {
                AtomicWrite(userReg, rendered, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                changed = true;
            }
        }

        return changed;
    }
}