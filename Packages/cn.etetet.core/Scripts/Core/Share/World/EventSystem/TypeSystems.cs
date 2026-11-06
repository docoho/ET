using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 存储所有 <see cref="Entity"/> 派生类型与其对应的 ECS <see cref="SystemObject"/> 处理器列表的映射索引表（由 <see cref="EntitySystemSingleton"/> 持有）。
    /// </summary>
    public class TypeSystems
    {
        /// <summary>
        /// 单个具体 <see cref="Entity"/> 类型名下挂载的所有 System 集合。
        /// </summary>
        public class OneTypeSystems
        {
            /// <summary>Key 为系统接口类型（如 <c>typeof(IAwakeSystem)</c>），Value 为对应的 System 实例列表</summary>
            public readonly UnOrderMultiMap<Type, SystemObject> Map = new();
            /// <summary>记录该实体实现了哪些需要在 <see cref="EntitySystem"/> 中每帧轮询的周期事件类型（如 <c>AEventSystem&lt;UpdateEvent&gt;</c>）</summary>
            // 这里不用hash，数量比较少，直接for循环速度更快
            public readonly List<Type> ClassType = new();
        }
        
        /// <summary>Key 为实体类型（如 <c>typeof(Unit)</c>），Value 为该实体类型对应的 <see cref="OneTypeSystems"/></summary>
        private readonly Dictionary<Type, OneTypeSystems> typeSystemsMap = new();

        /// <summary>
        /// 获取或创建指定实体 <paramref name="type"/> 的系统映射项。
        /// </summary>
        public OneTypeSystems GetOrCreateOneTypeSystems(Type type)
        {
            OneTypeSystems systems = null;
            this.typeSystemsMap.TryGetValue(type, out systems);
            if (systems != null)
            {
                return systems;
            }

            systems = new OneTypeSystems();
            this.typeSystemsMap.Add(type, systems);
            return systems;
        }

        /// <summary>
        /// 获取指定实体 <paramref name="type"/> 的系统映射项（若该实体没有任何 System 则返回 null）。
        /// </summary>
        public OneTypeSystems GetOneTypeSystems(Type type)
        {
            OneTypeSystems systems = null;
            this.typeSystemsMap.TryGetValue(type, out systems);
            return systems;
        }

        /// <summary>
        /// 获取指定实体类型 <paramref name="type"/> 在特定生命周期接口 <paramref name="systemType"/> 下的所有 System 处理器列表。
        /// </summary>
        public List<SystemObject> GetSystems(Type type, Type systemType)
        {
            OneTypeSystems oneTypeSystems = null;
            if (!this.typeSystemsMap.TryGetValue(type, out oneTypeSystems))
            {
                return null;
            }

            if (!oneTypeSystems.Map.TryGetValue(systemType, out List<SystemObject> systems))
            {
                return null;
            }

            return systems;
        }
    }
}