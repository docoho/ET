namespace ET
{
    /// <summary>
    /// 定时器回调类型扩展定义（Move 移动模块）。
    /// </summary>
    public static partial class TimerInvokeType
    {
        /// <summary>
        /// <see cref="MoveComponent"/> 每帧位置插值定时器回调类型。
        /// </summary>
        public const int MoveTimer = PackageType.Move * 1000 + 1;

        /// <summary>
        /// <see cref="TurnComponent"/> 每帧旋转插值定时器回调类型。
        /// </summary>
        public const int TurnTimer = PackageType.Move * 1000 + 2;
    }
}