namespace ET
{
    /// <summary>
    /// <see cref="Session"/> 空闲超时（心跳）检测组件。
    /// <remarks>
    /// 定期检查所属 <see cref="Session"/> 的 <c>LastRecvTime</c> 与 <c>LastSendTime</c>，若长时间未发生任何收发通信则判定为死连接并主动断开。
    /// </remarks>
    /// </summary>
    [ComponentOf(typeof(Session))]
    public class SessionIdleCheckerComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 周期检查定时器的 Id。
        /// </summary>
        public long RepeatedTimer;
    }
}