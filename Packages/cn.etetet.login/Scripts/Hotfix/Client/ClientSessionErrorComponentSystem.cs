namespace ET.Client
{
    /// <summary>
    /// 客户端网关 Session 错误监听组件系统：当 NetClient 纤程中的 Session 销毁时，向主纤程（Main Fiber）发送断开通知。
    /// </summary>
    [EntitySystemOf(typeof(ClientSessionErrorComponent))]
    public static partial class ClientSessionErrorComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientSessionErrorComponent self)
        {

        }
        
        [EntitySystem]
        private static void Destroy(this ClientSessionErrorComponent self)
        {
            Fiber fiber = self.Fiber();
            if (fiber.IsDisposed)
            {
                return;
            }
            NetClient2Main_SessionDispose message = NetClient2Main_SessionDispose.Create();
            message.Error = self.GetParent<Session>().Error;
            fiber.Root.GetComponent<ProcessInnerSender>().Send(new FiberInstanceId(self.Fiber().ParentFiberId), message);
        }
    }
}