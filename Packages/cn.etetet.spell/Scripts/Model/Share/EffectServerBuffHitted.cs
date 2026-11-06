using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 服务端 Buff 宿主受击时触发的行为树根节点，可用于实现反伤护盾、受击触发被动效果等逻辑。
    /// </summary>
    //[LabelText("Buff被攻击的时候触发 (服务器)")]
    //[HideReferenceObjectPicker]
    public class EffectServerBuffHitted: EffectNode
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