using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 动作节点配置：从技能/Buff 的施法输入参数（BuffCreateInfo）中获取目标点坐标（TargetPos）并写入黑板。
    /// </summary>
    public class BTGetSpellTargetPos: BTAction
    {
        /// <summary>输入黑板变量名：携带施法目标信息的 Buff</summary>
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        /// <summary>输出黑板变量名：获取到的施法目标点坐标（float3）</summary>
        [BTOutput(typeof(float3))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Pos;
    }
}