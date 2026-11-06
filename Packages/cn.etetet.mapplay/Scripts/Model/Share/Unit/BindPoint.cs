namespace ET
{
    /// <summary>
    /// 角色模型上的骨骼挂载点（BindPoint）类型枚举，用于挂载特效、发射投射物、跟随相机或显示头顶血条。
    /// </summary>
    [EnableClass]
    public enum BindPoint
    {
        None,
        Attack,
        Base,
        Head,
        Bullet,
        LeftHand,
        RightHand,
        Buff,
        HP,
        Hitted,
        JointR,
        JointL,
    }
}