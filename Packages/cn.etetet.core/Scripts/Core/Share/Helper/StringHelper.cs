using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ET
{
	/// <summary>
	/// 字符串与集合格式化辅助扩展工具类。
	/// </summary>
	public static class StringHelper
	{
		/// <summary>
		/// 将字符串的首字母转换为大写并返回新字符串。
		/// </summary>
		public static string CapitalizeFirstLetter(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			string s1 = input[..1];
			string s2 = input[1..];
			return s1.ToUpper() + s2;
		}
		
		/// <summary>
		/// 使用系统默认编码将字符串编码为字节序列。
		/// </summary>
		public static IEnumerable<byte> ToBytes(this string str)
		{
			byte[] byteArray = Encoding.Default.GetBytes(str);
			return byteArray;
		}

		/// <summary>
		/// 使用系统默认编码将字符串编码为字节数组。
		/// </summary>
		public static byte[] ToByteArray(this string str)
		{
			byte[] byteArray = Encoding.Default.GetBytes(str);
			return byteArray;
		}

		/// <summary>
		/// 使用 UTF-8 编码将字符串编码为字节数组。
		/// </summary>
	    public static byte[] ToUtf8(this string str)
	    {
            byte[] byteArray = Encoding.UTF8.GetBytes(str);
            return byteArray;
        }

		/// <summary>
		/// 将偶数长度的十六进制字符串（如 <c>"0A1F"</c>）还原为对应的字节数组。
		/// </summary>
		public static byte[] HexToBytes(this string hexString)
		{
			if (hexString.Length % 2 != 0)
			{
				throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
			}

			var hexAsBytes = new byte[hexString.Length / 2];
			for (int index = 0; index < hexAsBytes.Length; index++)
			{
				string byteValue = "";
				byteValue += hexString[index * 2];
				byteValue += hexString[index * 2 + 1];
				hexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}
			return hexAsBytes;
		}

		/// <summary>
		/// <see cref="string.Format(string, object[])"/> 的链式扩展方法简写。
		/// </summary>
		public static string Fmt(this string text, params object[] args)
		{
			return string.Format(text, args);
		}

		/// <summary>
		/// 将列表中的所有元素用英文逗号拼接为字符串。
		/// </summary>
		public static string ListToString<T>(this List<T> list)
		{
			StringBuilder sb = new StringBuilder();
			foreach (T t in list)
			{
				sb.Append(t);
				sb.Append(",");
			}
			return sb.ToString();
		}
		
		/// <summary>
		/// 将数组格式化为形如 <c>" [a, b, c]"</c> 的可读字符串。
		/// </summary>
		public static string ArrayToString<T>(this T[] args)
		{
			if (args == null)
			{
				return "";
			}

			string argStr = " [";
			for (int arrIndex = 0; arrIndex < args.Length; arrIndex++)
			{
				argStr += args[arrIndex];
				if (arrIndex != args.Length - 1)
				{
					argStr += ", ";
				}
			}

			argStr += "]";
			return argStr;
		}
        
		/// <summary>
		/// 将数组的指定切片范围格式化为形如 <c>" [a, b, c]"</c> 的可读字符串。
		/// </summary>
		public static string ArrayToString<T>(this T[] args, int index, int count)
		{
			if (args == null)
			{
				return "";
			}

			string argStr = " [";
			for (int arrIndex = index; arrIndex < count + index; arrIndex++)
			{
				argStr += args[arrIndex];
				if (arrIndex != args.Length - 1)
				{
					argStr += ", ";
				}
			}

			argStr += "]";
			return argStr;
		}
	}
}