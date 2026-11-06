using System.IO;

namespace ET.Server
{
    /// <summary>
    /// 单个目标实体的位置消息发送器（缓存目标实体的最新 <see cref="ActorId"/>，当知道对方的实体 Id 时通过它发送 Actor Location 消息）。
    /// </summary>
    // 知道对方的Id，使用这个类发actor消息
    [ChildOf(typeof(MessageLocationSenderOneType))]
    public class MessageLocationSender: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 缓存的目标实体当前所在的 ActorId。
        /// </summary>
        public ActorId ActorId;

        /// <summary>
        /// 最近一次成功接收或发送消息的时间戳（毫秒），用于空闲超时回收。
        /// </summary>
        // 最近接收或者发送消息的时间
        public long LastSendOrRecvTime;
    }
}