using System;

namespace ET
{
    /// <summary>
    /// UnityBridge 编辑器命令处理器泛型抽象基类，封装请求类型绑定与错误响应构造辅助方法。
    /// </summary>
    [EnableClass]
    public abstract class AUnityBridgeHandler<TRequest, TResponse> : IUnityBridgeHandler
            where TRequest : class, IRequest
            where TResponse : class, IResponse
    {
        public Type RequestType => typeof(TRequest);

        public Type ResponseType => typeof(TResponse);

        public virtual async ETTask<IResponse> Handle(IRequest request)
        {
            return await this.Run((TRequest)request);
        }

        protected abstract ETTask<IResponse> Run(TRequest command);

        protected IResponse CreateErrorResponse(IRequest request, int error, string message)
        {
            return UnityBridgeResponseHelper.CreateErrorResponse(
                request?.RpcId ?? 0,
                typeof(TRequest).Name,
                error,
                message);
        }
    }
}