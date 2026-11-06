using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 服务端 Buff 移除时触发的行为树根节点，可用于在 Buff 结束或被打断时触发二段效果。
    /// </summary>
    //[LabelText("移除BUFF效果 (服务器)")]
    //[HideReferenceObjectPicker]
    public class EffectServerBuffRemove: EffectNode
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