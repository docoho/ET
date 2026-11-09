namespace ET.Client
{
    /// <summary>新手 03：组件的生命周期和业务逻辑。不要直接 new 组件或手动调用 Awake。</summary>
    [EntitySystemOf(typeof(PracticeCounterComponent))]
    public static partial class PracticeCounterComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PracticeCounterComponent self, int initialValue)
        {
            self.Value = initialValue;
            Log.Info($"[Beginner03] 2. Awake: Value={self.Value}, InstanceId={self.InstanceId}");
        }

        /// <summary>普通扩展方法需要主动调用；它不会像 Awake 一样自动执行。</summary>
        public static void Increase(this PracticeCounterComponent self, int amount)
        {
            self.Value += amount;
            Log.Info($"[Beginner03] 4. Increase: amount={amount}, Value={self.Value}");
        }

        [EntitySystem]
        private static void Destroy(this PracticeCounterComponent self)
        {
            // Destroy 期间还能读取数据；返回后对象可能被清理并回收到对象池。
            Log.Info($"[Beginner03] 7. Destroy: final Value={self.Value}");
        }
    }
}
