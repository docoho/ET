namespace ET.Server
{
    /// <summary>
    /// 帧同步服务端房间内的参赛玩家实体（作为 <see cref="RoomServerComponent"/> 的子实体，ID 为玩家 ID）
    /// </summary>
    [ChildOf(typeof (RoomServerComponent))]
    public class RoomPlayer: Entity, IAwake
    {
        /// <summary>
        /// 玩家客户端场景加载进度（0~100）
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// 玩家当前是否在线（用于广播时过滤掉线玩家）
        /// </summary>
        public bool IsOnline { get; set; } = true;
    }
}