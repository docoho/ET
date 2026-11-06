using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace ET
{
    /// <summary>
    /// 单个 Luban 配置集合（如 <c>Config</c>、<c>Localhost</c> 等）的跨包路径聚合信息。
    /// </summary>
    public class LubanInfo
    {
        /// <summary>包含主 <c>luban.conf</c> 和 <c>LubanGen.ps1</c> 的目录路径。</summary>
        public string LubanConfigPath;

        /// <summary>所有包下属于同一配置集合的子目录列表（用于合并各包的 <c>__tables__.xlsx</c>、<c>__beans__.xlsx</c>、<c>__enums__.xlsx</c> 和 <c>Defines</c>）。</summary>
        public List<string> subConfigDir = new List<string>();
    }

    /// <summary>
    /// Luban 配置表跨包聚合与代码/数据导出驱动器。
    /// 扫描所有 <c>Packages/cn.etetet.*/Luban/*</c> 目录，按配置集合名聚合 Schema 文件列表写入 <c>luban.conf</c>，并调用各集合下的 <c>LubanGen.ps1</c> 完成导表。
    /// </summary>
    public static class LubanGen
    {
        /// <summary>
        /// 对应 <c>luban.conf</c> 中 <c>schemaFiles</c> 数组的序列化节点结构。
        /// </summary>
        private class SchemaFile
        {
            public string fileName { get; set; }
            public string type { get; set; }
        }

        private static Dictionary<string, LubanInfo> lubanInfos = new();

        /// <summary>
        /// 扫描全工程各包的 <c>Luban</c> 目录，动态更新 <c>luban.conf</c> 的 <c>schemaFiles</c> 并执行 <c>LubanGen.ps1</c> 导出。
        /// </summary>
        /// <param name="configType">导出目标类型参数（如 <c>"Code"</c>）。</param>
        /// <returns>全部配置集合均导出成功返回 true，否则返回 false。</returns>
        public static bool CreateLubanConf(string configType)
        {
            lubanInfos.Clear();
            foreach (string packageDir in Directory.GetDirectories("Packages", "cn.etetet.*"))
            {
                string d = Path.Combine(packageDir, "Luban");
                if (!Directory.Exists(d))
                {
                    continue;
                }

                foreach (string directory in Directory.GetDirectories(d))
                {
                    string configCollectionName = Path.GetFileName(directory);
                    if (!lubanInfos.TryGetValue(configCollectionName, out LubanInfo lubanInfo))
                    {
                        lubanInfo = new LubanInfo();
                        lubanInfos.Add(configCollectionName, lubanInfo);
                    }

                    if (File.Exists(Path.Combine(directory, "luban.conf")))
                    {
                        lubanInfo.LubanConfigPath = directory;
                    }

                    lubanInfo.subConfigDir.Add(directory);
                }
            }

            foreach ((string configCollectionsName, LubanInfo lubanInfo) in lubanInfos)
            {
                List<SchemaFile> AllSchemaFile = new();
                foreach (var configPath in lubanInfo.subConfigDir)
                {
                    var tablesPath = Path.Combine(configPath, "Base/__tables__.xlsx");
                    if (File.Exists(tablesPath))
                    {
                        AllSchemaFile.Add(new() { fileName = $"../../../../{tablesPath}", type = "table" });
                    }

                    var beansPath = Path.Combine(configPath, "Base/__beans__.xlsx");
                    if (File.Exists(beansPath))
                    {
                        AllSchemaFile.Add(new() { fileName = $"../../../../{beansPath}", type = "bean" });
                    }

                    var enumsPath = Path.Combine(configPath, "Base/__enums__.xlsx");
                    if (File.Exists(enumsPath))
                    {
                        AllSchemaFile.Add(new() { fileName = $"../../../../{enumsPath}", type = "enum" });
                    }

                    var definesPath = Path.Combine(configPath, "Defines");
                    if (Directory.Exists(definesPath))
                    {
                        AllSchemaFile.Add(new() { fileName = $"../../../../{definesPath}", type = "" });
                    }
                }

                var packagePath = $"{lubanInfo.LubanConfigPath}/luban.conf";

                if (!File.Exists(packagePath))
                {
                    Console.WriteLine($"[ERROR] 源文件 luban.conf 不存在 请重新初始化LubanGen");
                    Console.Out.Flush();
                    return false;
                }

                try
                {
                    string fileContent = File.ReadAllText(packagePath);

                    var json = JsonDocument.Parse(fileContent).RootElement;
                    var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(fileContent);

                    jsonDict["schemaFiles"] = AllSchemaFile;

                    string updatedJsonContent = JsonSerializer.Serialize(jsonDict, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    File.WriteAllText(packagePath, updatedJsonContent, Encoding.UTF8);

                    Console.WriteLine($"[INFO] 开始导出 {configCollectionsName}");
                    Console.Out.Flush();
                    int exitCode = RunLubanGen(lubanInfo.LubanConfigPath, configType);
                    if (exitCode != 0)
                    {
                        Console.WriteLine($"[ERROR] Luban 导出失败 {configCollectionsName} ExitCode={exitCode}");
                        Console.Out.Flush();
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[ERROR] 创建 LubanConf失败 {configCollectionsName} {e.Message}");
                    Console.Out.Flush();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 调用 PowerShell 执行指定配置集合目录下的 <c>LubanGen.ps1</c> 脚本。
        /// </summary>
        private static int RunLubanGen(string configCollectionPath, string configType)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RunProcess("pwsh.exe", $"-ExecutionPolicy Bypass -File {Path.Combine(configCollectionPath, "LubanGen.ps1")} -ConfigType {configType}");
            }
            else
            {
                return RunProcess("/usr/local/bin/pwsh", $"-ExecutionPolicy Bypass -File {Path.Combine(configCollectionPath, "LubanGen.ps1")} -ConfigType {configType}");
            }
        }

        /// <summary>
        /// 在 Unix/macOS 系统下调用 <c>/bin/chmod</c> 修改文件权限。
        /// </summary>
        public static void ChangePermissions(string filePath, string permissions)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"{permissions} \"{filePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();

                    // You can handle the output if needed  
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"[ERROR] Failed to change file permissions: {e.Message}");
                Console.Out.Flush();
            }
        }

        /// <summary>
        /// 启动子进程并实时流式打印 Luban 标准输出与错误输出（带 20 秒超时保护）。
        /// </summary>
        private static int RunProcess(string exe, string arguments, string workingDirectory = ".", bool waitExit = true)
        {
            var redirectStandardOutput = false;
            var redirectStandardError = false;
            var useShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (waitExit)
            {
                redirectStandardOutput = true;
                redirectStandardError = true;
                useShellExecute = false;
            }

            var ImportAllOutput = new StringBuilder();
            var ImportAllError = new StringBuilder();
            var importProcess = new Process();
            importProcess.StartInfo.FileName = exe;
            importProcess.StartInfo.Arguments = arguments;
            importProcess.StartInfo.WorkingDirectory = workingDirectory;
            importProcess.StartInfo.UseShellExecute = useShellExecute;
            importProcess.StartInfo.CreateNoWindow = true;
            importProcess.StartInfo.RedirectStandardOutput = redirectStandardOutput;
            importProcess.StartInfo.RedirectStandardError = redirectStandardError;
            importProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            importProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            importProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // 实时输出每一行到控制台
                    Console.WriteLine($"[LUBAN] {args.Data}");
                    Console.Out.Flush();

                    if (args.Data.Contains("ERROR"))
                    {
                        ImportAllError.AppendLine(args.Data);
                    }

                    ImportAllOutput.AppendLine(args.Data);
                }
            };
            importProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    // 实时输出错误信息到控制台
                    Console.WriteLine($"[LUBAN ERROR] {args.Data}");
                    Console.Out.Flush();

                    ImportAllError.AppendLine(args.Data);
                }
            };

            try
            {
                importProcess.Start();
                importProcess.BeginOutputReadLine();
                importProcess.BeginErrorReadLine();
                if (!importProcess.WaitForExit(20000))
                {
                    importProcess.Kill();
                    Console.WriteLine($"[LUBAN ERROR] 导出超时，已终止进程: {exe} {arguments}");
                    Console.Out.Flush();
                    return -1;
                }

                importProcess.WaitForExit();

                // 由于已经实时输出了，这里只输出总结信息
                var error = ImportAllError.ToString();
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"[LUBAN] 导出过程中有错误，请检查上面的输出");
                    Console.Out.Flush();
                }
                else
                {
                    Console.WriteLine($"[LUBAN] 导出完成");
                    Console.Out.Flush();
                }
                return importProcess.ExitCode;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[LUBAN ERROR] 导出执行报错: {e.Message}");
                Console.Out.Flush();
            }
            finally
            {
                importProcess.Close();
            }
            return -1;
        }
    }
}
