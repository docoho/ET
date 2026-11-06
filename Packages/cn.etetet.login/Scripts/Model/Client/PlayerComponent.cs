namespace ET.Client
{
    /// <summary>
    /// 客户端本地玩家信息组件：记录当前登录成功的玩家实体 ID（MyId）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class PlayerComponent: Entity, IAwake
    {
        /// <summary>当前客户端登录的玩家 PlayerId（同时也是主角 Unit 的 ID）</summary>
        public long MyId { get; set; }
    }
}