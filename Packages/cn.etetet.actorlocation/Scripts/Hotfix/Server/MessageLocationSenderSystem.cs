using System;

namespace ET.Server
{
    /// <summary>
    /// 单个目标实体的位置消息发送缓存项系统（<see cref="MessageLocationSender"/>）。
    /// 记录目标实体的缓存 <see cref="ActorId"/> 以及最后一次收发消息的时间戳。
    /// </summary>
    [EntitySystemOf(typeof(MessageLocationSender))]
    public static partial class MessageLocationSenderSystem
    {
        [EntitySystem]
        private static void Awake(this MessageLocationSender self)
        {
            self.LastSendOrRecvTime = self.GetSingleton<TimeInfo>().ServerNow();
            self.ActorId = default;
        }
        
        [EntitySystem]
        private static void Destroy(this MessageLocationSender self)
        {
            Log.Debug($"actor location remove: {self.Id}");
            self.LastSendOrRecvTime = 0;
            self.ActorId = default;
        }
    }
}