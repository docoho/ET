using MongoDB.Bson.Serialization.Attributes;
using Unity.Mathematics;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 表现层节点配置：在世界空间的指定坐标位置上创建一次性表现特效。
    /// </summary>
    public class BTCreateEffectOnPos : BTNode
    {
        /// <summary>输入黑板变量名：特效生成的世界坐标（float3）</summary>
        [BTInput(typeof(float3))]
        public string Pos;
        
        /// <summary>特效预制体资源名称</summary>
#if UNITY
        [LabelText("特效资源名")]
#endif
        public string EffectName;

        /// <summary>特效持续时间（毫秒，默认 5000ms）</summary>
        public int Duration = 5000;
    }
}
