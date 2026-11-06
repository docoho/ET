using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 技能目标选择器行为树根节点抽象基类，定义了施法距离限制及默认输出到行为树环境的黑板键名。
    /// </summary>
    public abstract class TargetSelector : BTRoot
    {
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Buff))]
        public string Buff = "Buff";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Caster = "Caster";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Owner = "Owner";

        /// <summary>
        /// 最大施法距离。
        /// </summary>
        public int MaxDistance;

        /// <summary>
        /// 最小施法距离。
        /// </summary>
        public int MinDistance;
    }
    

    

    

    

    

}