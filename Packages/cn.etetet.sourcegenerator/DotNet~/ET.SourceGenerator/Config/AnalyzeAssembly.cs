using System.IO;

namespace ET
{
    /// <summary>
    /// 需要执行 Roslyn 静态分析与代码生成的目标程序集名称常量配置。
    /// <para>各 Analyzer 通过比对当前编译程序集名称与此处定义的集合，决定是否对当前项目启用特定规则检查。</para>
    /// </summary>
    public static class AnalyzeAssembly
    {
        private const string Core = "ET.Core";
        private const string Loader = "ET.Loader";
        private const string Model = "ET.Model";
        private const string Hotfix = "ET.Hotfix";
        private const string ModelView = "ET.ModelView";
        private const string HotfixView = "ET.HotfixView";

        /// <summary>
        /// 所有热更逻辑层程序集（Hotfix + HotfixView）。
        /// </summary>
        public static readonly string[] AllHotfix =
        [
            Hotfix, HotfixView
        ];

        /// <summary>
        /// 所有数据模型层程序集（Model + ModelView）。
        /// </summary>
        public static readonly string[] AllModel =
        [
            Model, ModelView
        ];

        /// <summary>
        /// 所有业务模型与热更逻辑程序集（Model + Hotfix + ModelView + HotfixView）。
        /// </summary>
        public static readonly string[] AllModelHotfix =
        [
            Model, Hotfix, ModelView, HotfixView
        ];
        
        /// <summary>
        /// 框架全部核心及业务程序集（Core + Loader + Model + Hotfix + ModelView + HotfixView）。
        /// </summary>
        public static readonly string[] All =
        [
            Core, Loader, Model, Hotfix, ModelView, HotfixView
        ];
        
        /// <summary>
        /// 纯逻辑数据模型程序集（仅 ET.Model）。
        /// </summary>
        public static readonly string[] AllLogicModel =
        [
            Model
        ];
    }
    
}