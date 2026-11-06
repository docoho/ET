using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 动作节点配置：从技能/Buff 的施法目标列表中获取第一个目标单位（Unit）并写入黑板。
    /// </summary>
    public class BTGetSpellTargetUnit: BTAction
    {
        /// <summary>输入黑板变量名：携带施法目标信息的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        /// <summary>输出黑板变量名：获取到的单体目标 Unit</summary>
        [BTOutput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit;
    }
}