using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 服务端 Buff 添加时触发的行为树根节点，用于执行瞬间伤害、属性增减、创建衍生子 Buff 或召唤物等服务端权威逻辑。
    /// </summary>
    //[LabelText("添加Buff效果 (服务器)")]
    //[HideReferenceObjectPicker]
    public class EffectServerBuffAdd: EffectNode
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