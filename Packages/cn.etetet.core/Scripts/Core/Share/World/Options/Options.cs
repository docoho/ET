using CommandLine;

namespace ET
{
    /// <summary>
    /// 服务端进程启动命令行参数配置单例（基于 CommandLineParser 解析）。
    /// </summary>
    [AllowInstance]
    public class Options: Singleton<Options>, ISingletonAwake
    {
        /// <summary>当前进程启动的场景或配置名称（定义在 <c>SceneType</c> 或启动配置中）</summary>
        [Option("SceneName", Required = true, HelpText = "define in SceneType class")]
        public string SceneName { get; set; }
        
        /// <summary>使用的启动配置目录名称（如 "Localhost"、"Benchmark"）</summary>
        [Option("StartConfig", Required = false, Default = "Localhost")]
        public string StartConfig { get; set; }

        /// <summary>基础进程编号（Process ID，用于拼接生成全局唯一的 <see cref="IdGenerater"/> 雪花 ID）</summary>
        [Option("Process", Required = false, Default = 1)]
        public int Process { get; set; }
        
        /// <summary>服务发现架构下同一服务的副本偏移索引（实际进程标识 = <c>Process + ReplicaIndex</c>）</summary>
        [Option("ReplicaIndex", Required = false, Default = 0)]
        public int ReplicaIndex { get; set; }
        
        /// <summary>日志输出最低级别（0 = Trace/Debug 全开，数值越大过滤越严格）</summary>
        [Option("LogLevel", Required = false, Default = 0)]
        public int LogLevel { get; set; }
        
        /// <summary>是否开启控制台交互组件（1 = 开启，0 = 关闭）</summary>
        [Option("Console", Required = false, Default = 0)]
        public int Console { get; set; }
        
        /// <summary>是否强制所有 Fiber 在主线程单线程调度（1 = 单线程调试模式，0 = 默认多线程/线程池调度）</summary>
        [Option("SingleThread", Required = false, Default = 0)]
        public int SingleThread { get; set; }

        public void Awake()
        {
        }
    }
}
