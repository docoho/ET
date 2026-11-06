using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    /// <summary>
    /// <c>ET.Proto2CS.dll</c> 协议导出命令行工具入口类。
    /// 读取工程根目录下的 <c>MainPackage.txt</c>，调用 <see cref="Proto2CS.Export"/> 将目标包中的 <c>*.proto</c> 协议定义文件转换为 C# 消息类代码。
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string mainPackagePath = Path.Combine("./", "MainPackage.txt");

                if (!File.Exists(mainPackagePath))
                {
                    Console.WriteLine("MainPackage.txt not found, skip proto export.");
                    return 0;
                }

                // 读取主包与依赖包列表
                string[] lines = File.ReadAllLines(mainPackagePath);
                HashSet<string> packages = new HashSet<string>();
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
                    Console.WriteLine("MainPackage.txt is empty, skip proto export.");
                    return 0;
                }

                Proto2CS.Export(packages);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("proto2cs ok!");
            return 1;
        }
    }
}