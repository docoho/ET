using System;

namespace ET
{
    /// <summary>
    /// 会话空闲检测组件系统（<see cref="SessionIdleCheckerComponent"/> 的逻辑扩展）。
    /// 定期检查 <see cref="Session"/> 的最后收包和发包时间，若双端长时间无通信（僵尸连接或断网），则自动超时断开连接。
    /// </summary>
    [EntitySystemOf(typeof(SessionIdleCheckerComponent))]
    public static partial class SessionIdleCheckerComponentSystem
    {
        /// <summary>
        /// 周期性空闲检查定时器回调实现。
        /// </summary>
        [Invoke(TimerInvokeType.SessionIdleChecker)]
        public class SessionIdleChecker: ATimer<SessionIdleCheckerComponent>
        {
            protected override void Run(SessionIdleCheckerComponent self)
            {
                try
                {
                    self.Check();
                }
                catch (Exception e)
                {
                    Log.Error($"session idle checker timer error: {self.Id}\n{e}");
                }
            }
        }
    
        /// <summary>
        /// 组件唤醒时启动周期性定时器（每 <see cref="CheckInteral"/> 毫秒执行一次检查）。
        /// </summary>
        [EntitySystem]
        private static void Awake(this SessionIdleCheckerComponent self)
        {
            self.RepeatedTimer = self.Root().TimerComponent.NewRepeatedTimer(CheckInteral, TimerInvokeType.SessionIdleChecker, self);
        }
        
        /// <summary>
        /// 组件销毁时移除周期性检查定时器。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this SessionIdleCheckerComponent self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                root.TimerComponent?.Remove(ref self.RepeatedTimer);
            }
        }

        /// <summary>
        /// 定时检查间隔（2000 毫秒 = 2 秒）。
        /// </summary>
        private const int CheckInteral = 2000;

#if UNITY_EDITOR || DEBUG
        /// <summary>
        /// 调试/编辑器模式下的会话超时时间（400 秒），设得较长是为了防止开发人员断点调试时连接被超时断开。
        /// </summary>
        public const int SessionTimeoutTime = 400000;
#else
        /// <summary>
        /// 正式发布模式下的会话超时时间（40 秒）。
        /// </summary>
        public const int SessionTimeoutTime = 40000;
#endif

        /// <summary>
        /// 执行空闲检查逻辑：若距上次收包或距上次发包的时间小于超时阈值，则视为活跃；否则标记超时错误并销毁 Session。
        /// </summary>
        private static void Check(this SessionIdleCheckerComponent self)
        {
            Session session = self.GetParent<Session>();
            long timeNow = self.GetSingleton<TimeInfo>().ClientNow();

            // 只要收包和发包都在超时时间窗口之内，说明连接正常
            if (timeNow - session.LastRecvTime < SessionTimeoutTime && timeNow - session.LastSendTime < SessionTimeoutTime)
            {
                return;
            }

            Log.Info($"session timeout: {session.Id} {timeNow} {session.LastRecvTime} {session.LastSendTime} {timeNow - session.LastRecvTime} {timeNow - session.LastSendTime}");
            session.Error = ErrorCode.ERR_SessionSendOrRecvTimeout;

            session.Dispose();
        }
    }
}