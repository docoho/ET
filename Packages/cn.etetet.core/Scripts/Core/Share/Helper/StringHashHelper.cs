using System;
using System.Text;

namespace ET
{
    /// <summary>
    /// 字符串确定性哈希计算工具类。
    /// <remarks>
    /// .NET 自带的 <see cref="string.GetHashCode()"/> 在不同进程或不同平台间是随机化的（出于防哈希碰撞攻击考虑）；
    /// 因此需要跨进程稳定一致的字符串哈希（如 Type 全名转长整型TypeHash、一致性分片取模）时，必须使用此类提供的确定性 BKDR 算法。
    /// </remarks>
    /// </summary>
    public static class StringHashHelper
    {
        // bkdr hash
        /// <summary>
        /// 使用 BKDR 哈希算法（种子为 1313）计算字符串的 64 位确定性哈希值。
        /// </summary>
        public static long GetLongHashCode(this string str)
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
        /// 计算字符串的 64 位确定性哈希并对正整数 <paramref name="mode"/> 取模（常用于账号路由分片）。
        /// </summary>
        public static int Mode(this string strText, int mode)
        {
            if (mode <= 0)
            {
                throw new Exception($"string mode < 0: {strText} {mode}");
            }
            return (int)((ulong)strText.GetLongHashCode() % (uint)mode);
        }
    }
}