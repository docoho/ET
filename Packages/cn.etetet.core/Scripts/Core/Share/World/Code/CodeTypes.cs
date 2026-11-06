using System.Collections.Generic;
using System.Reflection;
using System;

namespace ET
{
    /// <summary>
    /// 全程序集类型与自定义特性（<see cref="BaseAttribute"/>）索引中心单例。
    /// </summary>
    /// <remarks>
    /// 【初学者指南 —— ET 是如何做到自动发现所有 Handler、System、Event 的？】
    /// 1. 在程序启动（或热重载）时，加载器会将所有业务程序集（<c>ET.Core</c>、<c>ET.Model</c>、<c>ET.Hotfix</c> 等）传入 <see cref="CodeTypes.Awake"/>；
    /// 2. <see cref="CodeTypes"/> 扫描所有非抽象类，只要某个类上标注了继承自 <see cref="BaseAttribute"/> 的特性（如 <c>[EntitySystem]</c>、<c>[Event]</c>、<c>[MessageHandler]</c>、<c>[Invoke]</c>、<c>[CodeProcess]</c>），
    ///    就会将该类按特性类型归类存入 <see cref="types"/> 多值字典中；
    /// 3. 随后调用 <see cref="CodeProcess"/>，自动实例化并唤醒所有标注了 <see cref="CodeProcessAttribute"/> 的核心分发器单例，完成整个框架的自动装配！
    /// </remarks>
    [AllowInstance]
    public class CodeTypes: Singleton<CodeTypes>, ISingletonAwake<Assembly[]>
    {
        /// <summary>存储类型全名（FullName）到 Type 的映射字典</summary>
        private readonly Dictionary<string, Type> allTypes = new();
        /// <summary>存储 BaseAttribute 派生特性类型到标注了该特性的所有具体类的多值集合映射</summary>
        private readonly UnOrderMultiMapSet<Type, Type> types = new();
        
        /// <summary>
        /// 扫描传入的所有程序集，建立类型全名索引与 <see cref="BaseAttribute"/> 特性分类索引。
        /// </summary>
        public void Awake(Assembly[] assemblies)
        {
            Dictionary<string, Type> addTypes = AssemblyHelper.GetAssemblyTypes(assemblies);
            foreach ((string fullName, Type type) in addTypes)
            {
                this.allTypes[fullName] = type;
                
                if (type.IsAbstract)
                {
                    continue;
                }
                
                // 记录所有的有BaseAttribute标记的的类型
                object[] objects = type.GetCustomAttributes(typeof(BaseAttribute), true);

                foreach (object o in objects)
                {
                    this.types.Add(o.GetType(), type);
                }
            }
        }

        /// <summary>
        /// 获取所有标注了指定 <paramref name="systemAttributeType"/> 特性的非抽象类型集合。
        /// </summary>
        public HashSet<Type> GetTypes(Type systemAttributeType)
        {
            if (!this.types.ContainsKey(systemAttributeType))
            {
                return new HashSet<Type>();
            }

            return this.types[systemAttributeType];
        }

        /// <summary>
        /// 获取所有已加载的类型字典（Key 为 Type.FullName）。
        /// </summary>
        public Dictionary<string, Type> GetTypes()
        {
            return allTypes;
        }
        
        /// <summary>
        /// 根据类型完整名称查找对应的 <see cref="Type"/>。
        /// </summary>
        public Type GetType(string typeName)
        {
            Type type = null;
            this.allTypes.TryGetValue(typeName, out type);
            return type;
        }
        
        /// <summary>
        /// 自动实例化并注册所有标注了 <see cref="CodeProcessAttribute"/> 的全局单例。
        /// </summary>
        public void CodeProcess()
        {
            var hashSet = this.GetTypes(typeof (CodeProcessAttribute));
            foreach (Type type in hashSet)
            {
                object obj = Activator.CreateInstance(type);
                ((ISingletonAwake)obj).Awake();
                World.Instance.AddSingleton((ASingleton)obj);
            }
        }
    }
}
