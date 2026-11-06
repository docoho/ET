using System;

namespace ET
{
    /// <summary>
    /// UnityBridge 磁盘请求信封，封装单次请求的 RpcId、幂等键、超时时间及原始命令 JSON。
    /// </summary>
    [EnableClass]
    public sealed class UnityBridgeRequestEnvelope : Object
    {
        /// <summary>
        /// 请求唯一 RPC 标识
        /// </summary>
        public int RpcId { get; set; }

        /// <summary>
        /// 幂等键（用于防止同一命令重复执行并直接返回缓存响应）
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// 请求超时时间（毫秒，0 表示使用命令默认超时）
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// 带有 _t 类型鉴别器的命令 BSON/JSON 字符串
        /// </summary>
        public string CommandJson { get; set; }
    }

    /// <summary>
    /// 表示命令已进入跨 Domain Reload / PlayMode 切换的延迟执行（Deferred）挂起状态的内部占位响应。
    /// </summary>
    [EnableClass]
    internal sealed class UnityBridgeDeferredResponse : Object, IResponse
    {
        public int Error { get; set; }

        public string Message { get; set; }

        public int RpcId { get; set; }
    }

    /// <summary>
    /// UnityBridge 命令处理器通用接口。
    /// </summary>
    [EnableClass]
    public interface IUnityBridgeHandler
    {
        /// <summary>
        /// 处理的请求消息类型
        /// </summary>
        Type RequestType { get; }

        /// <summary>
        /// 返回的响应消息类型
        /// </summary>
        Type ResponseType { get; }

        /// <summary>
        /// 异步处理请求并返回响应
        /// </summary>
        ETTask<IResponse> Handle(IRequest request);
    }

    /// <summary>
    /// 支持跨编译/跨 PlayMode 状态变化的延迟恢复（Deferred）命令处理器接口。
    /// </summary>
    [EnableClass]
    public interface IUnityBridgeDeferredHandler
    {
        /// <summary>
        /// 在 Editor Update 中轮询恢复执行挂起的命令，若尚未就绪则返回 null，完成后返回最终 IResponse
        /// </summary>
        ETTask<IResponse> Deferred(IRequest request, long startedAt);
    }

    /// <summary>
    /// UnityBridge 响应构建辅助工具类。
    /// </summary>
    public static class UnityBridgeResponseHelper
    {
        public static IResponse CreateDeferredResponse()
        {
            return new UnityBridgeDeferredResponse();
        }

        public static ErrorResponse CreateErrorResponse(int rpcId, string commandType, int error, string message)
        {
            ErrorResponse response = ErrorResponse.Create();
            response.RpcId = rpcId;
            response.Error = error;
            response.Message = message ?? string.Empty;
            response.Command = commandType ?? string.Empty;
            return response;
        }

        public static bool IsDeferredResponse(IResponse response)
        {
            return response is UnityBridgeDeferredResponse;
        }
    }
}
