using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 每个测试用例都是一个全新的环境，全新的服务器环境
    /// </summary>
    [Invoke(SceneType.TestCase)]
    public class FiberInit_TestCase: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            LogMsg.Instance.AddIgnore(typeof(ServiceHeartbeatRequest));
            LogMsg.Instance.AddIgnore(typeof(ServiceHeartbeatResponse));
            
            Fiber fiber = fiberInit.Fiber;
            Scene root = fiber.Root;
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            
            int process = Options.Instance.Process;
            
            StartProcessConfig startProcessConfig = fiber.GetSingleton<StartProcessConfigCategory>().Get(process);
            
            fiber.AddSingleton<AddressSingleton>();
            // 先看环境变量是否有地址传过来，如果没有，则使用StartProcessConfig的地址跟端口
            AddressHelper.SetInnerIPInnerPortOuterIP(fiber, startProcessConfig);

            fiber.AddSingleton<ProcessFiberAddressSingleton>();
            fiber.AddSingleton<ServiceDiscoveryBootstrapSingleton>();

            await fiber.CreateFiber(0, IdGenerater.Instance.GenerateId(), SceneType.ServiceDiscovery, nameof(SceneType.ServiceDiscovery));

            // TestCase本地ServiceDiscovery Agent Fiber：当前Test fiber下的业务Fiber统一通过此Fiber转发。
            await fiber.CreateFiber(SchedulerType.ThreadPool, 0, IdGenerater.Instance.GenerateId(),
                SceneType.ServiceDiscoveryAgent, $"ServiceDiscoveryAgent@{process}@{Options.Instance.ReplicaIndex}");

            // 根据配置创建纤程
            var scenes = fiber.GetSingleton<StartSceneConfigCategory>().GetByProcess(process);
            
            foreach (StartSceneConfig startConfig in scenes)
            {
                int sceneType = SceneTypeSingleton.Instance.GetSceneType(startConfig.SceneType);
                if (sceneType == SceneType.ServiceDiscovery)
                {
                    continue;
                }

                await fiber.CreateFiber(startConfig.Zone, startConfig.Id, sceneType, startConfig.Name);
            }
        }
    }

    /// <summary>
    /// 监听 Fiber 销毁事件，若销毁的 Scene 上挂载了 TestFiberDatabaseCleanupComponent，则自动清理该测试用例创建的临时数据库。
    /// </summary>
    [Event(SceneType.All)]
    public class FiberDestroyEvent_TestFiberDatabaseCleanup : AEvent<Scene, FiberDestroyEvent>
    {
        protected override async ETTask Run(Scene scene, FiberDestroyEvent args)
        {
            TestFiberDatabaseCleanupComponent cleanupComponent = scene.GetComponent<TestFiberDatabaseCleanupComponent>();
            if (cleanupComponent == null)
            {
                return;
            }

            await cleanupComponent.CleanupAsync(nameof(FiberDestroyEvent));
        }
    }
}
