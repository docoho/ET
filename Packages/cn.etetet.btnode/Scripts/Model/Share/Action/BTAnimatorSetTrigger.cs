namespace ET
{
    /// <summary>
    /// 表现层动作节点配置：触发目标单位 Animator 状态机中指定动作类型的 Trigger 参数。
    /// </summary>
    public class BTAnimatorSetTrigger: BTAction
    {
        /// <summary>输入黑板变量名：要触发动画的单位</summary>
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        /// <summary>要触发的动作类型枚举</summary>
        public MotionType MotionType;
    }
}