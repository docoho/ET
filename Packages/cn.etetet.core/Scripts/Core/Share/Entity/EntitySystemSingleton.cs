using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 全局 ECS 实体系统管理与调度单例。
    /// </summary>
    /// <remarks>
    /// 【初学者指南 —— ET 是怎么把 Entity（数据）和 System（逻辑）关联起来的？】
    /// 1. <b>启动扫描（<see cref="Awake()"/>）</b>：
    ///    在游戏或服务器启动加载代码时，<see cref="EntitySystemSingleton"/> 会从 <see cref="CodeTypes"/> 中找出所有标注了 <see cref="EntitySystemAttribute"/> 的系统类（由 SourceGenerator 自动生成），
    ///    实例化它们并按“目标 Entity 类型 -> 系统接口类型（如 <see cref="IAwakeSystem"/>、<see cref="IUpdateSystem"/>、<see cref="IDestroySystem"/>）”存入 <see cref="TypeSystems"/> 索引表中。
    /// 2. <b>即时生命周期分发</b>：
    ///    - 当调用 <c>entity.AddComponent&lt;T&gt;()</c> 或 <c>entity.AddChild&lt;T&gt;()</c> 时，会立即调用本类的 <see cref="Awake(Entity)"/> 重载；
    ///    - 当调用 <c>entity.Dispose()</c> 时，会立即调用本类的 <see cref="Destroy(Entity)"/>；
    ///    - 当实体序列化/反序列化时，会分别调用 <see cref="Serialize(Entity)"/> 与 <see cref="Deserialize(Entity)"/>。
    /// 3. <b>帧循环生命周期（Update / LateUpdate）</b>：
    ///    由于不同实体归属于不同的 <see cref="Fiber"/>（纤程），<c>Update</c> 和 <c>LateUpdate</c> 的轮询队列保存在每个 <see cref="Fiber.EntitySystem"/> 中，但它们底层查询的依然是本单例维护的 <see cref="TypeSystems"/> 映射表。
    /// </remarks>
    [CodeProcess]
    [AllowInstance]
    public class EntitySystemSingleton: Singleton<EntitySystemSingleton>, ISingletonAwake
    {
        /// <summary>
        /// 存储所有实体类型到其各类 System 处理器列表的映射容器。
        /// </summary>
        public TypeSystems TypeSystems { get; private set; }
        
        /// <summary>
        /// 单例初始化：扫描所有带 <see cref="EntitySystemAttribute"/> 的生成类并注册到 <see cref="TypeSystems"/>。
        /// </summary>
        public void Awake()
        {
            this.TypeSystems = new TypeSystems();

            foreach (Type type in CodeTypes.Instance.GetTypes(typeof (EntitySystemAttribute)))
            {
                SystemObject obj = (SystemObject)Activator.CreateInstance(type);

                if (obj is ISystemType iSystemType)
                {
                    TypeSystems.OneTypeSystems oneTypeSystems = this.TypeSystems.GetOrCreateOneTypeSystems(iSystemType.Type());
                    oneTypeSystems.Map.Add(iSystemType.SystemType(), obj);
                    
                    if (iSystemType is IEventSystem)
                    {
                        oneTypeSystems.ClassType.Add(iSystemType.SystemType());
                    }
                }
            }
        }
        
        /// <summary>
        /// 触发实体的 <see cref="ISerializeSystem"/> 回调（仅当实体实现了 <see cref="ISerialize"/> 标记接口时执行）。
        /// </summary>
        public void Serialize(Entity component)
        {
            if (component is not ISerialize)
            {
                return;
            }
            
            List<SystemObject> iSerializeSystems = this.TypeSystems.GetSystems(component.GetType(), typeof (ISerializeSystem));
            if (iSerializeSystems == null)
            {
                return;
            }

            foreach (ISerializeSystem serializeSystem in iSerializeSystems)
            {
                if (serializeSystem == null)
                {
                    continue;
                }

                try
                {
                    serializeSystem.Run(component);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        /// <summary>
        /// 触发实体的 <see cref="IDeserializeSystem"/> 回调（当反序列化出的实体挂载到场景树且实现了 <see cref="IDeserialize"/> 接口时执行）。
        /// </summary>
        public void Deserialize(Entity component)
        {
            if (component is not IDeserialize)
            {
                return;
            }
            
            List<SystemObject> iDeserializeSystems = this.TypeSystems.GetSystems(component.GetType(), typeof (IDeserializeSystem));
            if (iDeserializeSystems == null)
            {
                return;
            }

            foreach (IDeserializeSystem deserializeSystem in iDeserializeSystems)
            {
                if (deserializeSystem == null)
                {
                    continue;
                }

                try
                {
                    deserializeSystem.Run(component);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        /// <summary>
        /// 触发实体的 <see cref="IGetComponentSysSystem"/> 回调（当在实现了 <see cref="IGetComponentSys"/> 的实体上调用 <c>GetComponent</c> 时触发）。
        /// </summary>
        // GetComponentSystem
        public void GetComponentSys(Entity entity, Type type)
        {
            List<SystemObject> iGetSystem = this.TypeSystems.GetSystems(entity.GetType(), typeof (IGetComponentSysSystem));
            if (iGetSystem == null)
            {
                return;
            }

            foreach (IGetComponentSysSystem getSystem in iGetSystem)
            {
                if (getSystem == null)
                {
                    continue;
                }

                try
                {
                    getSystem.Run(entity, type);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        
        /// <summary>
        /// 触发实体的无参 <see cref="IAwakeSystem"/> 初始化回调。
        /// </summary>
        public void Awake(Entity component)
        {
            List<SystemObject> iAwakeSystems = this.TypeSystems.GetSystems(component.GetType(), typeof (IAwakeSystem));
            if (iAwakeSystems == null)
            {
                return;
            }

            foreach (IAwakeSystem aAwakeSystem in iAwakeSystems)
            {
                if (aAwakeSystem == null)
                {
                    continue;
                }

                try
                {
                    aAwakeSystem.Run(component);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 触发实体带 1 个参数的 <see cref="IAwakeSystem{P1}"/> 初始化回调。
        /// </summary>
        public void Awake<P1>(Entity component, P1 p1)
        {
            if (component is not IAwake<P1>)
            {
                return;
            }
            
            List<SystemObject> iAwakeSystems = this.TypeSystems.GetSystems(component.GetType(), typeof (IAwakeSystem<P1>));
            if (iAwakeSystems == null)
            {
                return;
            }

            foreach (IAwakeSystem<P1> aAwakeSystem in iAwakeSystems)
            {
                if (aAwakeSystem == null)
                {
                    continue;
                }

                try
                {
                    aAwakeSystem.Run(component, p1);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 触发实体带 2 个参数的 <see cref="IAwakeSystem{P1, P2}"/> 初始化回调。
        /// </summary>
        public void Awake<P1, P2>(Entity component, P1 p1, P2 p2)
        {
            if (component is not IAwake<P1, P2>)
            {
                return;
            }
            
            List<SystemObject> iAwakeSystems = this.TypeSystems.GetSystems(component.GetType(), typeof (IAwakeSystem<P1, P2>));
            if (iAwakeSystems == null)
            {
                return;
            }

            foreach (IAwakeSystem<P1, P2> aAwakeSystem in iAwakeSystems)
            {
                if (aAwakeSystem == null)
                {
                    continue;
                }

                try
                {
                    aAwakeSystem.Run(component, p1, p2);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 触发实体带 3 个参数的 <see cref="IAwakeSystem{P1, P2, P3}"/> 初始化回调。
        /// </summary>
        public void Awake<P1, P2, P3>(Entity component, P1 p1, P2 p2, P3 p3)
        {
            if (component is not IAwake<P1, P2, P3>)
            {
                return;
            }
            
            List<SystemObject> iAwakeSystems = this.TypeSystems.GetSystems(component.GetType(), typeof (IAwakeSystem<P1, P2, P3>));
            if (iAwakeSystems == null)
            {
                return;
            }

            foreach (IAwakeSystem<P1, P2, P3> aAwakeSystem in iAwakeSystems)
            {
                if (aAwakeSystem == null)
                {
                    continue;
                }

                try
                {
                    aAwakeSystem.Run(component, p1, p2, p3);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 触发实体的 <see cref="IDestroySystem"/> 销毁清理回调（在 <see cref="Entity.Dispose"/> 过程中自动调用）。
        /// </summary>
        public void Destroy(Entity component)
        {
            if (component is not IDestroy)
            {
                return;
            }
            
            List<SystemObject> iDestroySystems = this.TypeSystems.GetSystems(component.GetType(), typeof (IDestroySystem));
            if (iDestroySystems == null)
            {
                return;
            }

            foreach (IDestroySystem iDestroySystem in iDestroySystems)
            {
                if (iDestroySystem == null)
                {
                    continue;
                }

                try
                {
                    iDestroySystem.Run(component);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}
