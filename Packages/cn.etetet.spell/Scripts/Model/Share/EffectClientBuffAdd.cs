using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 客户端 Buff 添加时触发的行为树根节点，通常用于播放技能动作、受击特效或音效。
    /// </summary>
    //[LabelText("添加Buff效果 (客户端)")]
    //[HideReferenceObjectPicker]
    public class EffectClientBuffAdd: EffectNode
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