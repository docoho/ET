namespace ET.Client
{
    /// <summary>
    /// 客户端心跳与网络延迟测量组件：挂载在网关 Session 上。
    /// </summary>
    [ComponentOf(typeof(Session))]
    public class PingComponent: Entity, IAwake, IDestroy
    {
        /// <summary>当前客户端与服务器之间的往返延迟（RTT，毫秒）</summary>
        public long Ping { get; set; } //延迟值
    }
}