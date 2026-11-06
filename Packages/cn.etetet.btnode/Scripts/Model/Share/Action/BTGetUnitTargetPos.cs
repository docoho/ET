using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 动作节点配置：获取指定单位当前的世界坐标（Unit.Position）并写入黑板。
    /// </summary>
    public class BTGetUnitTargetPos: BTAction
    {
        /// <summary>输入黑板变量名：要读取位置的单位</summary>
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;

        /// <summary>输出黑板变量名：该单位当前的世界坐标（float3）</summary>
        [BTOutput(typeof(float3))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Pos;
    }
}