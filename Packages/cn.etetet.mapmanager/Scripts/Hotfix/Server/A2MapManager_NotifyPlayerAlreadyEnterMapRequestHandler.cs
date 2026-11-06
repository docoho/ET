
namespace ET.Server
{
    /// <summary>
    /// 处理通知 MapManager 玩家已成功传送进入地图副本（A2MapManager_NotifyPlayerAlreadyEnterMapRequest）的消息处理器。
    /// </summary>
    [MessageHandler(SceneType.MapManager)]
    public class A2MapManager_NotifyPlayerAlreadyEnterMapRequestHandler: MessageHandler<Scene, A2MapManager_NotifyPlayerAlreadyEnterMapRequest, A2MapManager_NotifyPlayerAlreadyEnterMapResponse>
    {
        /// <summary>
        /// 将玩家从目标地图副本的等待列表移入正式在线玩家集合，并从旧副本玩家集合中移除。
        /// </summary>
        protected override async ETTask Run(Scene root, A2MapManager_NotifyPlayerAlreadyEnterMapRequest request, A2MapManager_NotifyPlayerAlreadyEnterMapResponse response)
        {
            MapManagerComponent mapManagerComponent = root.GetComponent<MapManagerComponent>();
            MapCopy mapCopy = mapManagerComponent.GetMap(request.MapName, request.MapId);
            mapCopy.AddPlayer(request.UnitId);  // 加入地图
            
            MapCopy preMapCopy = mapManagerComponent.GetMap(request.MapName, request.PreMapCopyId);
            if (preMapCopy != null)
            {
                preMapCopy.Players.Remove(request.UnitId);
            }

            await ETTask.CompletedTask;
        }
    }
}