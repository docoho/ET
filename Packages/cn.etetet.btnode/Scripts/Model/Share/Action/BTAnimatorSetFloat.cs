namespace ET
{
    /// <summary>
    /// 表现层动作节点配置：设置目标单位 Animator 状态机中指定动作类型的 float 参数。
    /// </summary>
    public class BTAnimatorSetFloat: BTAction
    {
        /// <summary>输入黑板变量名：要设置动画参数的单位</summary>
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
        
        /// <summary>动画参数对应的动作类型枚举</summary>
        public MotionType MotionType;
        /// <summary>要设置的浮点数值</summary>
        public float Value;
    }
}