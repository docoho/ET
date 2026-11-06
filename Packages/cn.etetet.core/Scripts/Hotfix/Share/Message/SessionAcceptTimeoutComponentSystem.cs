using System;

namespace ET
{
    /// <summary>
    /// 会话连接验证超时组件系统（<see cref="SessionAcceptTimeoutComponent"/> 的逻辑扩展）。
    /// 服务端在 Accept 新连接后挂载此组件，若 5 秒内未完成身份验证（验证通过后需手动移除该组件），
    /// 则自动断开并销毁对应的 <see cref="Session"/>，防止恶意空连接占用服务器资源。
    /// </summary>
    [EntitySystemOf(typeof(SessionAcceptTimeoutComponent))]
    public static partial class SessionAcceptTimeoutComponentSystem
    {
        /// <summary>
        /// 5 秒超时定时器触发器：直接销毁父节点（即 <see cref="Session"/>）。
        /// </summary>
        [Invoke(TimerInvokeType.SessionAcceptTimeout)]
        public class SessionAcceptTimeout: ATimer<SessionAcceptTimeoutComponent>
        {
            protected override void Run(SessionAcceptTimeoutComponent self)
            {
                try
                {
                    self.Parent.Dispose();
                }
                catch (Exception e)
                {
                    Log.Error($"move timer error: {self.Id}\n{e}");
                }
            }
        }
        
        /// <summary>
        /// 组件唤醒时注册一个 5000 毫秒（5 秒）后触发的一次性定时器。
        /// </summary>
        [EntitySystem]
        private static void Awake(this SessionAcceptTimeoutComponent self)
        {
            self.Timer = self.Root().TimerComponent.NewOnceTimer(self.GetSingleton<TimeInfo>().ServerNow() + 5000, TimerInvokeType.SessionAcceptTimeout, self);
        }
        
        /// <summary>
        /// 组件销毁时（例如验证成功主动移除组件，或 Session 已提前断开）取消该超时定时器。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this SessionAcceptTimeoutComponent self)
        {
            self.Root().TimerComponent?.Remove(ref self.Timer);
        }
        
    }
    
    
}