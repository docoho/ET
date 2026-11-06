using System;
using System.Collections.Generic;
using Random = System.Random;

namespace ET
{
    // 支持多线程
    /// <summary>
    /// 线程安全的伪随机数生成工具类（每个线程独立持有一个 <c>[ThreadStatic]</c> 的 <see cref="Random"/> 实例）。
    /// </summary>
    public static class RandomGenerator
    {
        [StaticField]
        [ThreadStatic]
        private static Random random;

        /// <summary>
        /// 获取当前线程专属的 <see cref="Random"/> 实例（首次调用时使用 Guid 哈希与系统 TickCount 异或作为随机种子）。
        /// </summary>
        public static Random GetRandom()
        {
            return random ??= new Random(Guid.NewGuid().GetHashCode() ^ Environment.TickCount);
        }
        
        /// <summary>
        /// 随机生成一个任意位模式的单精度浮点数。
        /// </summary>
        public static float RandFloat()
        {
            int v = GetRandom().Next();
            float f;
            unsafe
            {
                f = *(float*)&v;
            }
            return f;
        }

        /// <summary>
        /// 随机生成一个 64 位无符号整数（<see cref="ulong"/>）。
        /// </summary>
        public static ulong RandUInt64()
        {
            int r1 = RandInt32();
            int r2 = RandInt32();
            return ((ulong)r1 << 32) | (uint)r2;
        }

        /// <summary>
        /// 随机生成一个非负的 32 位有符号整数（<see cref="int"/>）。
        /// </summary>
        public static int RandInt32()
        {
            return GetRandom().Next();
        }

        /// <summary>
        /// 随机生成一个 32 位无符号整数（<see cref="uint"/>）。
        /// </summary>
        public static uint RandUInt32()
        {
            return (uint) GetRandom().Next();
        }

        /// <summary>
        /// 随机生成一个 64 位有符号整数（<see cref="long"/>）。
        /// </summary>
        public static long RandInt64()
        {
            uint r1 = RandUInt32();
            uint r2 = RandUInt32();
            return (long)(((ulong)r1 << 32) | r2);
        }

        /// <summary>
        /// 获取lower与Upper之间的随机数,包含下限，不包含上限
        /// </summary>
        /// <param name="lower"></param>
        /// <param name="upper"></param>
        /// <returns></returns>
        public static int RandomNumber(int lower, int upper)
        {
            int value = GetRandom().Next(lower, upper);
            return value;
        }

        /// <summary>
        /// 以 50% 的概率随机返回 <c>true</c> 或 <c>false</c>。
        /// </summary>
        public static bool RandomBool()
        {
            return GetRandom().Next(2) == 0;
        }

        /// <summary>
        /// 从数组中随机抽取一个元素。
        /// </summary>
        public static T RandomArray<T>(T[] array)
        {
            return array[RandomNumber(0, array.Length)];
        }

        /// <summary>
        /// 从列表中随机抽取一个元素。
        /// </summary>
        public static T RandomArray<T>(List<T> array)
        {
            return array[RandomNumber(0, array.Count)];
        }

        /// <summary>
        /// 打乱数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr">要打乱的数组</param>
        public static void BreakRank<T>(List<T> arr)
        {
            if (arr == null || arr.Count < 2)
            {
                return;
            }

            for (int i = 0; i < arr.Count; i++)
            {
                int index = GetRandom().Next(0, arr.Count);
                (arr[index], arr[i]) = (arr[i], arr[index]);
            }
        }

        /// <summary>
        /// 生成一个位于 <c>[0.0f, 1.0f)</c> 区间内的随机浮点数（精度为百万分之一）。
        /// </summary>
        public static float RandFloat01()
        {
            int a = RandomNumber(0, 1000000);
            return a / 1000000f;
        }
    }
}