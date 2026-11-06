namespace ET.Client
{
    /// <summary>
    /// 客户端路由存活检测组件：挂载在 Session 上，超时未收包时自动触发切换 Router 节点重连。
    /// </summary>
    [ComponentOf(typeof(Session))]
    public class RouterCheckComponent: Entity, IAwake
    {
    }
}