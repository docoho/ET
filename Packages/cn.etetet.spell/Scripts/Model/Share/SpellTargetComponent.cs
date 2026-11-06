using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 技能目标数据组件，挂载在 BuffData 上下文中，保存当次施法选中的目标单位列表或目标坐标点。
    /// </summary>
    [ComponentOf(typeof(BuffData))]
    public class SpellTargetComponent: Entity, IAwake
    {
        /// <summary>
        /// 选中的目标 Unit 实体 ID 列表。
        /// </summary>
        public List<long> Units { get; } = new();
        /// <summary>
        /// 选中的地面目标坐标点。
        /// </summary>
        public float3 Position { get; set; }
    }
}