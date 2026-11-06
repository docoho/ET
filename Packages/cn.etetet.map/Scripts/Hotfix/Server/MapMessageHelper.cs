using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端地图消息广播与单发辅助类（通过玩家的 <see cref="UnitGateInfoComponent"/> 将消息经由 Gate 转发给客户端）。
    /// </summary>
    public static partial class MapMessageHelper
    {
        /// <summary>
        /// 向当前能看见 <paramref name="unit"/> 的所有周围玩家广播消息（广播消息禁止使用 <see cref="IPool"/> 对象池实例，以防多次异步发送期间被回收复用）。
        /// </summary>
        private static void Broadcast(Unit unit, IMessage message, bool withSelf = true)
        {
            if (message is IPool)
            {
                throw new System.Exception($"broadcast message can not be IPool: {message.GetType().FullName}");
            }

            Dictionary<long, EntityRef<AOIEntity>> dict = unit.GetBeSeePlayers();
            MessageSender messageSender = unit.Root().GetComponent<MessageSender>();
            
            foreach (AOIEntity u in dict.Values)
            {
                if (!withSelf && u.Id == unit.Id)
                {
                    continue;
                }
                messageSender.Send(u.Unit.GetComponent<UnitGateInfoComponent>().ActorId, message);
            }
        }
        
        /// <summary>
        /// 仅向指定玩家单位自身的客户端发送消息。
        /// </summary>
        private static void SendToClient(Unit unit, IMessage message)
        {
            if (!unit.UnitType.IsSame(UnitType.Player))
            {
                return;
            }
            MessageSender messageSender = unit.Root().GetComponent<MessageSender>();
            messageSender.Send(unit.GetComponent<UnitGateInfoComponent>().ActorId, message);
        }
        
        /// <summary>
        /// 按指定的通知范围类型（<see cref="NoticeType"/>：自己、全体视野广播、除自己外广播、不通知）向客户端发送消息。
        /// </summary>
        public static void NoticeClient(Unit unit, IMessage message, NoticeType noticeType)
        {
            switch (noticeType)
            {
                case NoticeType.Broadcast:
                    Broadcast(unit, message);
                    break;
                case NoticeType.Self:
                    SendToClient(unit, message);
                    break;
                case NoticeType.NoNotice:
                    break;
                case NoticeType.BroadcastWithoutSelf:
                    Broadcast(unit, message, false);
                    break;
            }
        }
    }
}
