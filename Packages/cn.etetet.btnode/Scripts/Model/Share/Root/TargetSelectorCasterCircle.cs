using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 技能目标选择器根节点配置：以施法者自身为圆心、指定半径（Radius）的圆形区域目标选择器。
    /// </summary>
    [System.Serializable]
    public class TargetSelectorCasterCircle : TargetSelector
    {
        /// <summary>输出黑板变量名：选中的单个目标单位（默认 "Unit"）</summary>
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";
        
        /// <summary>输出黑板变量名：选中的圆形范围内所有目标单位列表（默认 "Units"）</summary>
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(List<EntityRef<Unit>>))]
        public string Units = "Units";
       
        /// <summary>圆形索敌半径</summary>
        public int Radius;
        
        /// <summary>允许命中的目标单位类型掩码</summary>
        public UnitType UnitType;
    }
}