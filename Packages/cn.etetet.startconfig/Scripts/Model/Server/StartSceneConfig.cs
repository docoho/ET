using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ET.Server
{
    /// <summary>
    /// 服务端场景（Fiber）启动配置表管理类。
    /// </summary>
    [EnableClass]
    public partial class StartSceneConfigCategory
    {
    }

    /// <summary>
    /// 服务端区服（Zone）配置表管理类（包含数据库连接串与库名等区服级信息）。
    /// </summary>
    [EnableClass]
    public partial class StartZoneConfigCategory
    {
    }

    /// <summary>
    /// <see cref="StartSceneConfigCategory"/> 的多维索引查询分部实现。
    /// <para>在配表加载完毕（<see cref="EndRef"/>）时建立按进程 ID（Process）和场景名称（Name）的快速索引。</para>
    /// </summary>
    public partial class StartSceneConfigCategory
    {
        /// <summary>按所属进程 ID 分组的场景配置映射表。</summary>
        private readonly MultiMap<int, StartSceneConfig> processScenes = new();
        
        /// <summary>按场景唯一名称索引的场景配置字典。</summary>
        private readonly Dictionary<string, StartSceneConfig> sceneNameScenes = new();
        
        /// <summary>
        /// 获取指定进程（<paramref name="process"/>）下配置的所有启动场景列表。
        /// </summary>
        public List<StartSceneConfig> GetByProcess(int process)
        {
            return this.processScenes[process];
        }
        
        /// <summary>
        /// 根据场景名称获取对应的 <see cref="StartSceneConfig"/>。
        /// </summary>
        public StartSceneConfig GetBySceneName(string sceneName)
        {
            return this.sceneNameScenes[sceneName];
        }

        /// <summary>
        /// 获取指定区服（<paramref name="zone"/>）下指定场景类型（<paramref name="sceneType"/>）的所有场景配置列表。
        /// </summary>
        public List<StartSceneConfig> GetBySceneType(int zone, int sceneType)
        {
            string sceneTypeName = SceneTypeSingleton.Instance.GetSceneName(sceneType);
            return this.GetAll().Values.Where(config => config.Zone == zone && config.SceneType == sceneTypeName).ToList();
        }

        /// <summary>
        /// 获取指定区服下指定场景类型的唯一场景配置；若数量不为 1 则抛出异常。
        /// </summary>
        public StartSceneConfig GetOneBySceneType(int zone, int sceneType)
        {
            List<StartSceneConfig> configs = this.GetBySceneType(zone, sceneType);
            if (configs.Count == 1)
            {
                return configs[0];
            }

            throw new Exception($"scene config count is not one, zone: {zone}, sceneType: {sceneType}, count: {configs.Count}");
        }
        
        /// <summary>
        /// Luban 配置表引用解析完成回调：构建 <see cref="processScenes"/> 与 <see cref="sceneNameScenes"/> 索引。
        /// </summary>
        partial void EndRef()
        {
            foreach (StartSceneConfig startSceneConfig in this.GetAll().Values)
            {
                this.processScenes.Add(startSceneConfig.Process, startSceneConfig);
                this.sceneNameScenes.Add(startSceneConfig.Name, startSceneConfig);
            }
        }
    }
    
    /// <summary>
    /// 单个服务端场景启动配置数据项的分部扩展。
    /// </summary>
    public partial class StartSceneConfig
    {
        partial void EndInit()
        {
        }
    }

    /// <summary>
    /// <see cref="StartSceneConfig"/> 的关联配置、网络终结点及 <see cref="ActorId"/> 获取扩展方法。
    /// </summary>
    public static class StartSceneConfigExtensions
    {
        /// <summary>
        /// 获取该场景所属进程的 <see cref="StartProcessConfig"/> 配置。
        /// </summary>
        public static StartProcessConfig GetStartProcessConfig(this StartSceneConfig self, Fiber fiber)
        {
            return fiber.GetSingleton<StartProcessConfigCategory>().Get(self.Process);
        }

        /// <summary>
        /// 获取该场景所属区服的 <see cref="StartZoneConfig"/> 配置。
        /// </summary>
        public static StartZoneConfig GetStartZoneConfig(this StartSceneConfig self, Fiber fiber)
        {
            return fiber.GetSingleton<StartZoneConfigCategory>().Get(self.Zone);
        }

        /// <summary>
        /// 获取该场景所在进程的内网通信 <see cref="Address"/>。
        /// </summary>
        public static Address GetAddress(this StartSceneConfig self, Fiber fiber)
        {
            return self.GetStartProcessConfig(fiber).GetAddress(fiber);
        }

        /// <summary>
        /// 获取该场景所在机器的内网 IP 搭配该场景对外端口（<c>InnerIP:ScenePort</c>）构成的终结点。
        /// </summary>
        public static IPEndPoint GetInnerIPOuterPort(this StartSceneConfig self, Fiber fiber)
        {
            return NetworkHelper.ToIPEndPoint($"{self.GetStartProcessConfig(fiber).GetInnerIP(fiber)}:{self.Port}");
        }

        /// <summary>
        /// 获取该场景所在机器的外网 IP 搭配该场景对外端口（<c>OuterIP:ScenePort</c>）构成的终结点（常用于客户端直连 Gate/Realm/Router）。
        /// </summary>
        public static IPEndPoint GetOuterIPOuterPort(this StartSceneConfig self, Fiber fiber)
        {
            return NetworkHelper.ToIPEndPoint($"{self.GetStartProcessConfig(fiber).GetOuterIP(fiber)}:{self.Port}");
        }

        /// <summary>
        /// 获取该启动场景对应的根实体 <see cref="ActorId"/>（其 FiberId 为配置表 <c>Id</c>，InstanceId 固定为 <c>1</c>）。
        /// </summary>
        public static ActorId GetActorId(this StartSceneConfig self, Fiber fiber)
        {
            return new ActorId(self.GetAddress(fiber), new FiberInstanceId(self.Id, 1));
        }
    }
}
