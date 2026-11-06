namespace ET.Client
{
    /// <summary>
    /// 客户端向 Router 节点发起 SYN/ReconnectSYN 握手的临时实体：接收底层 KService 的 RouterACK 标志位。
    /// </summary>
    [ChildOf(typeof(NetComponent))]
    public class RouterConnector: Entity, IAwake, IDestroy
    {
        /// <summary>握手完成标志位（收到 RouterACK 后变为非 0）</summary>
        public byte Flag { get; set; }
    }
}