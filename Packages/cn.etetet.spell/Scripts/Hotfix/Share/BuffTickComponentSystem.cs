namespace ET
{
    /// <summary>
    /// Buff 周期触发（Tick）组件生命周期系统。
    /// </summary>
    [EntitySystemOf(typeof(BuffTickComponent))]
    public static partial class BuffTickComponentSystem
    {
        /// <summary>
        /// Buff 周期 Tick 定时器回调。
        /// </summary>
        [Invoke(TimerInvokeType.BuffTickTimer)]
        public class BuffTickTimer: ATimer<BuffTickComponent>
        {
            /// <summary>
            /// 每次定时器触发时执行一次 <see cref="BuffTickComponentHelper.OnTick"/>。
            /// </summary>
            protected override void Run(BuffTickComponent self)
            {
                self.OnTick();
            }
        }

        /// <summary>
        /// 组件唤醒时检查是否为覆盖型（Override）AI Tick，若是则压入 <see cref="BTCoroutineComponent"/> 栈，否则直接启动周期定时器。
        /// </summary>
        [EntitySystem]
        private static void Awake(this BuffTickComponent self)
        {
            Buff buff = self.GetParent<Buff>();
            EffectServerBuffTick effect = buff.GetConfig().GetEffect<EffectServerBuffTick>();
            self.Override = effect.Override;

            if (self.Override)
            {
                Unit unit = buff.Parent.GetParent<Unit>();
                self.UnitId = unit.Id;

                BTCoroutineComponent btCoroutineComponent = unit.GetComponent<BTCoroutineComponent>() ?? unit.AddComponent<BTCoroutineComponent>();
                btCoroutineComponent.AddAI(buff);
            }
            
            self.Start();
        }
        
        /// <summary>
        /// 组件销毁时停止定时器与进行中的异步行为树协程，并在覆盖型 AI 结束时恢复下层 AI。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this BuffTickComponent self)
        {
            self.Stop();
            
            if (self.Override)
            {
                Unit unit = self.Scene()?.GetComponent<UnitComponent>()?.Get(self.UnitId);
                unit?.GetComponent<BTCoroutineComponent>()?.ResumeAI();
            }
        }


    }

    /// <summary>
    /// <see cref="BuffTickComponent"/> 启动、触发与停止辅助扩展方法。
    /// </summary>
    public static class BuffTickComponentHelper
    {
        /// <summary>
        /// 立即执行一次 <see cref="OnTick"/> 并注册周期重复定时器（<see cref="TimerInvokeType.BuffTickTimer"/>）。
        /// </summary>
        public static void Start(this BuffTickComponent self)
        {
            Buff buff = self.GetParent<Buff>();
            int tickTime = buff.TickTime;
            if (tickTime <= 0)
            {
                return;
            }
            
            self.OnTick();
            
            TimerComponent timerComponent = buff.Root().TimerComponent;
            self.TimerId = timerComponent.NewRepeatedTimer(tickTime, TimerInvokeType.BuffTickTimer, self);
        }
        
        /// <summary>
        /// 执行一次 Buff 的 <see cref="EffectServerBuffTick"/> 服务端周期行为树。
        /// </summary>
        public static void OnTick(this BuffTickComponent self)
        {
            Buff buff = self.GetParent<Buff>();
            if (buff == null)
            {
                return;
            }

            EffectServerBuffTick effect = buff.GetConfig().GetEffect<EffectServerBuffTick>();
            if (effect != null)
            {
                Unit unit = buff.Parent.GetParent<Unit>();
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                BTHelper.RunTree(effect, env);
            }
        }

        /// <summary>
        /// 移除周期定时器，取消正在等待的 <see cref="ETCancellationToken"/> 并重置当前协程节点状态。
        /// </summary>
        public static void Stop(this BuffTickComponent self)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }
            root.TimerComponent?.Remove(ref self.TimerId);

            self.CancellationToken?.Cancel();
            self.CancellationToken = null;
            self.Current = 0;
            self.HashCode = 0;
        }
    }
}
