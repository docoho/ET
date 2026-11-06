using System;

namespace ET
{
    /// <summary>
    /// <see cref="ETCancellationToken"/> 协程取消与超时控制扩展工具类。
    /// 支持子取消令牌级联（<see cref="AddCancel"/>）以及异步任务超时自动取消（<see cref="TimeoutAsync(ETTask,long)"/>）。
    /// </summary>
    public static class ETCancellationTokenHelper
    {
        /// <summary>
        /// 判断取消令牌是否已被触发取消（若传入 <c>null</c> 则安全返回 <c>false</c>）。
        /// </summary>
        public static bool IsCancel(this ETCancellationToken self)
        {
            if (self == null)
            {
                return false;
            }
            return self.IsDispose();
        }
        
        /// <summary>
        /// 在等待指定毫秒数（<paramref name="afterTimeCancel"/>）后，若令牌尚未取消，则自动调用 <see cref="ETCancellationToken.Cancel"/>。
        /// </summary>
        private static async ETTask TimeoutAsync(this ETCancellationToken self, long afterTimeCancel)
        {
            if (afterTimeCancel <= 0)
            {
                return;
            }
            
            if (self.IsCancel())
            {
                return;
            }

            // 这里用了Fiber.Instance是因为我知道有什么后果, 你们千万不能乱用这个Fiber.Instance
            await Fiber.Instance.Root.TimerComponent.WaitAsync(afterTimeCancel);
            if (self.IsCancel())
            {
                return;
            }
            self.Cancel();
        }
        
        /// <summary>
        /// 为当前异步任务叠加一个新的子取消令牌 <paramref name="addCancelToken"/>：
        /// 既可以用新增的 <paramref name="addCancelToken"/> 单独取消该子任务，同时如果外层原有的父取消令牌触发取消，也会级联取消 <paramref name="addCancelToken"/>。
        /// </summary>
        public static async ETTask AddCancel(this ETTask task, ETCancellationToken addCancelToken)
        {
            if (addCancelToken == null)
            {
                throw new Exception("add cancel token is null");
            }
            ETCancellationToken cancelToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            cancelToken?.Add(addCancelToken.Cancel);
            
            await task.NewContext(addCancelToken);
        }
        
        /// <summary>
        /// 为带返回值的异步任务叠加一个新的子取消令牌 <paramref name="addCancelToken"/>（支持父令牌级联取消）。
        /// </summary>
        public static async ETTask<T> AddCancel<T>(this ETTask<T> task, ETCancellationToken addCancelToken)
        {
            if (addCancelToken == null)
            {
                throw new Exception("add cancel token is null");
            }
            
            ETCancellationToken cancelToken = await ETTask.GetContextAsync<ETCancellationToken>();
            
            cancelToken?.Add(addCancelToken.Cancel);
            
            return await task.NewContext(addCancelToken);
        }
        
        /// <summary>
        /// 为异步任务绑定指定的 <paramref name="cancellationToken"/> 并设置超时时间（毫秒），超时后自动触发取消。
        /// </summary>
        public static async ETTask TimeoutAsync(this ETTask task, ETCancellationToken cancellationToken, long afterTimeCancel)
        {
            cancellationToken.TimeoutAsync(afterTimeCancel).Coroutine();
            await AddCancel(task, cancellationToken);
        }
        
        /// <summary>
        /// 为带返回值的异步任务绑定指定的 <paramref name="cancellationToken"/> 并设置超时时间（毫秒），超时后自动触发取消。
        /// </summary>
        public static async ETTask<T> TimeoutAsync<T>(this ETTask<T> task, ETCancellationToken cancellationToken, long afterTimeCancel)
        {
            cancellationToken.TimeoutAsync(afterTimeCancel).Coroutine();
            return await AddCancel(task, cancellationToken);
        }
        
        /// <summary>
        /// 为异步任务设置超时时间（毫秒），内部自动新建一个 <see cref="ETCancellationToken"/> 并在超时后触发取消。
        /// </summary>
        public static async ETTask TimeoutAsync(this ETTask task, long afterTimeCancel)
        {
            ETCancellationToken cancellationToken = new();
            cancellationToken.TimeoutAsync(afterTimeCancel).Coroutine();
            await AddCancel(task, cancellationToken);
        }
        
        /// <summary>
        /// 为带返回值的异步任务设置超时时间（毫秒），内部自动新建一个 <see cref="ETCancellationToken"/> 并在超时后触发取消。
        /// </summary>
        public static async ETTask<T> TimeoutAsync<T>(this ETTask<T> task, long afterTimeCancel)
        {
            ETCancellationToken cancellationToken = new();
            cancellationToken.TimeoutAsync(afterTimeCancel).Coroutine();
            return await AddCancel(task, cancellationToken);
        }
    }
}