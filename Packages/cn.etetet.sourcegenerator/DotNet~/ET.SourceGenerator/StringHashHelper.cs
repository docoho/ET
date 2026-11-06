using System;
using System.Text;

namespace ET.SourceGenerator
{
    /// <summary>
    /// 源码生成器专用的字符串哈希辅助类。
    /// <para>采用与运行时一致的 BKDR Hash 算法计算类型名称的 64 位哈希值，用于生成编译期确定的类型唯一标识（Tag）。</para>
    /// </summary>
    public static class SourceGenStringHashHelper
    {
        /// <summary>
        /// 使用 BKDR 哈希算法计算字符串的 64 位长整型哈希值（分别处理 UTF-16 字符的高 8 位与低 8 位）。
        /// </summary>
        // bkdr hash
        public static long GetLongHashCode(string str)
        {
            const uint seed = 1313; // 31 131 1313 13131 131313 etc..
            
            ulong hash = 0;
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                byte high = (byte)(c >> 8);
                byte low = (byte)(c & byte.MaxValue);
                hash = hash * seed + high;
                hash = hash * seed + low;
            }
            return (long)hash;
        }

        /// <summary>
        /// 计算字符串哈希值对指定正整数 <paramref name="mode"/> 取模的结果。
        /// </summary>
        public static int Mode(string strText, int mode)
        {
            if (mode <= 0)
            {
                throw new Exception($"string mode < 0: {strText} {mode}");
            }
            return (int)((ulong)GetLongHashCode(strText) % (uint)mode);
        }
    }
}
