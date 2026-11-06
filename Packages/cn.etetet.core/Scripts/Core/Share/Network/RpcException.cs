using System;

namespace ET
{
    /// <summary>
    /// RPC异常,带ErrorCode。
    /// <remarks>
    /// 当 RPC 调用发生底层网络断开或返回的错误码大于 <see cref="ErrorCore.ERR_WithException"/> 时抛出，
    /// 其 <see cref="ToString"/> 会自动解析错误码所属的包编号（<c>error / 1000</c>）与包内序号（<c>error % 1000</c>），便于快速定位问题模块。
    /// </remarks>
    /// </summary>
    public class RpcException: Exception
    {
        /// <summary>
        /// 触发异常的错误码。
        /// </summary>
        public int Error
        {
            get;
        }

        public RpcException(int error, string message): base(message)
        {
            this.Error = error;
        }

        public override string ToString()
        {
            int error = this.Error;
            if (this.Error > ErrorCore.ERR_WithException)
            {
                error -= ErrorCore.ERR_WithException;
            }
            return $"Error: {this.Error} package: {error / 1000} id: {error % 1000}\n{base.ToString()}";
        }
    }
}