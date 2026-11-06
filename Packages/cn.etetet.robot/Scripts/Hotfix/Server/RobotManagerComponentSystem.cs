using System;

namespace ET.Server
{
    /// <summary>
    /// 压测机器人管理组件系统，负责创建机器人独立客户端纤程、驱动机器人自动登录及销毁回收所有机器人纤程
    /// </summary>
    [EntitySystemOf(typeof(RobotManagerComponent))]
    public static partial class RobotManagerComponentSystem
    {
        /// <summary>
        /// 组件初始化回调
        /// </summary>
        [EntitySystem]
        private static void Awake(this RobotManagerComponent self)
        {
        }
        
        /// <summary>
        /// 组件销毁时移除所有已创建的机器人纤程
        /// </summary>
        [EntitySystem]
        private static void Destroy(this RobotManagerComponent self)
        {
            async ETTask Remove(long f)
            {
                await self.Fiber().RemoveFiber(f);
            }
            
            foreach (long fiberId in self.robots.Values)
            {
                Remove(fiberId).Coroutine();
            }
        }

        /// <summary>
        /// 根据机器人账号名查找其客户端根场景对应的 <see cref="ActorId"/>
        /// </summary>
        /// <param name="self">机器人管理组件实例</param>
        /// <param name="account">机器人账号名</param>
        /// <param name="actorId">输出查找到的机器人 ActorId</param>
        /// <returns>是否查找成功</returns>
        public static bool GetRobotActorId(this RobotManagerComponent self, string account, out ActorId actorId)
        {
            if (!self.robots.TryGetValue(account, out long fiberId))
            {
                actorId = default;
                return false;
            }
            actorId = new ActorId(self.GetSingleton<AddressSingleton>().InnerAddress, new FiberInstanceId(fiberId, 1));
            return true;
        }

        /// <summary>
        /// 创建机器人，await之后，机器人则登录成功
        /// </summary>
        public static async ETTask<long> NewRobot(this RobotManagerComponent self, SchedulerType schedulerType, string account)
        {
            EntityRef<RobotManagerComponent> selfRef = self;
            string routerManagerAddress = GetRouterManagerAddress(self);
            long robot = await self.Fiber().CreateFiber(schedulerType, IdGenerater.Instance.GenerateId(), SceneType.Client, account);
            self = selfRef;
            ProcessInnerSender processInnerSender = self.Root().GetComponent<ProcessInnerSender>();
            Robot_LoginRequest robotLoginRequest = Robot_LoginRequest.Create();
            robotLoginRequest.Account = account;
            robotLoginRequest.Password = "";
            robotLoginRequest.Address = routerManagerAddress;
            await processInnerSender.Call(new FiberInstanceId(robot, 1), robotLoginRequest);
            self = selfRef;
            self.robots.Add(account, robot);
            return robot;
        }

        /// <summary>
        /// 从当前纤程的子纤程 <c>RouterManager</c> 中获取路由管理器的对外监听地址
        /// </summary>
        private static string GetRouterManagerAddress(this RobotManagerComponent self)
        {
            Fiber routerManagerFiber = self.Fiber().GetFiber("RouterManager");
            if (routerManagerFiber == null)
            {
                throw new Exception($"RouterManager fiber not found under fiber: {self.Fiber().Name}");
            }

            string address = routerManagerFiber.Root.GetComponent<RouterManagerAddressComponent>()?.Address;
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new Exception("RouterManagerAddressComponent is missing on RouterManager scene.");
            }

            return address;
        }
    }
}
