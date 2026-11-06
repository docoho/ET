namespace ET.Client
{
    /// <summary>
    /// 客户端接收帧世界状态哈希校验失败通知（<see cref="Room2C_CheckHashFail"/>）的处理器
    /// </summary>
    [MessageHandler(SceneType.LockStep)]
    public class Room2C_CheckHashFailHandler: MessageHandler<Scene, Room2C_CheckHashFail>
    {
        /// <summary>
        /// 反序列化服务端下发的权威 <see cref="LSWorld"/> 快照并与客户端同帧本地快照一同输出为 JSON 日志，便于开发者逐字段比对定位帧同步不一致原因
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="message">哈希校验失败消息（包含帧号与服务端世界快照字节流）</param>
        protected override async ETTask Run(Scene root, Room2C_CheckHashFail message)
        {
            LSWorld serverWorld = MemoryPackHelper.Deserialize(typeof(LSWorld), message.LSWorldBytes, 0, message.LSWorldBytes.Length) as LSWorld;
            using (root.AddChild(serverWorld))
            {
                Log.Debug($"check hash fail, server: {message.Frame} {serverWorld.ToJson()}");
            }

            Room room = root.GetComponent<Room>();
            LSWorld clientWorld = room.GetLSWorld(SceneType.LockStepClient, message.Frame);
            using (root.AddChild(clientWorld))
            {
                Log.Debug($"check hash fail, client: {message.Frame} {clientWorld.ToJson()}");
            }
            
            await ETTask.CompletedTask;
        }
    }
}