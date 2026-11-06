using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET
{
    /// <summary>
    /// 配置工厂分组集合。
    /// 按“配置表类型（<see cref="Type"/>） -> 配置分组名称（<c>configGroup</c>） -> 配置工厂（<see cref="IConfigFactory"/>）”二级字典组织，
    /// 支持如默认业务配置组（<see cref="ConfigGroupNames.Config"/>）与不同环境启动配置组（如 <c>Localhost</c>、<c>Benchmark</c> 等）的按需加载。
    /// </summary>
    public sealed class ConfigFactoryGroupCollection
    {
        private readonly Dictionary<Type, Dictionary<string, IConfigFactory>> factories = new();

        /// <summary>
        /// 注册一个配置表工厂到指定的配置组中。
        /// </summary>
        /// <param name="configGroup">配置分组名称。</param>
        /// <param name="factory">配置工厂实例。</param>
        public void Add(string configGroup, IConfigFactory factory)
        {
            if (factory.ConfigType == null)
            {
                throw new Exception($"config factory target is null: {factory.GetType().FullName}");
            }

            if (string.IsNullOrWhiteSpace(configGroup))
            {
                throw new Exception($"config group is empty: {factory.GetType().FullName}");
            }

            if (!this.factories.TryGetValue(factory.ConfigType, out Dictionary<string, IConfigFactory> groupFactories))
            {
                groupFactories = new Dictionary<string, IConfigFactory>(StringComparer.Ordinal);
                this.factories.Add(factory.ConfigType, groupFactories);
            }

            if (!groupFactories.TryAdd(configGroup, factory))
            {
                throw new Exception($"duplicate config factory: {factory.ConfigType.FullName} group={configGroup}");
            }
        }

        /// <summary>
        /// 尝试获取指定配置类型下的所有分组工厂字典。
        /// </summary>
        public bool TryGetFactories(Type configType, out Dictionary<string, IConfigFactory> groupFactories)
        {
            return this.factories.TryGetValue(configType, out groupFactories);
        }
    }

    /// <summary>
    /// 配置加载核心辅助类（客户端与服务端共享）。
    /// 负责扫描 <see cref="ConfigProcessAttribute"/>、实例化 <c>ET.Config</c> 中的代码化配置工厂（<see cref="IConfigFactory"/>）、
    /// 按当前激活的配置分组（<c>Config</c> + <see cref="Options.StartConfig"/>）创建配置单例，并统一执行 <see cref="IConfig.ResolveRef"/>。
    /// </summary>
    internal static class ConfigLoaderHelper
    {
        /// <summary>
        /// 获取所有标记了 <see cref="ConfigProcessAttribute"/> 的配置表 Category 类型，并按类型全名排序以保证确定性加载顺序。
        /// </summary>
        public static List<Type> GetConfigTypes()
        {
            List<Type> configTypes = new(CodeTypes.Instance.GetTypes(typeof(ConfigProcessAttribute)));
            configTypes.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return configTypes;
        }

        /// <summary>
        /// 将已创建的配置表 Category 单例替换注册到全局 <see cref="World"/> 中，并依次调用 <see cref="IConfig.ResolveRef"/> 建立表间关联引用。
        /// </summary>
        public static void ApplyLoadedConfigs(List<ASingleton> loadedSingletons, Dictionary<Type, IConfig> loadedConfigs)
        {
            foreach (ASingleton singleton in loadedSingletons)
            {
                World.Instance.ReplaceSingleton(singleton);
            }

            foreach (IConfig config in loadedConfigs.Values)
            {
                config.ResolveRef();
            }
        }

        /// <summary>
        /// 检查待加载的配置类型列表中是否包含基于代码生成（<see cref="ConfigType.Code"/>）的配置表。
        /// </summary>
        public static bool HasCodeConfig(List<Type> configTypes)
        {
            foreach (Type configType in configTypes)
            {
                ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
                if (configProcessAttribute.ConfigType == ConfigType.Code)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从当前 <see cref="AppDomain"/> 中查找已加载的 <c>ET.Config</c> 程序集。
        /// </summary>
        public static Assembly GetLoadedConfigAssembly()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "ET.Config")
                {
                    return assembly;
                }
            }

            return null;
        }

        /// <summary>
        /// 扫描给定类型集合中所有实现了 <see cref="IConfigFactory"/> 的具体类，实例化并按 <see cref="ConfigGroupAttribute"/> 注册到 <see cref="ConfigFactoryGroupCollection"/>。
        /// </summary>
        public static ConfigFactoryGroupCollection CreateConfigFactories(IEnumerable<Type> allTypes)
        {
            List<Type> factoryTypes = new();
            foreach (Type type in allTypes)
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (!typeof(IConfigFactory).IsAssignableFrom(type))
                {
                    continue;
                }

                factoryTypes.Add(type);
            }

            factoryTypes.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            ConfigFactoryGroupCollection factories = new();
            foreach (Type factoryType in factoryTypes)
            {
                if (Activator.CreateInstance(factoryType) is not IConfigFactory factory)
                {
                    throw new Exception($"create config factory failed: {factoryType.FullName}");
                }

                factories.Add(GetConfigGroup(factoryType), factory);
            }

            return factories;
        }

        /// <summary>
        /// 加载单个配置表 Category：通过匹配的配置工厂实例化配置单例并校验类型实现。
        /// </summary>
        public static ASingleton LoadOneConfig(Type configType, ConfigFactoryGroupCollection configFactories, Dictionary<Type, IConfig> loadedConfigs)
        {
            ConfigProcessAttribute configProcessAttribute = configType.GetCustomAttributes(typeof(ConfigProcessAttribute), false)[0] as ConfigProcessAttribute;
            if (configProcessAttribute.ConfigType != ConfigType.Code)
            {
                throw new Exception($"unsupported config type: {configProcessAttribute.ConfigType} {configType.FullName}");
            }

            IConfigFactory factory = GetConfigFactory(configType, configFactories);
            object category = factory.Create();
            if (category == null)
            {
                throw new Exception($"config create failed: {configType.FullName}");
            }

            if (category.GetType() != configType)
            {
                throw new Exception($"config type mismatch: expect={configType.FullName} actual={category.GetType().FullName}");
            }

            if (category is not ASingleton singleton)
            {
                throw new Exception($"config singleton invalid: {configType.FullName}");
            }

            if (category is not IConfig iConfig)
            {
                throw new Exception($"config interface invalid: {configType.FullName}");
            }

            loadedConfigs.Add(configType, iConfig);
            return singleton;
        }

        /// <summary>
        /// 获取配置工厂类上的 <see cref="ConfigGroupAttribute"/> 组名（若未标注则默认为 <see cref="ConfigGroupNames.Config"/>）。
        /// </summary>
        private static string GetConfigGroup(MemberInfo factoryType)
        {
            ConfigGroupAttribute configGroupAttribute = factoryType.GetCustomAttribute<ConfigGroupAttribute>();
            return string.IsNullOrWhiteSpace(configGroupAttribute?.Name) ? ConfigGroupNames.Config : configGroupAttribute.Name;
        }

        /// <summary>
        /// 根据当前激活的配置分组（<see cref="GetActiveConfigGroups"/>）为指定配置类型查找唯一匹配的 <see cref="IConfigFactory"/>。
        /// </summary>
        private static IConfigFactory GetConfigFactory(Type configType, ConfigFactoryGroupCollection configFactories)
        {
            if (!configFactories.TryGetFactories(configType, out Dictionary<string, IConfigFactory> groupFactories))
            {
                throw new Exception($"config factory not found: {configType.FullName}");
            }

            List<string> activeGroups = GetActiveConfigGroups();
            IConfigFactory match = null;
            string matchedGroup = null;
            foreach (string activeGroup in activeGroups)
            {
                if (!groupFactories.TryGetValue(activeGroup, out IConfigFactory factory))
                {
                    continue;
                }

                if (match != null)
                {
                    throw new Exception(
                        $"config factory group ambiguous: {configType.FullName}, matched groups: {matchedGroup}, {activeGroup}");
                }

                match = factory;
                matchedGroup = activeGroup;
            }

            if (match != null)
            {
                return match;
            }

            throw new Exception(
                $"config factory group not found: {configType.FullName}, active groups: {string.Join(", ", activeGroups)}, available groups: {string.Join(", ", groupFactories.Keys)}");
        }

        /// <summary>
        /// 获取当前进程激活的配置分组列表：始终包含默认组 <see cref="ConfigGroupNames.Config"/>，若 <see cref="Options.StartConfig"/> 指定了额外启动配置组（如 <c>Localhost</c>）则一并加入。
        /// </summary>
        private static List<string> GetActiveConfigGroups()
        {
            List<string> groups = new() { ConfigGroupNames.Config };
            string startConfig = Options.Instance?.StartConfig;
            if (!string.IsNullOrWhiteSpace(startConfig) && !string.Equals(startConfig, ConfigGroupNames.Config, StringComparison.Ordinal))
            {
                groups.Add(startConfig);
            }

            return groups;
        }
    }
}
