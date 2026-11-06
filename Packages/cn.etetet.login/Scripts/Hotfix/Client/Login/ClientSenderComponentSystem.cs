using System;
using System.Threading.Tasks;

namespace ET.Client
{
    /// <summary>
    /// 客户端主纤程网络发送组件系统：管理后台 NetClient 纤程生命周期，并将主纤程的 Login / Send / Call 请求通过进程内 Actor 消息转发给 NetClient 纤程。
    /// </summary>
    [EntitySystemOf(typeof(ClientSenderComponent))]
    public static partial class ClientSenderComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientSenderComponent self)
        {

        }
        
        [EntitySystem]
        private static void Destroy(this ClientSenderComponent self)
        {
            self.RemoveFiberAsync().Coroutine();
        }

        /// <summary>
        /// 异步移除并销毁关联的 NetClient 纤程。
        /// </summary>
        private static async ETTask RemoveFiberAsync(this ClientSenderComponent self)
        {
            if (self.fiberId == 0)
            {
                return;
            }

            long fiberId = self.fiberId;
            self.fiberId = 0;
            await self.Fiber().RemoveFiber(fiberId);
        }

        /// <summary>
        /// 先异步移除 NetClient 纤程再销毁自身组件。
        /// </summary>
        public static async ETTask DisposeAsync(this ClientSenderComponent self)
        {
            EntityRef<ClientSenderComponent> selfRef = self;
            await self.RemoveFiberAsync();
            self = selfRef;
            self?.Dispose();
        }

        /// <summary>
        /// 创建独立的 NetClient 线程池纤程并发起登录流程，返回登录成功后的玩家 PlayerId。
        /// </summary>
        public static async ETTask<long> LoginAsync(this ClientSenderComponent self, string address, string account, string password)
        {
            EntityRef<ClientSenderComponent> selfRef = self;
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new Exception("router manager address is empty.");
            }

            self.fiberId = await self.Fiber().CreateFiber(SchedulerType.ThreadPool, IdGenerater.Instance.GenerateId(), SceneType.NetClient, "NetClient");
            self = selfRef;
            self.FiberInstanceId = new FiberInstanceId(self.fiberId);

            Main2NetClient_Login main2NetClientLogin = Main2NetClient_Login.Create();
            main2NetClientLogin.OwnerFiberId = self.Fiber().Id;
            main2NetClientLogin.Account = account;
            main2NetClientLogin.Password = password;
            main2NetClientLogin.Address = address;
            
            NetClient2Main_Login response = await self.Root().GetComponent<ProcessInnerSender>().Call(self.FiberInstanceId, main2NetClientLogin) as NetClient2Main_Login;
            
            return response.PlayerId;
        }

        /// <summary>
        /// 委托 NetClient 纤程向服务器发送单向消息（IMessage）。
        /// </summary>
        public static void Send(this ClientSenderComponent self, IMessage message)
        {
            A2NetClient_Message a2NetClientMessage = A2NetClient_Message.Create();
            a2NetClientMessage.MessageObject = message;
            self.Root().GetComponent<ProcessInnerSender>().Send(self.FiberInstanceId, a2NetClientMessage);
        }

        /// <summary>
        /// 委托 NetClient 纤程向服务器发送 RPC 请求（IRequest）并异步等待响应。
        /// </summary>
        public static async ETTask<IResponse> Call(this ClientSenderComponent self, IRequest request, bool needException = true)
        {
            A2NetClient_Request a2NetClientRequest = A2NetClient_Request.Create();
            a2NetClientRequest.MessageObject = request;
            
            using A2NetClient_Response a2NetClientResponse = await self.Root().GetComponent<ProcessInnerSender>().Call(self.FiberInstanceId, a2NetClientRequest) as A2NetClient_Response;
            IResponse response = a2NetClientResponse.MessageObject;
            
            if (response.Error == ErrorCode.ERR_MessageTimeout)
            {
                throw new RpcException(response.Error, $"Rpc error: request, 注意Actor消息超时，请注意查看是否死锁或者没有reply: {request}, response: {response}");
            }

            if (needException && ErrorCode.IsRpcNeedThrowException(response.Error))
            {
                throw new RpcException(response.Error, $"Rpc error: {request}, response: {response}");
            }
            return response;
        }

    }
}
