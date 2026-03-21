using System.Runtime.InteropServices;
using System.Text;
using BedrockBoot.Netease.Native;

namespace BedrockBoot.Netease.Utils;

public class LevelDbEncryptHelper
{
    [DllImport("XOREncryptDLL.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto, EntryPoint = "decrypt_file")]
    public unsafe static extern int decrypt_file(byte* filePath, int filePathLen, out IntPtr buff, out int len);

    public static string DecryptRecord(string dbDir)
    {
        if (!Directory.Exists(dbDir))
        {
            return "";
        }

        return LevelDbEncryptHelper.DecryptRecord(new DirectoryInfo(dbDir));
    }

    private static string DecryptRecord(DirectoryInfo dbDirInfo)
    {
        DirectoryInfo[] directories = dbDirInfo.GetDirectories();
        for (int i = 0; i < directories.Length; i++)
        {
            string text = LevelDbEncryptHelper.DecryptRecord(directories[i]);
            if (!string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        FileInfo[] files = dbDirInfo.GetFiles();
        for (int i = 0; i < files.Length; i++)
        {
            string text2 = LevelDbEncryptHelper.DecryptFile(files[i].FullName);
            if (!string.IsNullOrEmpty(text2))
            {
                return text2;
            }
        }

        return "";
    }

    private unsafe static string DecryptFile(string filePath)
    {
        string text;
        byte[] bytes = Encoding.UTF8.GetBytes(filePath);
        int length = filePath.Length;
        IntPtr zero = IntPtr.Zero;
        int num = 0;
        fixed (byte* ptr = &bytes[0])
        {
            int num2 = LevelDbEncryptHelper.decrypt_file(ptr, length, out zero, out num);
            if (num2 != 0)
            {
                return string.Format("存档解密失败,错误码：{0}", num2);
            }

            if (num != 0 && zero != IntPtr.Zero)
            {
                byte[] array = new byte[num];
                Marshal.Copy(zero, array, 0, num);
                CoreNative.FreeMemory(zero);
                string @string = Encoding.UTF8.GetString(array);
                if (filePath == @string)
                {
                    return "";
                }

                try
                {
                    File.Copy(@string, filePath, true);
                    Delete(@string);
                    return "";
                }

                catch (Exception ex)
                {
                    Delete(@string);
                    return string.Format("转换失败！文件复制出错，错误: " + ex.Message);
                }
            }
        }

        return "";
    }

    public const string CORE_DLL_NAME = "XOREncryptDLL.dll";

    public static void Delete(string file)
    {
        if (File.Exists(file))
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        if (Directory.Exists(file))
        {
            Directory.Delete(file);
        }
    }
}