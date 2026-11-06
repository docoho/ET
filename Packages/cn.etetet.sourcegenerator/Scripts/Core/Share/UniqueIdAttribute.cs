using System;

namespace ET
{
    /// <summary>
    /// 唯一Id标签（由 <c>UniqueIdAnalyzer</c> 在编译期静态检查）。
    /// 使用此标签标记的类（通常是跨多个包的 <c>partial static class</c>，如 <c>PackageType</c>、<c>SceneType</c>、<c>Opcode</c>、<c>ErrorCode</c>），
    /// 分析器会自动汇总全工程所有同名分部类内部的 <c>const int</c> / <c>const ushort</c> 常量成员，检测是否存在重复值或超出 <c>[Min, Max]</c> 范围的值。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UniqueIdAttribute : Attribute
    {
        /// <summary>允许的最小 ID 值（含）。</summary>
        public int Min;

        /// <summary>允许的最大 ID 值（含）。</summary>
        public int Max;
        
        public UniqueIdAttribute(int min = int.MinValue, int max = int.MaxValue)
        {
            this.Min = min;
            this.Max = max;
        }
    }
}

