namespace ET
{
    /// <summary>
    /// 标记需要路由到客户端当前活动地图场景（<see cref="SceneType.Current"/>）处理的网络消息接口。
    /// </summary>
    public interface ICurrentMessage: IMessage
    {
    }
}
