using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 地面点坐标目标选择器根节点，用于在客户端展示点选指示器并将选中的坐标传入技能逻辑。
    /// </summary>
    [System.Serializable]
    public class TargetSelectorPosition : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Pos = "Pos";

        /// <summary>
        /// 指示器显示或作用半径。
        /// </summary>
        public int Radius;

#if UNITY
        [LabelText("技能指示器资源名")]
#endif
        public string SpellIndicatorName;
    }
}
