using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 服务端 Buff 周期性（Tick）触发的行为树根节点，既可用于 DOT 持续伤害/治疗，也可在勾选 Override 时作为单位的 AI 行为树调度入口。
    /// </summary>
    //[LabelText("效果Buff每帧 (服务器)")]
    //[HideReferenceObjectPicker]
    public class EffectServerBuffTick: EffectNode
    {
        [BTOutput(typeof(Buff))]
        //[ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = "Buff";
        
#if UNITY_EDITOR
        [HideIf("@true")]
        [BsonIgnore]
        // 用来允许BTCoroutine作为孩子
        [BTOutput(typeof(BTCoroutine))]
        [BoxGroup("输出参数")]
        public string RootMustBeBuffTick = "RootMustBeBuffTick";
#endif

        /// <summary>
        /// 若为 true，则该 Buff Tick 会被压入 Unit 的 BTCoroutineComponent（AI 栈）中，接管并暂停低优先级的 AI Buff。
        /// </summary>
        public bool Override;
    }
}