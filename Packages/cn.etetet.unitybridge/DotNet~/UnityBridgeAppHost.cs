using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using ET.Server;

namespace ET
{
    /// <summary>
    /// UnityBridge 命令行端应用宿主，负责初始化日志、加载 ET.Core/ET.Model/ET.Hotfix 程序集并完成 CodeTypes 与 MongoDB 序列化注册。
    /// </summary>
    internal sealed class UnityBridgeAppHost
    {
        private static bool started;
        private static Assembly hotfixAssembly;
        private static AssemblyLoadContext hotfixAssemblyLoadContext;

        /// <summary>
        /// 幂等启动宿主环境，完成反射类型扫描与 BSON 序列化注册
        /// </summary>
        public void Start()
        {
            if (started)
            {
                return;
            }

            started = true;

            AvoidCut.Init();

            AppDomain.CurrentDomain.UnhandledException += static (_, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };

            if (Logger.Instance == null)
            {
                World.Instance.AddSingleton<Logger>().Log = new NLogger("UnityBridge");
            }

            ETTask.ExceptionHandler += Log.Error;

            List<Assembly> assemblies = new();
            HashSet<string> assemblyNames = new(StringComparer.Ordinal)
            {
                "ET.Core",
                "ET.Loader",
                "ET.Model"
            };

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name;
                if (!assemblyNames.Contains(assemblyName))
                {
                    continue;
                }

                assemblies.Add(assembly);
            }

            hotfixAssembly = this.LoadHotfix();
            assemblies.Add(hotfixAssembly);

            if (CodeTypes.Instance == null)
            {
                World.Instance.AddSingleton<CodeTypes, Assembly[]>(assemblies.ToArray());
            }

            MongoRegister.Init();
        }

        private Assembly LoadHotfix()
        {
            if (hotfixAssembly != null)
            {
                return hotfixAssembly;
            }

            string dllPath = Path.GetFullPath("./Bin/ET.Hotfix.dll");
            string pdbPath = Path.GetFullPath("./Bin/ET.Hotfix.pdb");
            if (!File.Exists(dllPath))
            {
                throw new FileNotFoundException($"unity bridge hotfix dll not found: {dllPath}");
            }

            hotfixAssemblyLoadContext = new AssemblyLoadContext("ET.UnityBridge.Hotfix", true);
            using MemoryStream dllStream = new(File.ReadAllBytes(dllPath));
            using MemoryStream pdbStream = File.Exists(pdbPath) ? new MemoryStream(File.ReadAllBytes(pdbPath)) : null;
            hotfixAssembly = pdbStream == null
                    ? hotfixAssemblyLoadContext.LoadFromStream(dllStream)
                    : hotfixAssemblyLoadContext.LoadFromStream(dllStream, pdbStream);
            return hotfixAssembly;
        }
    }
}
