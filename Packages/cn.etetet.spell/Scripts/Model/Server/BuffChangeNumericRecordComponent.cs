using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// Buff 修改数值记录组件，用于在 Buff 移除时自动回滚其对宿主 Unit 造成的属性修改。
    /// </summary>
    [ComponentOf(typeof(Buff))]
    public class BuffChangeNumericRecordComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 属性修改记录列表，偶数索引存储 NumericType，奇数索引存储修改的数值差量。
        /// </summary>
        public List<long> Records = new();
    }
}