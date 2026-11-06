namespace ET
{
    /// <summary>
    /// 条件节点配置：检查指定单位当前的仇恨组件（ThreatComponent）中是否存在仇恨目标。
    /// </summary>
    public class BTHasThreat: BTCondition
    {
        /// <summary>输入黑板变量名：要检查仇恨列表的单位</summary>
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
    }
}
