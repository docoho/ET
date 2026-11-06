using System;
using ET.Client;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 集成测试辅助工具类，提供在测试环境中一键创建客户端 Robot Fiber 并自动完成登录与进入地图、获取服务端 Map Fiber 及服务端 Unit 实体的便捷方法。
    /// </summary>
    public static class TestHelper
    {
        /// <summary>
        /// 在测试 Fiber 下创建客户端机器人 Fiber，自动执行登录与进入地图流程
        /// </summary>
        public static async ETTask<Fiber> CreateRobot(Fiber fiber, string robotName)
        {
            string routerManagerAddress = GetRouterManagerAddress(fiber);
            Fiber robot = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), SceneType.Client, robotName);
            Scene root = robot.Root;
            EntityRef<Scene> rootRef = root;
            await LoginHelper.Login(root, routerManagerAddress, robotName, "");
            root = rootRef;
            await EnterMapHelper.EnterMapAsync(root);
            return robot;
        }

        /// <summary>
        /// 从当前测试环境中的 RouterManager Fiber 获取路由管理服监听地址
        /// </summary>
        public static string GetRouterManagerAddress(Fiber fiber)
        {
            Fiber routerManagerFiber = fiber.GetFiber("RouterManager");
            if (routerManagerFiber == null)
            {
                throw new Exception($"RouterManager fiber not found under fiber: {fiber?.Name}");
            }

            string address = routerManagerFiber.Root.GetComponent<RouterManagerAddressComponent>()?.Address;
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new Exception("RouterManagerAddressComponent is missing on RouterManager scene.");
            }

            return address;
        }
        
        /// <summary>
        /// 根据客户端机器人当前所在的地图场景名称，在服务端 MapManager 下查找对应的服务端 Map Fiber
        /// </summary>
        public static Fiber GetMap(Fiber testFiber, Fiber robotFiber)
        {
            Scene clientScene = robotFiber.Root;
            string mapName = clientScene.CurrentScene().Name;
            Fiber map = testFiber.GetFiber("MapManager").GetFiber(mapName);
            return map;
        }

        /// <summary>
        /// 获取客户端机器人在服务端对应地图场景中的服务端 Unit 实体
        /// </summary>
        public static Unit GetServerUnit(Fiber testFiber, Fiber robotFiber)
        {
            Scene clientScene = robotFiber.Root;
            string mapName = clientScene.CurrentScene().Name;
            Fiber map = testFiber.GetFiber("MapManager").GetFiber(mapName);
            ET.Client.PlayerComponent player = clientScene.GetComponent<ET.Client.PlayerComponent>();
            Unit unit = map.Root.GetComponent<UnitComponent>().Get(player.MyId);
            return unit;
        }
    }
}
