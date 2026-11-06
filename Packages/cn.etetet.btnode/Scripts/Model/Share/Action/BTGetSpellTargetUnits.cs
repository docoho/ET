using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 动作节点配置：从技能/Buff 的施法输入参数中获取所有目标单位 ID 列表（List&lt;long&gt;）并写入黑板。
    /// </summary>
    public class BTGetSpellTargetUnits: BTAction
    {
        /// <summary>输入黑板变量名：携带施法目标信息的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        /// <summary>输出黑板变量名：获取到的目标单位 ID 列表</summary>
        [BTOutput(typeof(List<long>))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Units;
    }
}