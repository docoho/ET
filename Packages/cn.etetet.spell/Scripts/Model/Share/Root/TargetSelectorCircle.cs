using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 圆形范围单位选择器根节点，以施法者或目标点为中心筛选指定半径和单位类型的目标列表。
    /// </summary>
    [System.Serializable]
    public class TargetSelectorCircle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(List<long>))]
        public string Units = "Units";
        
        /// <summary>
        /// 圆形作用半径。
        /// </summary>
        public int Radius;
        /// <summary>
        /// 筛选的目标单位类型。
        /// </summary>
        public UnitType UnitType;

#if UNITY
        [LabelText("技能指示器资源名")]
#endif
        public string SpellIndicatorName;
    }
}
