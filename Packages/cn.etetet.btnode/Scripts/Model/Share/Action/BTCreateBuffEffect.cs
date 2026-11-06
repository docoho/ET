using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 表现层节点配置：在目标单位指定绑点创建与 Buff 生命周期绑定的表现特效（Buff 移除时自动销毁）。
    /// </summary>
    public class BTCreateBuffEffect : BTNode
    {
        /// <summary>输入黑板变量名：挂载特效的目标单位</summary>
        [BTInput(typeof(Unit))]
        public string Unit;
        
        /// <summary>输入黑板变量名：关联的 Buff 实例（特效挂在该 Buff 下，随 Buff 一并销毁）</summary>
        [BTInput(typeof(Buff))]
        public string Buff;
        
        /// <summary>角色模型上的特效挂载点类型</summary>
        public BindPoint BindPoint;

        /// <summary>特效预制体资源名称</summary>
#if UNITY
        [LabelText("特效资源名")]
#endif
        public string EffectName;

        /// <summary>特效持续时间（毫秒，默认 5000ms）</summary>
        public int Duration = 5000;
    }
}
