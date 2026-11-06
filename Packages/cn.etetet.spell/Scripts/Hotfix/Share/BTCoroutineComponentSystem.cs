namespace ET
{
    /// <summary>
    /// 行为树协程 AI 栈管理组件系统（通过栈结构实现高优先级 AI Buff 覆盖压栈暂停旧 AI，并在新 AI 结束时自动恢复栈顶旧 AI）。
    /// </summary>
    [EntitySystemOf(typeof(BTCoroutineComponent))]
    public static partial class BTCoroutineComponentSystem
    {
        /// <summary>
        /// 组件唤醒初始化。
        /// </summary>
        [EntitySystem]
        private static void Awake(this BTCoroutineComponent self)
        {
        }

        /// <summary>
        /// 组件销毁清理。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this BTCoroutineComponent self)
        {
        }

        /// <summary>
        /// 压入新的覆盖型 AI Buff：暂停当前栈顶的旧 AI Tick，并将新 AI Buff 压入栈顶启动运行。
        /// </summary>
        public static void AddAI(this BTCoroutineComponent self, Buff buff)
        {
            Buff oldTop = self.GetTopInvalid();
            oldTop?.GetComponent<BuffTickComponent>().Stop();

            self.AIStack.Push(buff);
            buff.GetComponent<BuffTickComponent>().Start();
        }

        /// <summary>
        /// 当覆盖型 AI Buff 销毁时，恢复当前栈顶有效 AI Buff 的 Tick 执行。
        /// </summary>
        public static void ResumeAI(this BTCoroutineComponent self)
        {
            Buff buff = self.GetTopInvalid();
            buff?.GetComponent<BuffTickComponent>().Start();
        }

        /// <summary>
        /// 弹出栈顶已销毁的无效 Buff 引用，并返回当前有效的栈顶 AI Buff。
        /// </summary>
        private static Buff GetTopInvalid(this BTCoroutineComponent self)
        {
            while (self.AIStack.Count > 0)
            {
                Buff buff = self.AIStack.Peek();
                if (buff == null)
                {
                    self.AIStack.Pop();
                    continue;
                }
                break;
            }
            
            if (self.AIStack.Count == 0)
            {
                return null;
            }

            return self.AIStack.Peek();
        }
    }
}
