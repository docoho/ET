namespace ET.Client
{
    /// <summary>
    /// 客户端网关 Session 错误通知组件：挂载在 NetClient 纤程的ゲート Session 上，销毁时通知主纤程连接已断开。
    /// </summary>
    [ComponentOf(typeof(Session))]
    public class ClientSessionErrorComponent: Entity, IAwake, IDestroy
    {

    }
}