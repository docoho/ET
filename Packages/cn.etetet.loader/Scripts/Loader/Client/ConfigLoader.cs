using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ET
{
    /// <summary>
    /// 客户端配置表加载器单例。
    /// 负责加载 <c>ET.Config</c> 程序集中的所有配置工厂（<see cref="IConfigFactory"/>），创建所有标记了 <see cref="ConfigProcessAttribute"/> 的配置表 Category 单例并注册到 <see cref="World"/> 中。
    /// </summary>
    [AllowInstance]
    public class ConfigLoader : Singleton<ConfigLoader>, ISingletonAwake
    {
        public void Awake()
        {
        }

        /// <summary>
        /// 异步加载所有配置表 Category 单例，并在全部加载完毕后统一调用 <see cref="IConfig.ResolveRef"/> 解析跨表引用。
        /// </summary>
        public async ETTask LoadAsync()
        {
            List<Type> configTypes = ConfigLoaderHelper.GetConfigTypes();
            ConfigFactoryGroupCollection configFactories = await this.LoadConfigFactoriesAsync(configTypes);
            Dictionary<Type, IConfig> loadedConfigs = new(configTypes.Count);
            List<ASingleton> loadedSingletons = new(configTypes.Count);

            foreach (Type configType in configTypes)
            {
                ASingleton singleton = ConfigLoaderHelper.LoadOneConfig(configType, configFactories, loadedConfigs);
                loadedSingletons.Add(singleton);
            }

            ConfigLoaderHelper.ApplyLoadedConfigs(loadedSingletons, loadedConfigs);
        }

        /// <summary>
        /// 加载 <c>ET.Config</c> 程序集（若当前域未加载则通过 <see cref="ResourcesComponent"/> 异步加载 <c>ET.Config.dll</c>），并构建配置工厂集合。
        /// </summary>
        private async ETTask<ConfigFactoryGroupCollection> LoadConfigFactoriesAsync(List<Type> configTypes)
        {
            if (!ConfigLoaderHelper.HasCodeConfig(configTypes))
            {
                return new ConfigFactoryGroupCollection();
            }

            Assembly configAssembly = ConfigLoaderHelper.GetLoadedConfigAssembly();
            if (configAssembly == null)
            {
                TextAsset dll = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.Config.dll");
                TextAsset pdb = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.Config.pdb");
                configAssembly = Assembly.Load(dll.bytes, pdb.bytes);
            }

            return ConfigLoaderHelper.CreateConfigFactories(configAssembly.GetTypes());
        }
    }
}
