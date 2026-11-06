namespace ET
{
    /// <summary>
    /// <see cref="ETTask"/> 异步等待状态枚举。
    /// </summary>
    public enum AwaiterStatus: byte
    {
        /// <summary>操作尚未完成（挂起等待中）</summary>
        Pending = 0,
        
        /// <summary>操作已成功完成</summary>
        Succeeded = 1,

        /// <summary>操作因未捕获异常而失败</summary>
        Faulted = 2,
    }
}