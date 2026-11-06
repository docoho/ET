using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 客户端代码加载器单例。
    /// 负责在客户端启动时加载 Model、ModelView、Hotfix、HotfixView 等程序集（编辑器下直接从当前 AppDomain 获取，打包后通过 YooAsset 加载 DLL/PDB 并补充 HybridCLR 的 AOT 泛型元数据），
    /// 初始化 <see cref="CodeTypes"/> 反射扫描并反射调用热更层入口 <c>ET.Entry.Start()</c>；同时支持开发期的 Hotfix 热重载（<see cref="Reload"/>）。
    /// </summary>
    [AllowInstance]
    public class CodeLoader: Singleton<CodeLoader>, ISingletonAwake
    {
        /// <summary>非编辑器模式下从资源包加载的热更程序集（DLL 与 PDB）资源字典。</summary>
        private Dictionary<string, TextAsset> dlls;
        /// <summary>非编辑器模式下用于 HybridCLR 补充元数据的 AOT 程序集资源字典。</summary>
        private Dictionary<string, TextAsset> aotDlls;
        /// <summary>基础常驻程序集列表（如 Core、Loader、Model、ModelView 等，Reload 时无需重新加载）。</summary>
        private readonly List<Assembly> assemblies = new();

        public void Awake()
        {
        }

        /// <summary>
        /// 异步加载热更 DLL/PDB 与 AOT 元数据 DLL（仅在非 Unity 编辑器打包环境下通过 <see cref="ResourcesComponent"/> 实际加载）。
        /// </summary>
        private async ETTask DownloadAsync()
        {
            try
            {
#if UNITY_EDITOR
                await ETTask.CompletedTask;
#else
                this.dlls = new Dictionary<string, TextAsset>
                {
                    ["ET.Model.dll"] = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.Model.dll"),
                    ["ET.Model.pdb"] = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.Model.pdb"),
                    ["ET.ModelView.dll"] = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.ModelView.dll"),
                    ["ET.ModelView.pdb"] = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.ModelView.pdb"),
                    ["ET.Hotfix.dll"] = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.Hotfix.dll"),
                    ["ET.Hotfix.pdb"] = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.Hotfix.pdb"),
                    ["ET.HotfixView.dll"] = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.HotfixView.dll"),
                    ["ET.HotfixView.pdb"] = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.HotfixView.pdb"),
                };
                this.aotDlls = await ResourcesComponent.Instance.LoadAllAssetsAsync<TextAsset>($"mscorlib.dll");
#endif
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        /// <summary>
        /// 启动代码加载流程：
        /// 1. 下载/加载所需的程序集资源；
        /// 2. 收集 <c>ET.Core</c>、<c>ET.Loader</c>、<c>ET.Model</c>、<c>ET.ModelView</c>、<c>ET.Hotfix</c>、<c>ET.HotfixView</c> 程序集；
        /// 3. 注册全局 <see cref="CodeTypes"/> 单例并反射执行 <c>ET.Entry.Start()</c> 进入热更逻辑层。
        /// </summary>
        public async ETTask Start()
        {
            await DownloadAsync();
            
            
            HashSet<string> assemblyNames = new()
            {
                "ET.Core",
                "ET.Loader",
#if UNITY_EDITOR     
                "ET.Model",
                "ET.ModelView",
                "ET.Editor",
#endif
            };

            Assembly[] domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in domainAssemblies)
            {
                string name = assembly.GetName().Name;
                if (assemblyNames.Contains(name))
                {
                    assemblies.Add(assembly);
                }
            }
            List<Assembly> list = new(this.assemblies);
            
#if !UNITY_EDITOR
            byte[] modelAssBytes = this.dlls["ET.Model.dll"].bytes;
            byte[] modelPdbBytes = this.dlls["ET.Model.pdb"].bytes;
            byte[] modelViewAssBytes = this.dlls["ET.ModelView.dll"].bytes;
            byte[] modelViewPdbBytes = this.dlls["ET.ModelView.pdb"].bytes;

            // 如果需要测试，可替换成下面注释的代码直接加载Packages/cn.etetet.loader/Code/ET.Model.dll.bytes，但真正打包时必须使用上面的代码
            //byte[] modelAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Model.dll.bytes"));
            //byte[] modelPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Model.pdb.bytes"));
            //byte[] modelViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.ModelView.dll.bytes"));
            //byte[] modelViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.ModelView.pdb.bytes"));

            // 为 HybridCLR 补充 AOT 泛型元数据，防止热更代码中调用 AOT 泛型方法时报错
            foreach (var kv in this.aotDlls)
            {
                TextAsset textAsset = kv.Value;
                HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HybridCLR.HomologousImageMode.SuperSet);
            }
            
            Assembly modelAssembly = Assembly.Load(modelAssBytes, modelPdbBytes);
            Assembly modelViewAssembly = Assembly.Load(modelViewAssBytes, modelViewPdbBytes);
            list.Add(modelAssembly);
            list.Add(modelViewAssembly);
#endif
            
            (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = this.LoadHotfix();
            
            list.Add(hotfixViewAssembly);
            list.Add(hotfixAssembly);
            World.Instance.AddSingleton<CodeTypes, Assembly[]>(list.ToArray());
            // 通过反射调用 ET.Hotfix 程序集中的全局启动入口 ET.Entry.Start()
            IStaticMethod start = new StaticMethod(hotfixAssembly, "ET.Entry", "Start");
            start.Run();
        }

        /// <summary>
        /// 加载 <c>ET.Hotfix</c> 与 <c>ET.HotfixView</c> 两个纯逻辑热更程序集。
        /// </summary>
        /// <param name="isReload">是否为运行期热重载（编辑器下首次启动直接从当前域获取，Reload 时从 <see cref="Define.CodeDir"/> 读取最新编译的 DLL 字节流）。</param>
        private (Assembly, Assembly) LoadHotfix(bool isReload = false)
        {
#if UNITY_EDITOR
            Assembly hotfixAssembly = null;
            Assembly hotfixViewAssembly = null;
            
            if (isReload)
            {
                byte[] hotfixAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.dll.bytes"));
                byte[] hotfixPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.pdb.bytes"));
                byte[] hotfixViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.dll.bytes"));
                byte[] hotfixViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.pdb.bytes"));
                hotfixAssembly = Assembly.Load(hotfixAssBytes, hotfixPdbBytes);
                hotfixViewAssembly = Assembly.Load(hotfixViewAssBytes, hotfixViewPdbBytes);
            }
            else
            {
                Assembly[] domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in domainAssemblies)
                {
                    string name = assembly.GetName().Name;
                    if (name == "ET.Hotfix")
                    {
                        hotfixAssembly = assembly;
                        continue;
                    }
                    if (name == "ET.HotfixView")
                    {
                        hotfixViewAssembly = assembly;
                        continue;
                    }
                    if (hotfixAssembly != null && hotfixViewAssembly != null)
                    {
                        break;
                    }
                }
            }

#else
            byte[] hotfixAssBytes = this.dlls["ET.Hotfix.dll"].bytes;
            byte[] hotfixPdbBytes = this.dlls["ET.Hotfix.pdb"].bytes;
            byte[] hotfixViewAssBytes = this.dlls["ET.HotfixView.dll"].bytes;
            byte[] hotfixViewPdbBytes = this.dlls["ET.HotfixView.pdb"].bytes;

            // 如果需要测试，可替换成下面注释的代码直接加载Packages/cn.etetet.loader/Code/Hotfix.dll.bytes，但真正打包时必须使用上面的代码
            //hotfixAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.dll.bytes"));
            //hotfixPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.Hotfix.pdb.bytes"));
            //hotfixViewAssBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.dll.bytes"));
            //hotfixViewPdbBytes = File.ReadAllBytes(Path.Combine(Define.CodeDir, "ET.HotfixView.pdb.bytes"));
            Assembly hotfixAssembly = Assembly.Load(hotfixAssBytes, hotfixPdbBytes);
            Assembly hotfixViewAssembly = Assembly.Load(hotfixViewAssBytes, hotfixViewPdbBytes);
#endif
            return (hotfixAssembly, hotfixViewAssembly);
        }

        /// <summary>
        /// 运行期热重载（Hot Reload）：重新加载 <c>ET.Hotfix</c> 和 <c>ET.HotfixView</c> 程序集，替换 <see cref="CodeTypes"/> 单例并重新绑定所有 System/Event/Handler。
        ///  завдяки ET“数据（Model）与逻辑（Hotfix）彻底分离”的 ECS 架构，热重载可以在保留内存中所有实体数据的情况下无缝更新业务逻辑。
        /// </summary>
        public void Reload()
        {
            (Assembly hotfixAssembly, Assembly hotfixViewAssembly) = this.LoadHotfix(true);
            List<Assembly> list = new(this.assemblies);
            list.Add(hotfixViewAssembly);
            list.Add(hotfixAssembly);
            CodeTypes codeTypes = World.Instance.AddSingleton<CodeTypes, Assembly[]>(list.ToArray());
            codeTypes.CodeProcess();

            Log.Info($"reload dll finish!");
        }
    }
}
