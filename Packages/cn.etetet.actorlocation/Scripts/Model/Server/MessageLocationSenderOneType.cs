namespace ET.Server
{
    /// <summary>
    /// 特定位置分类（LocationType）下的位置消息发送器容器（按目标实体 Id 管理 <see cref="MessageLocationSender"/> 子实体，并定期清理过期缓存）。
    /// </summary>
    [ChildOf(typeof(MessageLocationSenderComponent))]
    public class MessageLocationSenderOneType: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 缓存的 <see cref="MessageLocationSender"/> 空闲超时回收时间（60 秒）。
        /// </summary>
        public const long TIMEOUT_TIME = 60 * 1000;

        /// <summary>
        /// 定期检查并清理超时缓存的定时器 ID。
        /// </summary>
        public long CheckTimer;
    }
    
    /// <summary>
    /// 位置消息发送器总管理组件（挂载在 Scene 上，按 LocationType 分类管理所有的 <see cref="MessageLocationSenderOneType"/>）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MessageLocationSenderComponent: Entity, IAwake
    {
        /// <summary>
        /// 检查定时器 ID。
        /// </summary>
        public long CheckTimer;
    }
}