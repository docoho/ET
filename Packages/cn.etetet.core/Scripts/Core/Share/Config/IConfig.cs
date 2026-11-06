using System;

namespace ET
{
    /// <summary>
    /// 配置分组特性，用于给配置表分类（如按模块或加载阶段分组）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigGroupAttribute : Attribute
    {
        /// <summary>
        /// 配置分组名称。
        /// </summary>
        public string Name { get; }

        public ConfigGroupAttribute(string name)
        {
            this.Name = name;
        }
    }

    /// <summary>
    /// 预定义的配置分组名称常量。
    /// </summary>
    public static class ConfigGroupNames
    {
        /// <summary>
        /// 默认通用配置分组名。
        /// </summary>
        public const string Config = "Config";
    }

    /// <summary>
    /// 每个XXConfigCategory单例的接口
    /// </summary>
    public interface IConfig
    {
        //Model中的所有配置初始化完毕后的调用
        //Ref的初始化
        /// <summary>
        /// 在所有配置表加载并反序列化完成后调用，用于解析表间的外键引用（Ref）。
        /// </summary>
        void ResolveRef();
    }

    /// <summary>
    /// 代码配置工厂接口，由Hotfix返回已经填充完数据的配置单例实例。
    /// </summary>
    public interface IConfigFactory
    {
        /// <summary>
        /// 目标配置单例的类型。
        /// </summary>
        Type ConfigType { get; }

        /// <summary>
        /// 创建并填充好数据的配置单例对象。
        /// </summary>
        ASingleton Create();
    }
}
