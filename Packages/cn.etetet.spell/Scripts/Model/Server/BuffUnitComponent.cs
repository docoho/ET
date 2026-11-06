using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// Buff 召唤/衍生单位记录组件，记录由该 Buff 创建的场景 Unit（如技能飞弹、陷阱、召唤物），并在 Buff 销毁时一并清理。
    /// </summary>
    [ComponentOf(typeof(Buff))]
    public class BuffUnitComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 关联的场景 Unit 实体 ID 列表。
        /// </summary>
        public List<long> UnitIds { get; set; } = new();
    }
}