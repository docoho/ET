using System;
using System.ComponentModel;
using System.IO;
using CommandLine;
using OfficeOpenXml;

namespace ET
{
    /// <summary>
    /// <c>ET.ExcelExporter.dll</c> 配置表导出命令行工具入口类。
    /// 依次执行所有 <see cref="IExcelHandler"/> 预处理器，随后调用 <see cref="LubanGen.CreateLubanConf"/> 将所有 Luban Excel 配置导出为 C# 代码与数据。
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                // 设置 EPPlus 非商业开源许可证上下文
                ExcelPackage.License.SetNonCommercialOrganization("ETET");
                
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Log.Console(e.ExceptionObject.ToString());
                };
                
                // 命令行参数
                Options options = World.Instance.AddSingleton<Options>();
                options.Console = 1;
                
                World.Instance.AddSingleton<Logger>().Log = new NLogger("ExcelExporter");
                
                // 反射扫描当前程序集中所有实现了 IExcelHandler 的预处理器并执行
                foreach (Type type in typeof(Program).Assembly.GetTypes())
                {
                    if (!typeof(IExcelHandler).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (type == typeof(IExcelHandler))
                    {
                        continue;
                    }
                    
                    IExcelHandler iExcelHandler = Activator.CreateInstance(type) as IExcelHandler;
                    iExcelHandler.Run();
                }
                
                // 调用 Luban 生成器导出代码模式（Code）配置
                if (!LubanGen.CreateLubanConf("Code"))
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
            Console.WriteLine("excelexporter ok!");
            return 0;
        }
    }
}
