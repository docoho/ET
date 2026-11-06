using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 地图场景（SceneType.Map）纤程初始化回调：挂载 AOI、Unit、ActorLocation 组件，加载 NavMesh 寻路网格，按配置表刷出初始地图怪物/NPC，并订阅 Location 与 MapManager 服务。
    /// </summary>
    [Invoke(SceneType.Map)]
    public class FiberInit_Map: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            EntityRef<Scene> rootRef = root;
            
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            UnitComponent unitComponent = root.AddComponent<UnitComponent>();
            
            root.AddComponent<AOIManagerComponent>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();
            
            EntityRef<UnitComponent> unitComponentRef = unitComponent;

            string mapName = root.Name.GetSceneConfigName();

            if (mapName != "GateMap")
            {
                // 加载场景寻路数据
                await NavmeshComponent.Instance.Load(mapName);
            }

            root = rootRef;
            unitComponent = unitComponentRef;

            // 遍历地图预设单位表（MapUnitConfigCategory），生成属于当前地图的怪物、NPC 等初始单位
            foreach (var kv in root.Fiber().GetSingleton<MapUnitConfigCategory>().GetAll())
            {
                if (mapName != kv.Value.MapName)
                {
                    continue;
                }
                Unit unit = UnitFactory.Create(root, kv.Key, kv.Value.UnitConfigId);
                unitComponent.Add(unit);
            }
            
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;
            await serviceDiscoveryProxy.RegisterToServiceDiscovery();
            // 订阅location,并未注册Map
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("Location", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Location)},
                });
            
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("MapManager", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.MapManager)},
                });
        }
    }
}
