using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 服务端启动运行模式枚举。
    /// </summary>
    public enum DevelopMode
    {
        正式 = 0,
        开发 = 1,
        压测 = 2,
    }

    /// <summary>
    /// 服务端命令行控制工具窗口（菜单项：<c>ET/Loader/ServerTools</c>）。
    /// 提供在 Unity 编辑器内实时监控游戏服务端（<c>ET.App.dll</c>）与 MongoDB 运行状态、
    /// 选择 <c>StartConfig</c> 环境配置、一键启动/停止单进程服务端或守护进程（Watcher），以及打开/清理服务端日志等功能。
    /// </summary>
    public class ServerCommandLineEditor: EditorWindow
    {
        /// <summary>
        /// 打开服务端控制工具窗口。
        /// </summary>
        [MenuItem("ET/Loader/ServerTools", false)]
        public static void ShowWindow()
        {
            var window = GetWindow<ServerCommandLineEditor>("Server Tools");
            window.minSize = new Vector2(400, 420);
        }

        private int selectStartConfigIndex = 0;
        private string[] startConfigs;
        private string startConfig;
        private DevelopMode developMode;
        private bool openInTerminal = true;
        private double lastCheckTime;
        private bool isServerRunning;
        private List<int> serverPids = new();
        private bool isMongoRunning;

        /// <summary>
        /// 窗口启用时扫描 <c>Packages/cn.etetet.startconfig/Luban</c> 下的所有启动配置子目录名称，并初始化服务运行状态检测。
        /// </summary>
        public void OnEnable()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo("Packages/cn.etetet.startconfig/Luban");
            if (directoryInfo.Exists)
            {
                this.startConfigs = directoryInfo.GetDirectories().Select(x => x.Name).OrderBy(x => x).ToArray();
                this.selectStartConfigIndex = Mathf.Clamp(this.selectStartConfigIndex, 0, Mathf.Max(this.startConfigs.Length - 1, 0));
            }
            else
            {
                this.startConfigs = new[] { "Localhost" };
                this.selectStartConfigIndex = 0;
            }

            // macOS / Windows 默认在独立终端中打开
            this.openInTerminal = EditorPrefs.GetBool("ET_ServerTools_OpenInTerminal", true);
            RefreshStatus();
        }

        /// <summary>
        /// 窗口关闭时保存“是否在独立终端窗口中启动”的用户偏好设置。
        /// </summary>
        private void OnDisable()
        {
            EditorPrefs.SetBool("ET_ServerTools_OpenInTerminal", this.openInTerminal);
        }

        /// <summary>
        /// 编辑器每帧回调：每隔 1 秒自动刷新一次服务端进程与 MongoDB 端口存活状态。
        /// </summary>
        private void Update()
        {
            if (EditorApplication.timeSinceStartup - lastCheckTime > 1.0f)
            {
                lastCheckTime = EditorApplication.timeSinceStartup;
                RefreshStatus();
                Repaint();
            }
        }

        /// <summary>
        /// 刷新 MongoDB 与游戏服务端进程的运行状态。
        /// </summary>
        private void RefreshStatus()
        {
            isMongoRunning = CheckMongoRunning();
            isServerRunning = CheckServerRunning(out serverPids);
        }

        /// <summary>
        /// 绘制服务端控制面板 GUI。
        /// </summary>
        public void OnGUI()
        {
            GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            if (globalConfig == null)
            {
                EditorGUILayout.HelpBox("未找到 GlobalConfig 资源，请确认 Resources/GlobalConfig 是否存在！", MessageType.Error);
                return;
            }

            EditorGUILayout.Space(8);
            DrawStatusSection();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("配置设置", EditorStyles.boldLabel);

            if (this.startConfigs != null && this.startConfigs.Length > 0)
            {
                selectStartConfigIndex = EditorGUILayout.Popup("StartConfig：", selectStartConfigIndex, this.startConfigs);
                this.startConfig = this.startConfigs[this.selectStartConfigIndex];
            }
            else
            {
                this.startConfig = "Localhost";
                EditorGUILayout.LabelField("StartConfig：", this.startConfig);
            }

            this.developMode = (DevelopMode)EditorGUILayout.EnumPopup("起服模式：", this.developMode);

            this.openInTerminal = EditorGUILayout.Toggle(new GUIContent("在独立终端窗口中启动", "推荐勾选。在独立终端中运行可实时查看彩色服务端日志、输出交互命令与启动错误。"), this.openInTerminal);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("服务端控制", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isServerRunning);
            if (GUILayout.Button("Start Server (Single Process)", GUILayout.Height(30)))
            {
                StartServer(globalConfig.SceneName, this.startConfig, this.openInTerminal);
                RefreshStatus();
            }

            if (GUILayout.Button("Start Watcher", GUILayout.Height(26)))
            {
                StartServer("Watcher", this.startConfig, this.openInTerminal);
                RefreshStatus();
            }
            EditorGUI.EndDisabledGroup();

            if (isServerRunning)
            {
                EditorGUILayout.Space(4);
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button($"停止服务端 (PID: {string.Join(", ", serverPids)})", GUILayout.Height(30)))
                {
                    StopServer();
                    RefreshStatus();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("数据库与日志工具", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isMongoRunning ? "MongoDB 运行中" : "Start Mongo", GUILayout.Height(26)))
            {
                StartMongo();
                RefreshStatus();
            }

            if (GUILayout.Button("打开日志目录", GUILayout.Height(26)))
            {
                string logDir = Path.GetFullPath("Logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                EditorUtility.RevealInFinder(logDir);
            }

            if (GUILayout.Button("清空服务端日志", GUILayout.Height(26)))
            {
                ClearLogs();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制顶部的游戏服务端与 MongoDB 运行状态指示区域。
        /// </summary>
        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("服务运行状态", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 服务端状态
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("游戏服务端：", GUILayout.Width(90));
            if (isServerRunning)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField($"● 运行中 (PID: {string.Join(", ", serverPids)})", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("○ 未运行", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            // Mongo 状态
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MongoDB：", GUILayout.Width(90));
            if (isMongoRunning)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("● 运行中 (127.0.0.1:27017)", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = new Color(1f, 0.5f, 0f);
                EditorGUILayout.LabelField("○ 未运行 (请先点击 Start Mongo)", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 检测当前机器上是否有正在运行的 <c>Bin/ET.App.dll</c> 服务端进程，并返回其进程 PID 列表。
        /// </summary>
        public static bool CheckServerRunning(out List<int> pids)
        {
            pids = new List<int>();
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var processes = Process.GetProcessesByName("dotnet");
                    foreach (var p in processes)
                    {
                        try
                        {
                            pids.Add(p.Id);
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = "-c \"pgrep -f 'Bin/ET.App.dll'\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit();
                        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (int.TryParse(line.Trim(), out int pid))
                            {
                                pids.Add(pid);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return pids.Count > 0;
        }

        /// <summary>
        /// 停止所有正在运行的 <c>ET.App.dll</c> 服务端进程。
        /// </summary>
        public static void StopServer()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = "/F /FI \"IMAGENAME eq dotnet.exe\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit();
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = "-c \"pkill -f 'Bin/ET.App.dll'\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit();
                }
                UnityEngine.Debug.Log("<color=yellow>[ET] 服务端进程已停止。</color>");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ET] 停止服务端失败: {e.Message}");
            }
        }

        /// <summary>
        /// 启动 <c>Bin/ET.App.dll</c> 服务端进程（支持在独立终端窗口或后台启动）。
        /// </summary>
        /// <param name="sceneName">启动场景名称（如 <c>AllServer</c> 或 <c>Watcher</c>）。</param>
        /// <param name="startConfig">启动配置目录名称（如 <c>Localhost</c>）。</param>
        /// <param name="openInTerminal">是否在操作系统独立终端窗口中运行。</param>
        private static void StartServer(string sceneName, string startConfig, bool openInTerminal)
        {
            if (CheckServerRunning(out var existingPids))
            {
                string pidsStr = string.Join(", ", existingPids);
                if (!EditorUtility.DisplayDialog("提示", $"检测到已有服务端进程正在运行 (PID: {pidsStr})。\n\n是否停止已有进程并重新启动？", "重新启动", "取消"))
                {
                    return;
                }
                StopServer();
                System.Threading.Thread.Sleep(500);
            }

            string arguments = $"Bin/ET.App.dll --SceneName={sceneName} --Process=1 --StartConfig={startConfig} --Console=1";
            string workingDir = Path.GetFullPath("./");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (openInTerminal)
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/k dotnet {arguments}",
                        WorkingDirectory = workingDir,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                    Process.Start(psi);
                    UnityEngine.Debug.Log($"<color=green>[ET] 已在命令提示符 (CMD) 中启动服务端: dotnet {arguments}</color>");
                }
                else
                {
                    ProcessHelper.DotNet(arguments, "./");
                    UnityEngine.Debug.Log($"<color=green>[ET] 已在后台启动服务端: dotnet {arguments}，日志保存在 Logs/ 目录下</color>");
                }
            }
            else
            {
                string dotnet = GetDotNetPath();

                if (openInTerminal)
                {
                    // 在 macOS Terminal.app 独立终端中打开
                    string script = $"tell application \"Terminal\" to do script \"cd \\\"{workingDir}\\\" && \\\"{dotnet}\\\" {arguments}\"";
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e '{script.Replace("'", "'\\''")}'",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(psi);
                    UnityEngine.Debug.Log($"<color=green>[ET] 已在独立终端 (Terminal.app) 中启动服务端: {dotnet} {arguments}</color>");
                }
                else
                {
                    ProcessHelper.DotNet(arguments, "./");
                    UnityEngine.Debug.Log($"<color=green>[ET] 已在后台启动服务端: {dotnet} {arguments}，日志保存在 Logs/ 目录下</color>");
                }
            }
        }

        /// <summary>
        /// 获取 macOS / Linux 下 <c>dotnet</c> 可执行文件的绝对路径。
        /// </summary>
        private static string GetDotNetPath()
        {
            if (File.Exists("/opt/homebrew/bin/dotnet"))
            {
                return "/opt/homebrew/bin/dotnet";
            }
            if (File.Exists("/usr/local/share/dotnet/dotnet"))
            {
                return "/usr/local/share/dotnet/dotnet";
            }
            if (File.Exists("/usr/local/bin/dotnet"))
            {
                return "/usr/local/bin/dotnet";
            }
            return "dotnet";
        }

        /// <summary>
        /// 清空 <c>Logs/</c> 目录下的所有 <c>*.log</c> 服务端日志文件。
        /// </summary>
        private static void ClearLogs()
        {
            if (!EditorUtility.DisplayDialog("确认", "确定要清空 Logs/ 目录下的日志文件吗？", "确定", "取消"))
            {
                return;
            }

            try
            {
                string logDir = Path.GetFullPath("Logs");
                if (Directory.Exists(logDir))
                {
                    var files = Directory.GetFiles(logDir, "*.log");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                        }
                    }
                    UnityEngine.Debug.Log($"[ET] 已清空 Logs 目录下共 {files.Length} 个日志文件。");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[ET] 清空日志失败: {e.Message}");
            }
        }

        /// <summary>
        /// 通过尝试建立 TCP 连接检测本地 MongoDB（默认 <c>127.0.0.1:27017</c>）是否正在运行。
        /// </summary>
        private static bool CheckMongoRunning(string host = "127.0.0.1", int port = 27017)
        {
            try
            {
                using var tcpClient = new TcpClient();
                var result = tcpClient.BeginConnect(host, port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(300);
                if (success && tcpClient.Connected)
                {
                    tcpClient.EndConnect(result);
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// 启动本地 MongoDB 数据库服务。
        /// </summary>
        private static void StartMongo()
        {
            if (CheckMongoRunning())
            {
                UnityEngine.Debug.Log("MongoDB 已经在 127.0.0.1:27017 运行中，无需重复启动。");
                EditorUtility.DisplayDialog("提示", "MongoDB 已经在 127.0.0.1:27017 运行中，无需重复启动！", "确定");
                return;
            }

            string workingDir = Path.GetFullPath("../Database/bin/");
            string dbDir = Path.Combine(workingDir, "db");
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }
            if (!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ProcessHelper.Run("mongod", @"--dbpath=db", workingDir);
            }
            else
            {
                // macOS / Linux: 优先通过 brew services 启动，或者直接启动可执行文件
                if (File.Exists("/opt/homebrew/bin/brew"))
                {
                    ProcessHelper.Run("/opt/homebrew/bin/brew", "services start mongodb-community", "./");
                    UnityEngine.Debug.Log("已通过 brew services start mongodb-community 启动 MongoDB。");
                    return;
                }

                string mongodPath = "mongod";
                if (File.Exists("/opt/homebrew/bin/mongod"))
                {
                    mongodPath = "/opt/homebrew/bin/mongod";
                }
                else if (File.Exists("/usr/local/bin/mongod"))
                {
                    mongodPath = "/usr/local/bin/mongod";
                }

                ProcessHelper.Run(mongodPath, $"--dbpath=\"{dbDir}\"", workingDir);
            }
        }
    }
}
