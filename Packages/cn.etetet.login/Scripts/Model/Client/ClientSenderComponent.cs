namespace ET.Client
{
    /// <summary>
    /// 客户端主纤程网络发送代理组件：持有后台 NetClient 纤程的 FiberInstanceId，供主线程向网络纤程发送登录及业务消息。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ClientSenderComponent: Entity, IAwake, IDestroy
    {
        /// <summary>后台 NetClient 纤程 ID</summary>
        public long fiberId;

        /// <summary>后台 NetClient 纤程的进程内通信标识</summary>
        public FiberInstanceId FiberInstanceId;
    }
}
