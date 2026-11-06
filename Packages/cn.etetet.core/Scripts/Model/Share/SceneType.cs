namespace ET
{
    /// <summary>
    /// 场景/纤程职责类型（SceneType）常量分部类定义（标记 <see cref="UniqueIdAttribute"/> 防止重复）。
    /// <remarks>
    /// 各个业务包（如 Login、Gate、Map、Location 等）在各自的分部类中扩展自己的 <c>SceneType</c> 常量。
    /// 事件监听（<c>[Event(SceneType.XXX)]</c>）与消息处理器（<c>[MessageHandler(SceneType.XXX)]</c>）均通过此类型进行过滤。
    /// </remarks>
    /// </summary>
    [UniqueId]
    public static partial class SceneType
    {
        /// <summary>
        /// 通配所有场景类型（当 Handler 或 Event 标记为 <see cref="All"/> 时，在任意类型的 Scene 下都会触发）。
        /// </summary>
        public const int All = 0;
    }
}