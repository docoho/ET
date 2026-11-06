namespace ET.Server
{
    /// <summary>
    /// 帧同步房间服务端消息广播辅助类
    /// </summary>
    public static partial class RoomMessageHelper
    {
        /// <summary>
        /// 向房间内所有在线玩家的网关会话（GateSession）广播消息
        /// </summary>
        /// <param name="room">目标帧同步房间实例</param>
        /// <param name="message">要广播的网络消息</param>
        public static void BroadCast(Room room, IMessage message)
        {
            // 广播的消息不能被池回收
            (message as MessageObject).IsFromPool = false;
            
            RoomServerComponent roomServerComponent = room.GetComponent<RoomServerComponent>();

            MessageLocationSenderComponent messageLocationSenderComponent = room.Root().GetComponent<MessageLocationSenderComponent>();
            foreach (var kv in roomServerComponent.Children)
            {
                RoomPlayer roomPlayer = kv.Value as RoomPlayer;

                if (!roomPlayer.IsOnline)
                {
                    continue;
                }

                messageLocationSenderComponent.Get(LocationType.GateSession).Send(roomPlayer.Id, message);
            }
        }
    }
}