using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 表现层动作节点配置：创建从施法者指定绑点飞向目标指定绑点的客户端表现假子弹特效。
    /// </summary>
    public class BTCreateFakeBullet : BTAction
    {
        /// <summary>输入黑板变量名：发射子弹的施法者单位</summary>
        [BTInput(typeof(Unit))]
        public string Caster;

        /// <summary>输入黑板变量名：子弹追踪的目标单位</summary>
        [BTInput(typeof(Unit))]
        public string Target;
        
        /// <summary>施法者身上的发射起始绑点</summary>
        public BindPoint CasterBindPoint;
        /// <summary>目标身上的受击命中绑点</summary>
        public BindPoint TargetBindPoint;
        /// <summary>子弹飞行速度</summary>
        public int Speed;

        /// <summary>子弹特效预制体资源名称</summary>
#if UNITY
        [LabelText("特效资源名")]
#endif
        public string EffectName;

        /// <summary>最大存活时间兜底（毫秒，默认 50000ms）</summary>
        public int Duration = 50000;
    }
}
