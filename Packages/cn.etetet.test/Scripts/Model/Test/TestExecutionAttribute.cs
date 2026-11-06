using System;

namespace ET.Test
{
    /// <summary>
    /// 标记测试用例执行并发模式的特性（可指定 Parallel 并行或 Exclusive 独占串行执行）。
    /// </summary>
    [EnableClass]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TestExecutionAttribute : Attribute
    {
        /// <summary>
        /// 测试执行模式
        /// </summary>
        public TestExecutionMode Mode { get; }

        public TestExecutionAttribute(TestExecutionMode mode)
        {
            this.Mode = mode;
        }
    }
}
