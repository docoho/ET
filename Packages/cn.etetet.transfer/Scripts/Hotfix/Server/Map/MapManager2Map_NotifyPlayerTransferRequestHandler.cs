namespace ET.Server
{
    /// <summary>
    /// MapManager 通知 Map 将指定玩家传送至新地图（MapManager2Map_NotifyPlayerTransferRequest）的处理器。
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class MapManager2Map_NotifyPlayerTransferRequestHandler: MessageLocationHandler<Unit, MapManager2Map_NotifyPlayerTransferRequest, MapManager2Map_NotifyPlayerTransferResponse>
    {
        /// <summary>
        /// 执行玩家跨地图传送（由于当前已在 <see cref="MessageLocationHandler{E,Request,Response}"/> 的 Mailbox 锁内，因此 <c>needLock</c> 传 <c>false</c>）。
        /// </summary>
        protected override async ETTask Run(Unit unit, MapManager2Map_NotifyPlayerTransferRequest request, MapManager2Map_NotifyPlayerTransferResponse response)
        {
            await TransferHelper.TransferLock(unit, request.MapName, request.MapId, false);
        }
    }
}