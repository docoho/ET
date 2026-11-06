using System;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 单位独立转向组件系统（在不移动时通过每帧定时器对四元数进行球面线性插值 Slerp）。
    /// </summary>
    [EntitySystemOf(typeof(TurnComponent))]
    public static partial class TurnComponentSystem
    {
        /// <summary>
        /// 每帧驱动单位旋转插值的定时器回调。
        /// </summary>
        [Invoke(TimerInvokeType.TurnTimer)]
        public class TurnTimer: ATimer<TurnComponent>
        {
            /// <summary>
            /// 每帧推进转向进度。
            /// </summary>
            protected override void Run(TurnComponent self)
            {
                try
                {
                    self.UpdateTurn();
                }
                catch (Exception e)
                {
                    Log.Error($"turn timer error: {self.Id}\n{e}");
                }
            }
        }
        
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this TurnComponent self)
        {
        }

        /// <summary>
        /// 按已流逝时间对 <see cref="Unit.Rotation"/> 执行球面插值（Slerp），到达转向时间后停止定时器。
        /// </summary>
        private static void UpdateTurn(this TurnComponent self)
        {
            long runTime = self.GetSingleton<TimeInfo>().ServerNow() - self.StartTime;
            self.GetParent<Unit>().Rotation = math.slerp(self.From, self.To, runTime * 1f / self.TurnTime);
            if (runTime < self.TurnTime)
            {
                return;
            }

            self.GetParent<Unit>().Rotation = self.To;
            self.Stop();
        }
        
        /// <summary>
        /// 启动平滑转向，在 <paramref name="turnTime"/> 毫秒内旋转到目标四元数 <paramref name="to"/>。
        /// </summary>
        public static void Turn(this TurnComponent self, quaternion to, int turnTime)
        {
            self.StartTime = self.GetSingleton<TimeInfo>().ServerNow();
            self.From = self.GetParent<Unit>().Rotation;
            self.To = to;
            self.TurnTime = turnTime;
            TimerComponent timerComponent = self.Root().TimerComponent;
            timerComponent.Remove(ref self.timer);
            self.timer = timerComponent.NewFrameTimer(TimerInvokeType.TurnTimer, self);
        }

        /// <summary>
        /// 停止当前的转向插值定时器。
        /// </summary>
        public static void Stop(this TurnComponent self)
        {
            self.Root().TimerComponent.Remove(ref self.timer);
        }
    }
}