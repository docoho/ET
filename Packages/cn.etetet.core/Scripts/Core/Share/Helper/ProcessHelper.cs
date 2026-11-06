using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Path = System.IO.Path;

namespace ET
{
    /// <summary>
    /// 跨平台外部子进程（<see cref="Process"/>）启动辅助工具类，封装了 PowerShell（<c>pwsh</c>）与 <c>dotnet</c> CLI 的路径探测与标准输出重定向。
    /// </summary>
    public static class ProcessHelper
    {
        /// <summary>
        /// 跨平台启动 PowerShell 7（<c>pwsh</c>）进程并执行传入的参数命令。
        /// </summary>
        /// <param name="arguments">传递给 pwsh 的命令行参数</param>
        /// <param name="workingDirectory">工作目录（默认为当前目录）</param>
        /// <param name="waitExit">是否重定向标准输出与错误流以便异步监听日志</param>
        public static Process PowerShell(string arguments, string workingDirectory = ".", bool waitExit = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Run("pwsh.exe", arguments, workingDirectory, waitExit);
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
        /// 跨平台启动 .NET SDK（<c>dotnet</c>）进程并执行传入的参数命令。
        /// </summary>
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
        /// 启动指定的可执行程序进程，并根据 <paramref name="waitExit"/> 配置 UTF-8 标准输出/错误流异步转发到 <see cref="Log"/>。
        /// </summary>
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
                            Log.Debug(args.Data);
                        }
                    };
            
                    // 异步读取错误输出
                    process.ErrorDataReceived += (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(args.Data))
                        {
                            Log.Error(args.Data);
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