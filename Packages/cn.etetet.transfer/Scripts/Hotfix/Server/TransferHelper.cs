using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace ET.Server
{
    /// <summary>
    /// 服务端跨场景/跨进程 Unit 传送辅助类。
    /// <remarks>
    /// 核心流程：
    /// 1. 向 MapManager 申请目标地图副本的 <see cref="ActorId"/>；
    /// 2. 对 Location 发起 <c>LockWithToken</c> 加锁，暂停外部向该 Unit 投递消息；
    /// 3. 将 Unit 从原场景的 AOI 中移除，并将实现了 <see cref="ITransfer"/> 的组件随 Unit 一起迁移到目标 Map（同进程直接传引用，跨进程 Bson 序列化）；
    /// 4. 目标 Map 挂载完成并返回新的 <see cref="ActorId"/> 后，调用 <c>UnLock</c> 更新 Location 路由并释放锁；
    /// 5. 通知 MapManager 玩家已成功进入目标地图。
    /// </remarks>
    /// </summary>
    public static partial class TransferHelper
    {
        /// <summary>
        /// 等待当前帧结束（WaitFrameFinish）后，加 Mailbox 协程锁并执行 Unit 跨地图传送。
        /// </summary>
        public static async ETTask TransferAtFrameFinish(Unit unit, string mapName, int mapId)
        {
            EntityRef<Unit> unitRef = unit;
            await unit.Fiber().WaitFrameFinish();
            unit = unitRef;
            await TransferHelper.TransferLock(unit, mapName, mapId, true);
        }

        /// <summary>
        /// 根据 <paramref name="needLock"/> 决定是否获取 Unit 的 Mailbox 顺序消息协程锁，然后执行传送。
        /// </summary>
        // needLock 是否需要加上Mailbox协程锁
        public static async ETTask TransferLock(Unit unit, string mapName, long mapId, bool needLock)
        {
            if (needLock)
            {
                EntityRef<Unit> unitRef = unit;
                using var _ = await unit.Root().CoroutineLockComponent.Wait(CoroutineLockType.Mailbox, unit.Id);
                unit = unitRef;
                await TransferHelper.Transfer(unit, mapName, mapId);
            }
            else
            {
                await TransferHelper.Transfer(unit, mapName, mapId);
            }
        }

        /// <summary>
        /// 完整传送三步曲：1. 向 MapManager 申请目标地图；2. 迁移 Unit 实体与 Location 锁；3. 通知 MapManager 玩家已进入。
        /// </summary>
        private static async ETTask Transfer(Unit unit, string mapName, long mapId)
        {
            Scene root = unit.Root();
            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> unitRef = unit;
            long unitId = unit.Id;
            
            bool changeScene = TransferSceneHelper.IsChangeScene(unit.Fiber(), unit.Scene().Name, mapName);
            
            
            //1. 申请地图副本
            A2MapManager_GetMapRequest mapRequest = A2MapManager_GetMapRequest.Create();
            mapRequest.MapName = mapName;
            mapRequest.UnitId = unitId;
            mapRequest.MapId = mapId;
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyRef = serviceDiscoveryProxy;
            string mapManagerName = serviceDiscoveryProxy.GetBySceneTypeAndZone(SceneType.MapManager, root.Zone())[0].SceneName;
            A2MapManager_GetMapResponse mapResponse = await serviceDiscoveryProxy.Call(mapManagerName, mapRequest) as A2MapManager_GetMapResponse;
            
            //2. 传送
            unit = unitRef;
            await Transfer(unit, mapResponse.MapActorId, changeScene);


            //3. 通知副本，玩家已经传送进入副本
            A2MapManager_NotifyPlayerAlreadyEnterMapRequest a2MapManagerNotifyPlayerAlreadyEnterMapRequest = A2MapManager_NotifyPlayerAlreadyEnterMapRequest.Create();
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.MapName = mapResponse.MapName;
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.UnitId = unitId;
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.MapId = mapResponse.MapId;
            
            serviceDiscoveryProxy = serviceDiscoveryProxyRef;
            await serviceDiscoveryProxy.Call(mapManagerName, a2MapManagerNotifyPlayerAlreadyEnterMapRequest);
        }

        /// <summary>
        /// 执行底层 Unit 实体跨 Map 迁移：Location 加锁 -> 移除 AOI -> 打包（同进程直接传引用 / 跨进程 Bson 序列化）-> 发送 M2M_UnitTransferRequest -> Location 解锁。
        /// </summary>
        private static async ETTask Transfer(Unit unit, ActorId mapActorId, bool changeScene)
        {
            EntityRef<Unit> unitRef = unit;
            long unitId = unit.Id;
            Log.Debug("start transfer1 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
            
            Scene root = unit.Root();
            EntityRef<Scene> rootRef = root;
            
            ActorId oldActorId = unit.GetActorId();
            //2. location加锁
            long locationLockToken = await root.GetComponent<LocationProxyComponent>()
                    .LockWithToken(LocationType.Unit, unitId, oldActorId);
            
            // 先从AOI中移除
            unit = unitRef;
            unit.RemoveComponent<AOIEntity>();

            ActorId newActorId = default;
            M2M_UnitTransferRequest request = M2M_UnitTransferRequest.Create();
            request.OldActorId = oldActorId;
            request.ChangeScene = changeScene;
            
            // 同一个进程
            root = rootRef;
            if (mapActorId.Address == root.GetSingleton<AddressSingleton>().InnerAddress)
            {
                foreach (Entity entity in unit.Components.Values.ToArray())
                {
                    if (entity is not ITransfer)
                    {
                        unit.RemoveComponent(entity.GetType());
                    }
                    else
                    {
                        // 清理掉不需要序列化的Entity
                        entity.ClearUnSerialized();
                        // 调用SerializeSystem
                        entity.BeginInit();
                    }
                }
                
                request.Unit = unit;
                
                // 这里需要移除Unit，但是不能Dispose，里面会把Unit部分数据Reset
                unit.GetParent<UnitComponent>().Remove(unit.Id, false);
            }
            else // 不同进程
            {
                //3. 拼装消息
                request.UnitBytes = unit.ToBson();
                foreach (Entity entity in unit.Components.Values)
                {
                    if (entity is ITransfer)
                    {
                        request.EntityBytes.Add(entity.ToBson());
                    }
                }
                unit.GetParent<UnitComponent>().Remove(unit.Id);
            }

            Log.Debug("start transfer2 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
            //4. 传送到副本
            root = rootRef;
            M2M_UnitTransferResponse response = await root.GetComponent<MessageSender>().Call(mapActorId, request) as M2M_UnitTransferResponse;
            newActorId = response.NewActorId;
            Log.Debug("start transfer3 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);

            root = rootRef;
            //5. 解锁location，可以接收发给Unit的消息
            await root.GetComponent<LocationProxyComponent>()
                    .UnLock(LocationType.Unit, unitId, oldActorId, newActorId, locationLockToken);
            
            Log.Debug("start transfer4 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
        }
    }
}
