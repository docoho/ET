using System;

namespace ET
{
    /// <summary>
    /// 跨 Unity 状态变化（如触发编译、AssetDatabase.Refresh、进入/退出 PlayMode）的延迟命令处理器抽象基类。
    /// <para>首次调用 Handle 时将命令持久化到 pending-command.json 并通过 Started() 抛出信号进入挂起；后续在 EditorApplication.update 中通过 Deferred() 轮询直到条件满足返回最终结果。</para>
    /// </summary>
    internal abstract class AUnityBridgeDeferredHandler<TRequest, TResponse> :
            AUnityBridgeHandler<TRequest, TResponse>, IUnityBridgeDeferredHandler
            where TRequest : class, IRequest
            where TResponse : class, IResponse
    {
        public sealed override async ETTask<IResponse> Handle(IRequest request)
        {
            TRequest command = (TRequest)request;

            if (!UnityBridgeDeferredRuntime.TryCreatePendingCurrent(command, out IResponse createError))
            {
                return createError;
            }

            try
            {
                await this.Run(command, UnityBridgeDeferredContext.CreateStart());
                UnityBridgeDeferredRuntime.RollbackCurrent();
                return this.CreateErrorResponse(
                    command,
                    UnityBridgeErrorCode.HandlerFail,
                    "unity bridge deferred handler completed without starting a deferred operation");
            }
            catch (UnityBridgeDeferredStartedException)
            {
                return UnityBridgeResponseHelper.CreateDeferredResponse();
            }
            catch (UnityBridgeCommandStateException e)
            {
                UnityBridgeDeferredRuntime.RollbackCurrent();
                return this.CreateErrorResponse(command, e.Error, e.Message);
            }
            catch (System.Exception e)
            {
                UnityBridgeDeferredRuntime.RollbackCurrent();
                return this.CreateErrorResponse(command, UnityBridgeErrorCode.HandlerFail, e.ToString());
            }
        }

        public async ETTask<IResponse> Deferred(IRequest request, long startedAt)
        {
            TRequest command = (TRequest)request;
            try
            {
                return await this.Run(command, UnityBridgeDeferredContext.CreateResume(startedAt));
            }
            catch (UnityBridgeDeferredNotReadyException)
            {
                return null;
            }
            catch (UnityBridgeDeferredStartedException)
            {
                return this.CreateErrorResponse(
                    command,
                    UnityBridgeErrorCode.HandlerFail,
                    "unity bridge deferred handler attempted to start while resuming");
            }
            catch (UnityBridgeCommandStateException e)
            {
                return this.CreateErrorResponse(command, e.Error, e.Message);
            }
            catch (System.Exception e)
            {
                return this.CreateErrorResponse(command, UnityBridgeErrorCode.HandlerFail, e.ToString());
            }
        }

        protected sealed override async ETTask<IResponse> Run(TRequest command)
        {
            return await this.Run(command, UnityBridgeDeferredContext.CreateResume(0));
        }

        protected abstract ETTask<TResponse> Run(TRequest command, UnityBridgeDeferredContext deferred);
    }

    internal sealed class UnityBridgeDeferredContext
    {
        private readonly bool isResuming;

        private UnityBridgeDeferredContext(bool isResuming, long startedAt)
        {
            this.isResuming = isResuming;
            this.StartedAt = startedAt;
        }

        public bool IsResuming => this.isResuming;

        public long StartedAt { get; }

        public static UnityBridgeDeferredContext CreateStart()
        {
            return new UnityBridgeDeferredContext(false, 0);
        }

        public static UnityBridgeDeferredContext CreateResume(long startedAt)
        {
            return new UnityBridgeDeferredContext(true, startedAt);
        }

        public TResponse Started<TResponse>() where TResponse : class, IResponse
        {
            throw new UnityBridgeDeferredStartedException();
        }

        public TResponse NotReady<TResponse>() where TResponse : class, IResponse
        {
            throw new UnityBridgeDeferredNotReadyException();
        }
    }

    internal sealed class UnityBridgeDeferredStartedException : Exception
    {
        internal UnityBridgeDeferredStartedException() : base("unity bridge deferred operation started")
        {
        }
    }

    internal sealed class UnityBridgeDeferredNotReadyException : Exception
    {
        internal UnityBridgeDeferredNotReadyException() : base("unity bridge deferred operation is not ready")
        {
        }
    }
}
