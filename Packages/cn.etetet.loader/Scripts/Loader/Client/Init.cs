using System;
using CommandLine;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// Unity 客户端启动入口组件（挂载在启动场景的 Global GameObject 上）。
    /// 负责初始化全局未捕获异常处理器、命令行参数（<see cref="Options"/>）、日志系统（<see cref="Logger"/>）、时钟（<see cref="TimeInfo"/>）、纤程管理器（<see cref="FiberManager"/>）、
    /// YooAsset 资源组件（<see cref="ResourcesComponent"/>）以及热更代码加载器（<see cref="CodeLoader"/>），并在 Unity 的每帧生命周期中驱动 <see cref="FiberManager"/>。
    /// </summary>
    public class Init: MonoBehaviour
    {
        /// <summary>
        /// Unity 启动回调：启动异步初始化流程。
        /// </summary>
        private void Start()
        {
            this.StartAsync().Coroutine();
        }
		
        /// <summary>
        /// 客户端核心异步启动引导流程。
        /// </summary>
        private async ETTask StartAsync()
        {
            // 保持启动节点跨场景切换不销毁
            DontDestroyOnLoad(gameObject);
			
            // 订阅当前应用程序域未捕获异常，统一输出到错误日志
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };

            GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            // 命令行参数（将 GlobalConfig.SceneName 转换为 Options.SceneName 单例）
            string[] args = { $"--SceneName={globalConfig.SceneName}" };
            Parser.Default.ParseArguments<Options>(args)
                    .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                    .WithParsed((o)=>World.Instance.AddSingleton(o));


            
            // 编辑器模式下如果开启了ENABLE_VIEW使用单线程，WEBGL模式也使用单线程
#if (ENABLE_VIEW && UNITY_EDITOR) || UNITY_WEBGL
            Options.Instance.SingleThread = 1;
#endif
            
            // 注册全局核心单例：日志、异常回调、时间、纤程调度器
            World.Instance.AddSingleton<Logger>().Log = new UnityLogger("None");
            ETTask.ExceptionHandler += Log.Error;
			
            World.Instance.AddSingleton<TimeInfo>();
            World.Instance.AddSingleton<FiberManager>();

            // 初始化 YooAsset 默认资源包
            await World.Instance.AddSingleton<ResourcesComponent>().CreatePackageAsync("DefaultPackage", true);
            
            // 启动代码加载器，加载程序集并进入 ET.Entry.Start()
            World.Instance.AddSingleton<CodeLoader>().Start().Coroutine();
        }

        /// <summary>
        /// Unity 每帧更新：驱动主线程纤程调度器执行所有主线程纤程的 Update 与帧末任务。
        /// </summary>
        private void Update()
        {
            FiberManager.Instance.Update();
        }

        /// <summary>
        /// Unity 每帧延迟更新：驱动主线程纤程调度器的 LateUpdate（常用于相机跟随、UI 刷新等）。
        /// </summary>
        private void LateUpdate()
        {
            FiberManager.Instance.LateUpdate();
        }

        /// <summary>
        /// 应用退出时销毁全局 <see cref="World"/> 及所有注册的单例与纤程。
        /// </summary>
        private void OnApplicationQuit()
        {
            World.Instance.Dispose();
        }
    }
}