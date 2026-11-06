
namespace ET.Server
{
    /// <summary>
    /// 处理向 MapManager 申请可用地图副本或分线（A2MapManager_GetMapRequest）的消息处理器。
    /// </summary>
    [MessageHandler(SceneType.MapManager)]
    public class A2MapManager_GetMapRequestHandler: MessageHandler<Scene, A2MapManager_GetMapRequest, A2MapManager_GetMapResponse>
    {
        /// <summary>
        /// 获取或按需创建目标地图副本，将玩家加入该副本的等待进入列表，并返回地图副本的 <see cref="ActorId"/>。
        /// </summary>
        protected override async ETTask Run(Scene root, A2MapManager_GetMapRequest request, A2MapManager_GetMapResponse response)
        {
            EntityRef<Scene> rootRef = root;
            MapManagerComponent mapManagerComponent = root.GetComponent<MapManagerComponent>();
            MapCopy mapCopy = await mapManagerComponent.GetMapAsync(request.MapName, request.MapId);

            root = rootRef;
            mapCopy.AddWaitPlayer(request.UnitId);  // 加入等待进入列表
            response.MapName = request.MapName;
            response.MapId = mapCopy.Id;
            response.MapActorId = new ActorId(root.GetSingleton<AddressSingleton>().InnerAddress, new FiberInstanceId(mapCopy.FiberId, 1));
        }
    }
}
