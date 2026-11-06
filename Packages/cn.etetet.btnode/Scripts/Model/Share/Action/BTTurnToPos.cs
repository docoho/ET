using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 动作节点配置：使施法者朝向黑板中指定的世界坐标位置（Pos）。
    /// </summary>
    public class BTTurnToPos: BTAction
    {
        /// <summary>输入黑板变量名：需要转向的施法者单位</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        /// <summary>输入黑板变量名：朝向的目标世界坐标（float3）</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(float3))]
        public string Pos;
        
        /// <summary>输入黑板变量名：关联的源 Buff</summary>
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
    }
}