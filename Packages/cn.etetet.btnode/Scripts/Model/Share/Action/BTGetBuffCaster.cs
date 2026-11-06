using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 动作节点配置：获取指定 Buff 的施法者（Caster）单位并写入黑板。
    /// </summary>
    public class BTGetBuffCaster: BTAction
    {
        /// <summary>输入黑板变量名：要查询的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        /// <summary>输出黑板变量名：获取到的施法者 Unit</summary>
        [BTOutput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit;
    }
}