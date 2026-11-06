using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 表现层节点配置：在目标单位的指定绑点上创建一次性表现特效（按持续时间自动销毁）。
    /// </summary>
    public class BTCreateEffect : BTNode
    {
        /// <summary>输入黑板变量名：挂载特效的目标单位</summary>
        [BTInput(typeof(Unit))]
        public string Unit;
        
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
