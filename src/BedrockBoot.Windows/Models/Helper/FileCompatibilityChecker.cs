using Microsoft.Win32;

namespace BedrockBoot.Models.Helper;

public static class FileCompatibilityChecker
{
    public static bool IsRunAsAdminChecked(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件不存在", filePath);

        if (!filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return false;

        string fullPath = Path.GetFullPath(filePath).ToLowerInvariant();
        
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
        {
            if (key != null)
            {
                string value = key.GetValue(fullPath) as string;
                if (!string.IsNullOrEmpty(value) && value.Contains("RUNASADMIN"))
                {
                    return true;
                }
            }
        }

        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
        {
            if (key != null)
            {
                string value = key.GetValue(fullPath) as string;
                if (!string.IsNullOrEmpty(value) && value.Contains("RUNASADMIN"))
                {
                    return true;
                }
            }
        }

        try
        {
            string adsPath = filePath + ":compatibility";
            if (File.Exists(adsPath))
            {
                string content = File.ReadAllText(adsPath);
                if (content.Contains("RUNASADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    public static void RemoveRunAsAdmin(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件不存在", filePath);

        if (!filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return;

        string fullPath = Path.GetFullPath(filePath).ToLowerInvariant();

        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true))
        {
            if (key != null)
            {
                string value = key.GetValue(fullPath) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Contains("RUNASADMIN"))
                    {
                        string newValue = value.Replace("RUNASADMIN", "").Trim();
                        if (string.IsNullOrEmpty(newValue))
                        {
                            key.DeleteValue(fullPath);
                        }
                        else
                        {
                            key.SetValue(fullPath, newValue);
                        }
                    }
                }
            }
        }

        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true))
        {
            if (key != null)
            {
                string value = key.GetValue(fullPath) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Contains("RUNASADMIN"))
                    {
                        string newValue = value.Replace("RUNASADMIN", "").Trim();
                        if (string.IsNullOrEmpty(newValue))
                        {
                            key.DeleteValue(fullPath);
                        }
                        else
                        {
                            key.SetValue(fullPath, newValue);
                        }
                    }
                }
            }
        }

        try
        {
            string adsPath = filePath + ":compatibility";
            if (File.Exists(adsPath))
            {
                string content = File.ReadAllText(adsPath);
                if (content.Contains("RUNASADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    string newContent = content.Replace("RUNASADMIN", "").Trim();
                    if (string.IsNullOrEmpty(newContent))
                    {
                        File.Delete(adsPath);
                    }
                    else
                    {
                        File.WriteAllText(adsPath, newContent);
                    }
                }
            }
        }
        catch
        {
        }
    }

    public static void SetRunAsAdmin(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件不存在", filePath);

        if (!filePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            return;

        string fullPath = Path.GetFullPath(filePath).ToLowerInvariant();

        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", true))
        {
            if (key != null)
            {
                string existingValue = key.GetValue(fullPath) as string;
                if (string.IsNullOrEmpty(existingValue))
                {
                    key.SetValue(fullPath, "RUNASADMIN");
                }
                else if (!existingValue.Contains("RUNASADMIN"))
                {
                    key.SetValue(fullPath, existingValue + " RUNASADMIN");
                }
            }
        }
    }
}