using System;

namespace ET
{
    /// <summary>
    /// UnityBridge 命令执行状态不满足要求时抛出的专用异常（携带标准 UnityBridgeErrorCode）。
    /// </summary>
    internal sealed class UnityBridgeCommandStateException : Exception
    {
        public UnityBridgeCommandStateException(int error, string message) : base(message)
        {
            this.Error = error;
        }

        public int Error { get; }
    }
}
