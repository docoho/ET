using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Path = System.IO.Path;

namespace ET
{
    internal static class ProcessHelper
    {
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
