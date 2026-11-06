using System;

namespace ET
{
    /// <summary>
    /// 64 位 <see cref="Fiber.Id"/> 编解码工具类。
    /// <para>
    /// 编码规则：高 32 位存储区服编号（<c>Zone</c>），低 32 位存储局部槽位号（<c>LocalSlot</c>）。
    /// 其中 LocalSlot 划分为三个区间：
    /// - <c>[1, 99999]</c>：配置表固定槽位区间（<see cref="ConfigLocalSlotMax"/>）；
    /// - <c>[100000, MaxLocalSlot - 16]</c>：运行时动态分配槽位区间（<see cref="DynamicLocalSlotStart"/>）；
    /// - <c>[MaxLocalSlot - 15, MaxLocalSlot]</c>：系统保留特殊槽位区间（<see cref="ReservedLocalSlotStart"/>）。
    /// </para>
    /// </summary>
    public static class FiberIdHelper
    {
        public const int ZoneBits = 32;
        public const int LocalSlotBits = 32;
        public const int MaxZone = int.MaxValue;
        public const int MaxLocalSlot = int.MaxValue;
        public const int ConfigLocalSlotMax = 99999;
        public const int DynamicLocalSlotStart = 100000;
        public const int ReservedLocalSlotStart = MaxLocalSlot - 15;

        /// <summary>
        /// 将 32 位 <paramref name="zone"/> 与 32 位 <paramref name="localSlot"/> 编码合成为 64 位 <c>FiberId</c>。
        /// </summary>
        public static long Encode(int zone, int localSlot)
        {
            ValidateZone(zone);
            ValidateLocalSlot(localSlot);
            ulong encoded = ((ulong)(uint)zone << LocalSlotBits) | (uint)localSlot;
            return unchecked((long)encoded);
        }

        /// <summary>
        /// 从 64 位 <paramref name="fiberId"/> 的高 32 位解码出区服编号（Zone）。
        /// </summary>
        public static int DecodeZone(long fiberId)
        {
            return (int)((ulong)fiberId >> LocalSlotBits);
        }

        /// <summary>
        /// 从 64 位 <paramref name="fiberId"/> 的低 32 位解码出局部槽位号（LocalSlot）。
        /// </summary>
        public static int DecodeLocalSlot(long fiberId)
        {
            return (int)((ulong)fiberId & uint.MaxValue);
        }

        /// <summary>
        /// 判断槽位号是否属于系统保留区间。
        /// </summary>
        public static bool IsReservedLocalSlot(int localSlot)
        {
            return localSlot >= ReservedLocalSlotStart && localSlot <= MaxLocalSlot;
        }

        /// <summary>
        /// 校验 <paramref name="zone"/> 是否在合法非负范围内。
        /// </summary>
        public static void ValidateZone(int zone)
        {
            if ((uint)zone > MaxZone)
            {
                throw new ArgumentOutOfRangeException(nameof(zone), zone, $"fiber zone must be in [0, {MaxZone}]");
            }
        }

        /// <summary>
        /// 校验 <paramref name="localSlot"/> 是否在合法正整数范围内（<c>[1, int.MaxValue]</c>）。
        /// </summary>
        public static void ValidateLocalSlot(int localSlot)
        {
            if ((uint)localSlot > MaxLocalSlot || localSlot == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(localSlot), localSlot,
                    $"fiber local slot must be in [1, {MaxLocalSlot}]");
            }
        }

        /// <summary>
        /// 校验固定槽位号是否未侵入动态分配区间。
        /// </summary>
        public static void ValidateFixedLocalSlot(int localSlot)
        {
            ValidateLocalSlot(localSlot);
            if (localSlot >= DynamicLocalSlotStart && localSlot < ReservedLocalSlotStart)
            {
                throw new ArgumentOutOfRangeException(nameof(localSlot), localSlot,
                    $"fixed fiber local slot cannot be in dynamic range [{DynamicLocalSlotStart}, {ReservedLocalSlotStart - 1}]");
            }
        }
    }
}
