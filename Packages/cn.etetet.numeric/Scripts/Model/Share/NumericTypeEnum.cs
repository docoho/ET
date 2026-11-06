using System;

namespace ET
{
    /// <summary>
    /// <see cref="NumericType"/> 枚举值与名称映射单例（用于调试、编辑器下拉列表与反射查询）。
    /// </summary>
    public class NumericTypeEnum: EnumSingleton<NumericTypeEnum>, ISingletonAwake
    {
        /// <summary>
        /// 初始化并缓存所有 <see cref="NumericType"/> 枚举项的整型值与字符串名称。
        /// </summary>
        public void Awake()
        {
            foreach (NumericType numericType in Enum.GetValues(typeof(NumericType)))
            {
                this.enumValueString.Add((int)numericType, numericType.ToString());
            }
        }
    }
}
