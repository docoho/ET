using System;
using System.Collections.Generic;
using System.IO;

namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端表现层初始化入口事件处理器（<see cref="EntryEvent3"/>）
    /// </summary>
    [Event(SceneType.LockStep)]
    public class EntryEvent3_InitClient: AEvent<Scene, EntryEvent3>
    {
        /// <summary>
        /// 注册帧同步实体系统单例 <see cref="LSEntitySystemSingleton"/>，挂载全局层级、资源加载、玩家信息与当前场景管理组件，并发布 <see cref="AppStartInitFinish"/> 事件
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="args">表现层启动事件参数</param>
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            World.Instance.AddSingleton<LSEntitySystemSingleton>();
            
            root.AddComponent<GlobalComponent>();
            root.AddComponent<ResourcesLoaderComponent>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<CurrentScenesComponent>();
            
            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
        }
    }
}