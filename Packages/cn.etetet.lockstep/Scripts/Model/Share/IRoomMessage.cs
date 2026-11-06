namespace ET
{
    /// <summary>
    /// 发往帧同步房间纤程（RoomRoot）的 Actor 消息接口（由网关自动填充 <see cref="PlayerId"/> 并路由至玩家所在房间）
    /// </summary>
    public interface IRoomMessage: IMessage
    {
        /// <summary>
        /// 发送该消息的玩家 ID
        /// </summary>
        long PlayerId { get; set; }
    }
}