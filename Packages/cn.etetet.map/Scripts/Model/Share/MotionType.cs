namespace ET
{
    /// <summary>
    /// 角色动作/动画状态枚举（对应 Animator 状态机中的动作类型）。
    /// </summary>
    public enum MotionType
    {
        /// <summary>
        /// 无动作。
        /// </summary>
        None,
        /// <summary>
        /// 移动速度参数。
        /// </summary>
        MoveSpeed,
        /// <summary>
        /// 待机状态。
        /// </summary>
        Idle,
        /// <summary>
        /// 奔跑状态。
        /// </summary>
        Run,
        /// <summary>
        /// 近战攻击动作 1。
        /// </summary>
        MeleeAttack1,
        /// <summary>
        /// 近战攻击动作 2。
        /// </summary>
        MeleeAttack2,
        /// <summary>
        /// 近战攻击动作 3。
        /// </summary>
        MeleeAttack3,
        /// <summary>
        /// 受击动作。
        /// </summary>
        GetHit,
        /// <summary>
        /// 死亡动作。
        /// </summary>
        Dead,
        /// <summary>
        /// 指向性施法动作。
        /// </summary>
        SpellCastDirected,
        /// <summary>
        /// 指向性施法准备动作。
        /// </summary>
        ReadySpellDirected,
        /// <summary>
        /// 全向/范围施法动作。
        /// </summary>
        SpellCastOmni,
        /// <summary>
        /// 全向/范围施法准备动作。
        /// </summary>
        ReadySpellOmni,
        /// <summary>
        /// 驯服引导动作。
        /// </summary>
        Tame,
    }
}