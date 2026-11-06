namespace ET.Server
{
    /// <summary>
    /// 网关玩家关联的帧同步房间信息组件（挂载在 Gate 服 <see cref="Player"/> 实体上）
    /// </summary>
    [ComponentOf(typeof (Player))]
    public class PlayerRoomComponent: Entity, IAwake
    {
        /// <summary>
        /// 玩家当前所在帧同步房间纤程的 <see cref="ActorId"/>
        /// </summary>
        public ActorId RoomActorId { get; set; }
    }
}