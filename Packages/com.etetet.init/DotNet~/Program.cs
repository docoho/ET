using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace ET
{
    /// <summary>
    /// 命令行工具内部使用的代码模式枚举。
    /// </summary>
    enum CodeMode
    {
        Client,
        Server,
        ClientServer,
    }

    /// <summary>
    /// <c>ET.CodeMode</c> 命令行参数定义。
    /// </summary>
    class Options
    {
        /// <summary>
        /// 目标代码模式（通过 <c>-c</c> 或 <c>--CodeMode</c> 传入，默认为 <see cref="CodeMode.ClientServer"/>）。
        /// </summary>
        [Option('c', "CodeMode", Required = false, Default = CodeMode.ClientServer)]
        public CodeMode CodeMode { get; set; }
    }

    /// <summary>
    /// <c>ET.CodeMode.dll</c> 命令行工具入口类。
    /// 读取工程根目录下的 <c>MainPackage.txt</c> 获取目标包列表，并调用 <see cref="CodeModeChangeHelper.ChangeToCodeMode"/> 刷新所有包的 <c>.asmref</c> 文件。
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Options options = null;
            // 命令行参数
            Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs())
                    .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                    .WithParsed((o) => options = o);

            string mainPackagePath = Path.Combine("./", "MainPackage.txt");

            if (!File.Exists(mainPackagePath))
            {
                Console.WriteLine("MainPackage.txt not found, skip change codemode.");
                return 0;
            }

            // 读取 MainPackage.txt 中的主包与依赖包列表（忽略大小写去重）
            string[] lines = File.ReadAllLines(mainPackagePath);
            HashSet<string> packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in lines)
            {
                string pkg = line.Trim();
                if (!string.IsNullOrWhiteSpace(pkg))
                {
                    packages.Add(pkg);
                }
            }

            if (packages.Count == 0)
            {
                Console.WriteLine("MainPackage.txt is empty, skip change codemode.");
                return 0;
            }

            CodeModeChangeHelper.ChangeToCodeMode(options.CodeMode.ToString(), packages);

            Console.WriteLine("change codemode ok!");
            return 0;
        }
    }
}
