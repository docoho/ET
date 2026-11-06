namespace ET
{
    /// <summary>
    /// 技能消耗行为树根节点，在施法流程中会被调用两次（第一次 Check=true 仅检查是否满足消耗，第二次 Check=false 真正扣除资源）。
    /// </summary>
    public class CostNode: BTRoot
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Buff))]
        public string Buff = "Buff";
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = "Unit";

        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(bool))]
        public string Check = "Check";
    }
}