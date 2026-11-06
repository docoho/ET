using CommandLine;

namespace ET.Test
{
    /// <summary>
    /// 测试运行命令行参数模型，支持按正则过滤用例名称、开启并行执行以及限制最大并发数。
    /// </summary>
    public class TestArgs: Object
    {
        /// <summary>
        /// 测试用例类名匹配正则表达式（默认 ".*" 匹配全部）
        /// </summary>
        [Option('n', "Name", Required = false, Default = ".*")]
        public string Name { get; set; }

        /// <summary>
        /// 是否启用并行执行模式
        /// </summary>
        [Option('p', "Parallel", Required = false, Default = false)]
        public bool Parallel { get; set; }

        /// <summary>
        /// 并行执行时的最大并发度（0 表示自动使用 CPU 逻辑核心数）
        /// </summary>
        [Option("MaxConcurrency", Required = false, Default = 0)]
        public int MaxConcurrency { get; set; }
    }
}
