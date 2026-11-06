using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Path = System.IO.Path;

namespace ET
{
    /// <summary>
    /// 初始化包内部使用的跨平台外部进程启动辅助类（供 <c>com.etetet.init</c> 独立编辑器程序集调用 PowerShell 与 dotnet 命令）。
    /// </summary>
    internal static class ProcessHelper
    {
        /// <summary>
        /// 跨平台启动 PowerShell 进程执行指定参数（Windows 下使用 <c>powershell.exe</c>，macOS/Linux 下自动查找 <c>pwsh</c>）。
        /// </summary>
        /// <param name="arguments">命令行参数。</param>
        /// <param name="workingDirectory">工作目录，默认为当前目录 <c>"."</c>。</param>
        /// <param name="waitExit">是否重定向标准输出/错误流以等待进程退出。</param>
        /// <returns>启动的 <see cref="Process"/> 实例。</returns>
        public static Process PowerShell(string arguments, string workingDirectory = ".", bool waitExit = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Run("powershell.exe", arguments, workingDirectory, waitExit);
            }

            string pwsh = "/usr/local/bin/pwsh";
            if (!System.IO.File.Exists(pwsh))
            {
                if (System.IO.File.Exists("/opt/homebrew/bin/pwsh"))
                {
                    pwsh = "/opt/homebrew/bin/pwsh";
                }
                else
                {
                    pwsh = "pwsh";
                }
            }

            return Run(pwsh, arguments, workingDirectory, waitExit);
        }
        
        /// <summary>
        /// 跨平台启动 <c>dotnet</c> CLI 进程执行指定参数。
        /// </summary>
        /// <param name="arguments">传给 <c>dotnet</c> 的命令行参数。</param>
        /// <param name="workingDirectory">工作目录，默认为 <c>"."</c>。</param>
        /// <param name="waitExit">是否重定向输出流并等待进程退出。</param>
        /// <returns>启动的 <see cref="Process"/> 实例。</returns>
        public static Process DotNet(string arguments, string workingDirectory = ".", bool waitExit = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Run("dotnet.exe", arguments, workingDirectory, waitExit);
            }

            string dotnet = "/usr/local/share/dotnet/dotnet";
            if (!System.IO.File.Exists(dotnet))
            {
                if (System.IO.File.Exists("/opt/homebrew/bin/dotnet"))
                {
                    dotnet = "/opt/homebrew/bin/dotnet";
                }
                else
                {
                    dotnet = "dotnet";
                }
            }

            return Run(dotnet, arguments, workingDirectory, waitExit);
        }

        /// <summary>
        /// 启动指定的可执行文件进程，并在 <paramref name="waitExit"/> 为 true 时自动将标准输出和错误流转发至 Unity 控制台日志。
        /// </summary>
        /// <param name="exe">可执行文件路径或名称。</param>
        /// <param name="arguments">命令行参数。</param>
        /// <param name="workingDirectory">工作目录。</param>
        /// <param name="waitExit">是否开启无窗口重定向输出模式。</param>
        /// <returns>启动的 <see cref="Process"/> 实例。</returns>
        public static Process Run(string exe, string arguments, string workingDirectory = ".", bool waitExit = false)
        {
            //Log.Debug($"Process Run exe:{exe} ,arguments:{arguments} ,workingDirectory:{workingDirectory}");
            try
            {
                bool redirectStandardOutput = false;
                bool redirectStandardError = false;
                bool useShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                
                if (waitExit)
                {
                    redirectStandardOutput = true;
                    redirectStandardError = true;
                    useShellExecute = false;
                }

                if (!string.IsNullOrEmpty(workingDirectory) && !System.IO.Directory.Exists(workingDirectory))
                {
                    System.IO.Directory.CreateDirectory(workingDirectory);
                }
                
                ProcessStartInfo info = new()
                {
                    FileName = exe,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = useShellExecute,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = redirectStandardOutput,
                    RedirectStandardError = redirectStandardError,
                };

                if (redirectStandardOutput)
                {
                    info.StandardOutputEncoding = Encoding.UTF8;
                }

                if (redirectStandardError)
                {
                    info.StandardErrorEncoding = Encoding.UTF8;
                }

                Process process = Process.Start(info);

                if (waitExit)
                {
                    // 异步读取标准输出
                    process.OutputDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            UnityEngine.Debug.Log(args.Data);
                        }
                    };
            
                    // 异步读取错误输出
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            UnityEngine.Debug.LogError(args.Data);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                return process;
            }
            catch (Exception e)
            {
                throw new Exception($"dir: {Path.GetFullPath(workingDirectory)}, command: {exe} {arguments}", e);
            }
        }
    }
}
