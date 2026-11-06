using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    /// <summary>
    /// 对象等待组件系统（<see cref="ObjectWait"/> 的逻辑扩展）。
    /// 允许业务逻辑通过 <c>await objectWait.Wait&lt;T&gt;()</c> 异步挂起当前协程，直到其它地方调用 <c>objectWait.Notify(new T())</c> 唤醒它。
    /// 常用于将回调/事件式的异步流程（如等待路径移动结束、等待技能吟唱结束、等待场景加载完成）转化为线性直观的 async/await 代码。
    /// </summary>
    [EntitySystemOf(typeof(ObjectWait))]
    public static partial class ObjectWaitSystem
    {
        /// <summary>
        /// 组件唤醒时清空等待字典。
        /// </summary>
        [EntitySystem]
        private static void Awake(this ObjectWait self)
        {
            self.tcss.Clear();
        }
        
        /// <summary>
        /// 组件销毁时，将所有仍在等待中的任务以 <see cref="WaitTypeError.Destroy"/> 状态唤醒，避免等待方协程永久泄露。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this ObjectWait self)
        {
            foreach (object v in self.tcss.Values.ToArray())
            {
                ((IDestroyRun) v).SetResult();
            }
        }
        
        /// <summary>
        /// 异步等待指定结构体类型 <typeparamref name="T"/> 的信号通知。
        /// 注意：同一种类型 <typeparamref name="T"/> 在同一个 <see cref="ObjectWait"/> 组件上同一时刻只能有一个等待者。
        /// 支持通过协程上下文中的 <see cref="ETCancellationToken"/> 取消等待（取消时返回 <c>Error = WaitTypeError.Cancel</c>）。
        /// </summary>
        /// <typeparam name="T">等待信号的值类型结构体，需实现 <see cref="IWaitType"/>。</typeparam>
        /// <param name="self">当前对象等待组件。</param>
        /// <returns>唤醒时传入的结果结构体（含错误码状态）。</returns>
        public static async ETTask<T> Wait<T>(this ObjectWait self) where T : struct, IWaitType
        {
            ResultCallback<T> tcs = new();
            ETCancellationToken cancellationToken = await ETTask.GetContextAsync<ETCancellationToken>();
            self.tcss.Add(typeof (T), tcs);
            EntityRef<ObjectWait> selfRef = self;
            void CancelAction()
            {
                ObjectWait objectWait = selfRef;
                objectWait.Notify(new T() { Error = WaitTypeError.Cancel });
            }
            
            T ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs.Task;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }

        /// <summary>
        /// 触发指定类型 <typeparamref name="T"/> 的通知信号，唤醒正在 <see cref="Wait{T}"/> 该类型的协程并传递结果数据。
        /// 如果当前没有任何协程在等待该类型，则直接忽略。
        /// </summary>
        /// <typeparam name="T">通知信号的值类型结构体，需实现 <see cref="IWaitType"/>。</typeparam>
        /// <param name="self">当前对象等待组件。</param>
        /// <param name="obj">要传递给等待方的结果结构体。</param>
        public static void Notify<T>(this ObjectWait self, T obj) where T : struct, IWaitType
        {
            Type type = typeof (T);
            if (!self.tcss.TryGetValue(type, out object tcs))
            {
                return;
            }

            self.tcss.Remove(type);
            ((ResultCallback<T>) tcs).SetResult(obj);
        }
    }
}
