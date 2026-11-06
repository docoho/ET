namespace ET.Server
{
    /// <summary>
    /// 网关上的玩家实体：代表一个已通过网关认证的在线（或短时断线待重连）玩家。
    /// </summary>
    [ChildOf(typeof(PlayerComponent))]
    public sealed class Player : Entity, IAwake<string>
    {
        /// <summary>玩家登录账号名</summary>
        public string Account { get; set; }
    }
}