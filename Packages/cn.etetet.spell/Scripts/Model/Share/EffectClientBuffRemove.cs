using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 客户端 Buff 移除时触发的行为树根节点，通常用于清理客户端表现特效或恢复动画状态。
    /// </summary>
    //[LabelText("移除Buff效果 (客户端)")]
    //[HideReferenceObjectPicker]
    public class EffectClientBuffRemove: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = "Buff";
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit = "Unit";
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Caster = "Caster";
    }
}