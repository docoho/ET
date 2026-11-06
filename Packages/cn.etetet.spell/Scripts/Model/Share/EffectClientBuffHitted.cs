using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 客户端 Buff 宿主受击时触发的行为树根节点。
    /// </summary>
    //[LabelText("Buff被攻击时触发 (客户端)")]
    //[HideReferenceObjectPicker]
    public class EffectClientBuffHitted: EffectNode
    {
        [BTOutput(typeof(Buff))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Buff = "Buff";
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit = "Unit";
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Caster = "Caster";
        
        // 攻击者
        [BTOutput(typeof(Buff))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Attacker = "Attacker";
    }
}