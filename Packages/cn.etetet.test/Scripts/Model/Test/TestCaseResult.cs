using System;

namespace ET.Test
{
    /// <summary>
    /// 单个测试用例执行完毕后的结果数据封装，记录耗时、错误码、未捕获异常及是否成功。
    /// </summary>
    [EnableClass]
    public sealed class TestCaseResult
    {
        /// <summary>
        /// 测试用例名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 返回错误码（0 表示成功）
        /// </summary>
        public int Error { get; }

        /// <summary>
        /// 执行耗时（毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; }

        /// <summary>
        /// 执行过程中抛出的异常（无异常时为 null）
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 测试用例是否通过（错误码为 0 且无异常）
        /// </summary>
        public bool IsSuccess => this.Error == 0 && this.Exception == null;

        public TestCaseResult(string name, int error, long elapsedMilliseconds, Exception exception)
        {
            this.Name = name;
            this.Error = error;
            this.ElapsedMilliseconds = elapsedMilliseconds;
            this.Exception = exception;
        }
    }
}
