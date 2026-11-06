namespace ET
{
    /// <summary>
    /// 逻辑层错误码区间划分与异常判定分部类。
    /// </summary>
    [UniqueId]
    public static partial class ErrorCode
    {
        /// <summary>
        /// 操作成功（无错误）。
        /// </summary>
        public const int ERR_Success = 0;

        // 1-11004 是SocketError请看SocketError定义
        //-----------------------------------
        // 100000-100000000是Core层的错误
        
        // 这里配置逻辑层的错误码
        // 100000000以上是逻辑层的错误
        // 200000000以上不抛异常  ErrorCore.IsRpcNeedThrowException
        
        //public const int ErrorExampleException = 100000000 + PackageType.Core * 1000 + 1;
        //public const int ErrorExampleNoException = 200000000 + PackageType.Core * 1000 + 1;
        
        // 小于这个Rpc会抛异常，大于这个异常的error需要自己判断处理，也就是说需要处理的错误应该要大于该值
        /// <summary>
        /// 不抛出 <see cref="RpcException"/> 的业务错误码起始基准值（200000000）。
        /// <remarks>
        /// 错误码处于 <c>(0, 200000000]</c> 之间时属于底层或系统异常，RPC 框架会自动抛出 <see cref="RpcException"/>；
        /// 错误码大于 <c>200000000</c> 时属于可预期的正常业务逻辑返回码，RPC 不抛异常而是直接返回 <see cref="IResponse"/> 供调用方判断。
        /// </remarks>
        /// </summary>
        public const int ERR_WithoutException = 200000000;
        
        /// <summary>
        /// 异步操作被取消（不抛异常错误码）。
        /// </summary>
        public const int ERR_Cancel = 200000000 + PackageType.Core * 1000 + 1; // 200001001
        /// <summary>
        /// 异步操作超时（不抛异常错误码）。
        /// </summary>
        public const int ERR_Timeout = 200000000 + PackageType.Core * 1000 + 2; // 200001002


        /// <summary>
        /// 判断指定的错误码在 RPC 返回时是否需要自动抛出 <see cref="RpcException"/> 异常。
        /// </summary>
        public static bool IsRpcNeedThrowException(int error)
        {
            if (error == 0)
            {
                return false;
            }
            if (error > ERR_WithoutException)
            {
                return false;
            }

            return true;
        }
    }
}