namespace ET
{
    // 刚accept的session只持续5秒，必须通过验证，否则断开
    /// <summary>
    /// 服务端新接入 <see cref="Session"/> 的首包验证限时组件。
    /// <remarks>
    /// 挂载后启动一个 5 秒的单次定时器；若客户端在 5 秒内完成登录/认证，业务代码需主动移除此组件（<c>session.RemoveComponent&lt;SessionAcceptTimeoutComponent&gt;()</c>），
    /// 否则定时器到期后将自动销毁该 <see cref="Session"/>，防止恶意空连接占用服务器资源。
    /// </remarks>
    /// </summary>
    [ComponentOf(typeof(Session))]
    public class SessionAcceptTimeoutComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 5 秒超时单次定时器的 Id。
        /// </summary>
        public long Timer;
    }
}