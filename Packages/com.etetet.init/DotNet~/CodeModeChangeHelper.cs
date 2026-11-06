using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    /// <summary>
    /// 代码运行模式（CodeMode）切换辅助类。
    /// 通过在各包（<c>Packages/cn.etetet.*</c>）的子目录中动态生成或删除 Unity 的程序集引用文件（<c>AssemblyReference.asmref</c>），
    /// 控制哪些包的哪些代码层（Model/Hotfix/ModelView/HotfixView/Core/Loader/Config/Editor）以及哪些端（Client/Server/Share/Test）汇入对应的主程序集参与编译。
    /// </summary>
    public static class CodeModeChangeHelper
    {
        /// <summary>扫描包的根路径列表。</summary>
        private static readonly string[] moduleDirs = { "Packages", "Library/PackageCache" };

        /// <summary>包内源码目录类型：手写源码目录 <c>Scripts</c> 与工具生成代码目录 <c>CodeMode</c>。</summary>
        private static readonly string[] scriptDirs = { "Scripts", "CodeMode" };

        /// <summary>ET 架构分层目录名称，对应汇入 <c>ET.{modelDir}</c> 程序集。</summary>
        private static readonly string[] modelDirs = { "Model", "Hotfix", "ModelView", "HotfixView", "Core", "Loader", "Config", "Editor" };

        /// <summary>端侧隔离目录名称。</summary>
        private static readonly string[] serverDirs = { "Server", "Client", "Share", "Test", "ClientServer" };

        /// <summary>
        /// 白名单集合：定义了在每种 <c>CodeMode</c>（Client / Server / ClientServer）下允许生成 <c>AssemblyReference.asmref</c> 的目录相对路径组合。
        /// </summary>
        private static readonly HashSet<string> v = new()
        {
            "Client/Scripts/Model/Client",
            "Client/Scripts/Model/Share",
            "Client/CodeMode/Model/Client",
            "Client/CodeMode/Config/Client",
            "Client/Scripts/ModelView/Client",
            "Client/Scripts/ModelView/Share",
            "Client/CodeMode/ModelView/Client",
            "Client/Scripts/Hotfix/Client",
            "Client/Scripts/Hotfix/Share",
            "Client/CodeMode/Hotfix/Client",
            "Client/Scripts/HotfixView/Client",
            "Client/Scripts/HotfixView/Share",
            "Client/CodeMode/HotfixView/Client",
            "Client/Scripts/Core/Client",
            "Client/Scripts/Core/Share",
            "Client/CodeMode/Core/Client",
            "Client/Scripts/Loader/Client",
            "Client/Scripts/Loader/Share",
            "Client/CodeMode/Loader/Client",
            "Client/Scripts/Editor/Share",
            "Client/Scripts/Editor/Client",
            "Client/CodeMode/Editor/Client",

            "Server/Scripts/Model/Server",
            "Server/Scripts/Model/Share",
            "Server/CodeMode/Model/Server",
            "Server/CodeMode/Config/Server",
            "Server/Scripts/Hotfix/Server",
            "Server/Scripts/Hotfix/Share",
            "Server/CodeMode/Hotfix/Server",
            "Server/Scripts/Core/Server",
            "Server/Scripts/Core/Share",
            "Server/CodeMode/Core/Server",
            "Server/Scripts/Loader/Server",
            "Server/Scripts/Loader/Share",
            "Server/CodeMode/Loader/Server",
            "Server/Scripts/Editor/Share",

            "ClientServer/Scripts/Model/Client",
            "ClientServer/Scripts/Model/Server",
            "ClientServer/Scripts/Model/Share",
            "ClientServer/CodeMode/Model/ClientServer",
            "ClientServer/CodeMode/Config/ClientServer",
            "ClientServer/Scripts/ModelView/Client",
            "ClientServer/Scripts/ModelView/Server",
            "ClientServer/Scripts/ModelView/Share",
            "ClientServer/CodeMode/ModelView/ClientServer",
            "ClientServer/Scripts/Hotfix/Client",
            "ClientServer/Scripts/Hotfix/Server",
            "ClientServer/Scripts/Hotfix/Share",
            "ClientServer/CodeMode/Hotfix/ClientServer",
            "ClientServer/Scripts/HotfixView/Client",
            "ClientServer/Scripts/HotfixView/Server",
            "ClientServer/Scripts/HotfixView/Share",
            "ClientServer/CodeMode/HotfixView/ClientServer",
            "ClientServer/Scripts/Core/Client",
            "ClientServer/Scripts/Core/Share",
            "ClientServer/Scripts/Core/Server",
            "ClientServer/CodeMode/Core/ClientServer",
            "ClientServer/Scripts/Loader/Client",
            "ClientServer/Scripts/Loader/Share",
            "ClientServer/Scripts/Loader/Server",
            "ClientServer/CodeMode/Loader/ClientServer",
            "ClientServer/Scripts/Editor/Share",
            "ClientServer/Scripts/Editor/Client",
            "ClientServer/CodeMode/Editor/ClientServer",
            "ClientServer/Scripts/Editor/Test",
            "ClientServer/Scripts/Hotfix/Test",
            "ClientServer/Scripts/HotfixView/Test",
            "ClientServer/Scripts/Model/Test",
            "ClientServer/Scripts/ModelView/Test",
        };

        /// <summary>
        /// 遍历所有 <c>cn.etetet.*</c> 包，根据指定的 <paramref name="codeMode"/> 和主包依赖集合 <paramref name="targetPackages"/> 更新各目录下的 <c>AssemblyReference.asmref</c>。
        /// </summary>
        /// <param name="codeMode">目标代码模式名称（"Client"、"Server" 或 "ClientServer"）。</param>
        /// <param name="targetPackages">从 <c>MainPackage.txt</c> 读取的当前生效包名集合。</param>
        public static void ChangeToCodeMode(string codeMode, HashSet<string> targetPackages)
        {
            Console.WriteLine($"目标包列表: {string.Join(", ", targetPackages)}");

            foreach (string a in moduleDirs)
            {
                if (!Directory.Exists(a))
                {
                    continue;
                }

                foreach (string moduleDir in Directory.GetDirectories(a, "cn.etetet.*"))
                {
                    string packageName = Path.GetFileName(moduleDir);
                    bool isTargetPackage = targetPackages.Contains(packageName);

                    foreach (string scriptDir in scriptDirs)
                    {
                        string p = Path.Combine(moduleDir, scriptDir);
                        if (!Directory.Exists(p))
                        {
                            continue;
                        }

                        foreach (string modelDir in modelDirs)
                        {
                            string c = Path.Combine(p, modelDir);
                            if (!Directory.Exists(c))
                            {
                                continue;
                            }

                            foreach (string serverDir in serverDirs)
                            {
                                string e = Path.Combine(c, serverDir);
                                if (!Directory.Exists(e))
                                {
                                    continue;
                                }

                                // 对于目标包，根据 codeMode 处理；Editor 作为通用扩展入口，对所有包保留。
                                HandleAssemblyReferenceFile(codeMode, moduleDir, scriptDir, modelDir, serverDir, isTargetPackage);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 单个目录的 <c>AssemblyReference.asmref</c> 清理与重建逻辑：
        /// 先删除已有的 <c>.asmref</c>，若该包属于目标依赖包（或者是通用的 Editor 目录）且路径组合在当前 <paramref name="codeMode"/> 白名单中，则重新生成引用文件。
        /// </summary>
        private static void HandleAssemblyReferenceFile(string codeMode, string moduleDir, string scriptDir, string modelDir, string serverDir, bool isTargetPackage)
        {
            string filePath = Path.Combine(moduleDir, scriptDir, modelDir, serverDir, "AssemblyReference.asmref");
            DeleteAssemblyReference(filePath);

            if (isTargetPackage || modelDir == "Editor")
            {
                string path = $"{codeMode}/{scriptDir}/{modelDir}/{serverDir}";
                if (v.Contains(path))
                {
                    CreateAssemblyReference(filePath, modelDir);
                }
            }
        }

        /// <summary>
        /// 若存在指定的 <c>AssemblyReference.asmref</c> 文件则将其删除。
        /// </summary>
        private static void DeleteAssemblyReference(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 创建 <c>AssemblyReference.asmref</c> 文件，使其目录下的脚本汇入 <c>ET.{modelDir}</c> 程序集。
        /// </summary>
        private static void CreateAssemblyReference(string path, string modelDir)
        {
            File.WriteAllText(path, $"{{ \"reference\": \"ET.{modelDir}\" }}");
        }
    }
}
