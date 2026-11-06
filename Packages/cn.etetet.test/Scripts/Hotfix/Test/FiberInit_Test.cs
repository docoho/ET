using System;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试主控场景（SceneType.Test）Fiber 初始化处理器，挂载基础通信/定时/控制台组件并初始化 TestDispatcher 与区服分配器。
    /// </summary>
    [Invoke(SceneType.Test)]
    public class FiberInit_Test: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Fiber fiber = fiberInit.Fiber;
            Scene root = fiber.Root;
            
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ObjectWait>();
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<ConsoleComponent>();

            EnsureGlobalConfigSingletons(fiber);
            World.Instance.AddSingleton<AddressSingleton>();
            World.Instance.AddSingleton<TestDispatcher>();
            root.AddComponent<TestZoneAllocatorComponent>();
            EnsureAddressSingletonReady(fiber);

            await ETTask.CompletedTask;
        }

        private static void EnsureGlobalConfigSingletons(Fiber fiber)
        {
            EnsureStartMachineConfigCategory(fiber);
            EnsureStartProcessConfigCategory(fiber);
            EnsureStartSceneConfigCategory(fiber);
            EnsureStartZoneConfigCategory(fiber);
        }

        private static void EnsureStartMachineConfigCategory(Fiber fiber)
        {
            StartMachineConfigCategory category = fiber.GetSingleton<StartMachineConfigCategory>();
            if (category != null)
            {
                return;
            }
            
            throw new Exception("StartMachineConfigCategory is not initialized before Test scene start.");
        }

        private static void EnsureStartProcessConfigCategory(Fiber fiber)
        {
            StartProcessConfigCategory category = fiber.GetSingleton<StartProcessConfigCategory>();
            if (category != null)
            {
                return;
            }
            
            throw new Exception("StartProcessConfigCategory is not initialized before Test scene start.");
        }

        private static void EnsureStartSceneConfigCategory(Fiber fiber)
        {
            StartSceneConfigCategory category = fiber.GetSingleton<StartSceneConfigCategory>();
            if (category != null)
            {
                return;
            }
            
            throw new Exception("StartSceneConfigCategory is not initialized before Test scene start.");
        }

        private static void EnsureStartZoneConfigCategory(Fiber fiber)
        {
            StartZoneConfigCategory category = fiber.GetSingleton<StartZoneConfigCategory>();
            if (category != null)
            {
                return;
            }
            
            throw new Exception("StartZoneConfigCategory is not initialized before Test scene start.");
        }

        private static void EnsureAddressSingletonReady(Fiber fiber)
        {
            AddressSingleton addressSingleton = fiber.GetSingleton<AddressSingleton>();
            if (addressSingleton == null)
            {
                addressSingleton = World.Instance.AddSingleton<AddressSingleton>();
            }

            if (!string.IsNullOrEmpty(addressSingleton.InnerIP) &&
                !string.IsNullOrEmpty(addressSingleton.OuterIP) &&
                addressSingleton.InnerPort > 0)
            {
                return;
            }

            StartProcessConfig startProcessConfig = fiber.GetSingleton<StartProcessConfigCategory>()?.Get(Options.Instance.Process);
            if (startProcessConfig == null)
            {
                throw new Exception($"test address init failed: process={Options.Instance.Process}");
            }

            StartMachineConfig startMachineConfig = fiber.GetSingleton<StartMachineConfigCategory>()?.Get(startProcessConfig.MachineId);
            addressSingleton.InnerIP ??= startMachineConfig?.InnerIP;
            addressSingleton.OuterIP ??= startMachineConfig?.OuterIP;
            addressSingleton.InnerPort = addressSingleton.InnerPort > 0 ? addressSingleton.InnerPort : startProcessConfig.Port;
        }
    }
}
