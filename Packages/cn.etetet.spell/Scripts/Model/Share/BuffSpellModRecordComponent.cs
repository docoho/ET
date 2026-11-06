using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// Buff 技能修改器（SpellMod）记录组件，用于在 Buff 移除时自动回滚其对宿主技能属性（如 CD、伤害、消耗等）的修改。
    /// </summary>
    [ComponentOf(typeof(Buff))]
    public class BuffSpellModRecordComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 技能修改记录列表，每 3 个 int 为一组：[SpellConfigId, SpellModType, 修改值]。
        /// </summary>
        public List<int> Records = new();
    }
}