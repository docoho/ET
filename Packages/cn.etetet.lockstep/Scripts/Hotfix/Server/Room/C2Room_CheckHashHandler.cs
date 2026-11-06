namespace ET.Server
{
    /// <summary>
    /// 客户端上报帧世界状态哈希校验请求（<see cref="C2Room_CheckHash"/>）处理器
    /// </summary>
    [MessageHandler(SceneType.RoomRoot)]
    public class C2Room_CheckHashHandler: MessageHandler<Scene, C2Room_CheckHash>
    {
        /// <summary>
        /// 对比客户端上报的指定帧状态哈希与服务端权威帧快照哈希；若不一致则下发服务端完整快照以供定位不同步问题
        /// </summary>
        /// <param name="root">房间根场景（RoomRoot）</param>
        /// <param name="message">客户端上报的帧号与世界哈希消息</param>
        protected override async ETTask Run(Scene root, C2Room_CheckHash message)
        {
            Room room = root.GetComponent<Room>();
            long hash = room.FrameBuffer.GetHash(message.Frame);
            if (message.Hash != hash)
            {
                byte[] bytes = room.FrameBuffer.Snapshot(message.Frame).ToArray();
                Room2C_CheckHashFail room2CCheckHashFail = Room2C_CheckHashFail.Create();
                room2CCheckHashFail.Frame = message.Frame;
                room2CCheckHashFail.LSWorldBytes = bytes;
                room.Root().GetComponent<MessageLocationSenderComponent>().Get(LocationType.GateSession).Send(message.PlayerId, room2CCheckHashFail);
            }
            await ETTask.CompletedTask;
        }
    }
}