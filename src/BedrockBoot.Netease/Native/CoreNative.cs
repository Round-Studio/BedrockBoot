using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BedrockBoot.Netease.Native;

public class CoreNative
{
	public unsafe static string ComputeSilence(string url, string body, string data)
	{
		if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(data))
		{
			return data;
		}

		byte[] bytes = Encoding.UTF8.GetBytes(url);
		byte[] array = Encoding.UTF8.GetBytes(body);
		byte[] bytes2 = Encoding.UTF8.GetBytes(data);
		int num = array.Length;
		if (num == 0)
		{
			array = new byte[1];
		}

		fixed (byte* ptr = &bytes[0])
		{
			byte* url2 = ptr;
			fixed (byte* ptr2 = &array[0])
			{
				byte* body2 = ptr2;
				fixed (byte* ptr3 = &bytes2[0])
				{
					byte* data2 = ptr3;
					IntPtr zero = IntPtr.Zero;
					int num2 = Native.ComputeSilence(url2, body2, data2, out zero, num);
					if (num2 != 0 && zero != IntPtr.Zero)
					{
						byte[] array2 = new byte[num2];
						Marshal.Copy(zero, array2, 0, num2);
						CoreNative.FreeMemory(zero);
						return Encoding.UTF8.GetString(array2);
					}
				}
			}
		}

		return data;
	}

	public unsafe static byte[] HttpEncrypt(string url, string body, out string keyStr)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(body);
		if (string.IsNullOrEmpty(body))
		{
			keyStr = string.Empty;
			return bytes;
		}

		fixed (byte* ptr = &Encoding.UTF8.GetBytes(url)[0])
		{
			byte* url2 = ptr;
			fixed (byte* ptr2 = &bytes[0])
			{
				byte* body2 = ptr2;
				IntPtr zero = IntPtr.Zero;
				IntPtr zero2 = IntPtr.Zero;
				int num = Native.HttpEncrypt(url2, body2, out zero, out zero2);
				byte[] array = new byte[num];
				byte[] array2 = new byte[16];
				if (num != 0 || zero != IntPtr.Zero)
				{
					Marshal.Copy(zero, array, 0, num);
					CoreNative.FreeMemory(zero);
				}

				if (zero2 != IntPtr.Zero)
				{
					Marshal.Copy(zero2, array2, 0, array2.Length);
					CoreNative.FreeMemory(zero2);
				}

				keyStr = Encoding.UTF8.GetString(array2);
				return array;
			}
		}
	}

	public static string HttpDecrypt(byte[] body, out string keyStr)
	{
		if (body == null || body.Length == 0)
		{
			keyStr = string.Empty;
			return string.Empty;
		}

		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf<byte>(body[0]) * body.Length);
		string result = string.Empty;
		keyStr = string.Empty;
		int nSize = body.Length;
		try
		{
			Marshal.Copy(body, 0, intPtr, body.Length);
			IntPtr zero = IntPtr.Zero;
			IntPtr zero2 = IntPtr.Zero;
			int num = Native.HttpDecrypt(intPtr, nSize, out zero, out zero2);
			if (num != 0 && zero != IntPtr.Zero)
			{
				byte[] array = new byte[num];
				Marshal.Copy(zero, array, 0, num);
				CoreNative.FreeMemory(zero);
				result = Encoding.UTF8.GetString(array);
			}

			if (zero2 != IntPtr.Zero)
			{
				byte[] array2 = new byte[16];
				Marshal.Copy(zero2, array2, 0, array2.Length);
				CoreNative.FreeMemory(zero2);
				keyStr = Encoding.UTF8.GetString(array2);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}

		return result;
	}

	public static string ParseLoginResponse(byte[] body, out string keyStr)
	{
		if (body == null || body.Length == 0)
		{
			keyStr = string.Empty;
			return string.Empty;
		}

		IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf<byte>(body[0]) * body.Length);
		string result = string.Empty;
		keyStr = string.Empty;
		int nSize = body.Length;
		try
		{
			Marshal.Copy(body, 0, intPtr, body.Length);
			IntPtr zero = IntPtr.Zero;
			IntPtr zero2 = IntPtr.Zero;
			int num = Native.ParseLoginResponse(intPtr, nSize, out zero, out zero2);
			if (num != 0 && zero != IntPtr.Zero)
			{
				byte[] array = new byte[num];
				Marshal.Copy(zero, array, 0, num);
				CoreNative.FreeMemory(zero);
				result = Encoding.UTF8.GetString(array);
			}

			if (zero2 != IntPtr.Zero)
			{
				byte[] array2 = new byte[16];
				Marshal.Copy(zero2, array2, 0, array2.Length);
				CoreNative.FreeMemory(zero2);
				keyStr = Encoding.UTF8.GetString(array2);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}

		return result;
	}

	public static string ComputeDynamicToken(string urlStr, string bodyStr)
	{
		byte[] array = null;
		byte[] array2 = null;
		int num = 0;
		int num2 = 0;
		IntPtr intPtr = IntPtr.Zero;
		IntPtr intPtr2 = IntPtr.Zero;
		if (!string.IsNullOrEmpty(urlStr))
		{
			array = Encoding.UTF8.GetBytes(urlStr);
			num2 = Marshal.SizeOf<byte>(array[0]) * array.Length;
			intPtr2 = Marshal.AllocHGlobal(num2);
		}

		if (!string.IsNullOrEmpty(bodyStr))
		{
			array2 = Encoding.UTF8.GetBytes(bodyStr);
			num = Marshal.SizeOf<byte>(array2[0]) * array2.Length;
			intPtr = Marshal.AllocHGlobal(num);
		}

		string result = string.Empty;
		try
		{
			if (array2 != null)
			{
				Marshal.Copy(array2, 0, intPtr, array2.Length);
			}

			if (array != null)
			{
				Marshal.Copy(array, 0, intPtr2, array.Length);
			}

			IntPtr zero = IntPtr.Zero;
			int num3 = Native.ComputeDynamicToken(intPtr2, num2, intPtr, num, out zero);
			if (num3 != 0 && zero != IntPtr.Zero)
			{
				byte[] array3 = new byte[num3];
				Marshal.Copy(zero, array3, 0, num3);
				CoreNative.FreeMemory(zero);
				result = Encoding.UTF8.GetString(array3);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr);
			}

			if (intPtr2 != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(intPtr2);
			}
		}

		return result;
	}

	public static string GetH5Token()
	{
		string result = string.Empty;
		try
		{
			IntPtr zero = IntPtr.Zero;
			IntPtr zero2 = IntPtr.Zero;
			int h5Token = Native.GetH5Token(out zero2, out zero);
			if (h5Token != 0 || zero != IntPtr.Zero)
			{
				byte[] array = new byte[h5Token];
				Marshal.Copy(zero, array, 0, h5Token);
				CoreNative.FreeMemory(zero2);
				CoreNative.FreeMemory(zero);
				result = Encoding.UTF8.GetString(array);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("c++ get h5 token" + ex.Message);
		}

		return result;
	}

	public static void FreeMemory(IntPtr ptr)
	{
		if (ptr == IntPtr.Zero)
		{
			return;
		}

		Native.FreeMemory(ptr);
	}
}