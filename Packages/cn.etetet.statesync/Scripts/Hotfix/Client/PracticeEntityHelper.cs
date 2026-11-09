using System;

namespace ET.Client
{
    /// <summary>
    /// 新手 03：完整的创建、Awake、修改、查询、销毁流程。
    /// 必须在 root 所属 Fiber 的执行上下文调用；本例全程同步，不需要 await。
    /// Unity Console 搜索 Beginner03 可看到按编号排列的日志。
    /// </summary>
    public static class PracticeEntityHelper
    {
        /// <summary>
        /// 在 root 上临时挂载计数组件，返回计算结果，并保证清理自己创建的组件。
        /// 调用前 root 上不能已存在 PracticeCounterComponent；同类型组件只能挂一个。
        /// </summary>
        public static int Run(Scene root, int initialValue, int increment)
        {
            Log.Info($"[Beginner03] 1. AddComponent: initialValue={initialValue}, scene={root.Name}");

            // 第二个泛型参数 int 对应 IAwake<int>，initialValue 会自动传入 Awake。
            PracticeCounterComponent counter = root.AddComponent<PracticeCounterComponent, int>(initialValue);
            EntityRef<PracticeCounterComponent> counterRef = counter;
            int result;
            try
            {
                // AddComponent 返回前 Awake 已同步执行完毕。
                if (counter.Value != initialValue || counter.InstanceId == 0)
                {
                    throw new InvalidOperationException("[Beginner03] Awake did not initialize the component.");
                }

                Log.Info($"[Beginner03] 3. AddComponent returned: Awake complete, Value={counter.Value}");
                counter.Increase(increment);

                // 查询拿到的是已经挂载的同一个组件；GetComponent 不会创建新组件。
                PracticeCounterComponent found = root.GetComponent<PracticeCounterComponent>();
                if (!ReferenceEquals(counter, found) || found.Value != initialValue + increment)
                {
                    throw new InvalidOperationException("[Beginner03] GetComponent returned an unexpected component or value.");
                }

                result = found.Value;
                Log.Info($"[Beginner03] 5. GetComponent: same instance=True, Value={result}");
            }
            finally
            {
                // RemoveComponent 同时解除挂载并调用 Dispose，框架会自动触发 Destroy。
                // 即使上面的检查失败，也不把演示组件留在游戏场景中。
                Log.Info("[Beginner03] 6. RemoveComponent: begin cleanup");
                root.RemoveComponent<PracticeCounterComponent>();
            }

            // 销毁后不要再读 counter.Value。使用 EntityRef 判断原来的实体是否仍然有效。
            PracticeCounterComponent afterRemove = root.GetComponent<PracticeCounterComponent>();
            PracticeCounterComponent afterDispose = counterRef;
            if (afterRemove != null || afterDispose != null)
            {
                throw new InvalidOperationException("[Beginner03] Component removal or EntityRef invalidation failed.");
            }

            Log.Info($"[Beginner03] 8. PASS: GetComponent=null, EntityRef=null, result={result}");
            return result;
        }
    }
}
