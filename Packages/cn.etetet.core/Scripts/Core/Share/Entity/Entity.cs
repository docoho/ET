using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// 实体运行期状态位掩码（使用 <c>byte</c> 压缩存储多个布尔状态，节省每个 Entity 实例的内存占用）。
    /// </summary>
    [Flags]
    public enum EntityStatus: byte
    {
        /// <summary>无任何特殊状态（初始值）</summary>
        None = 0,
        /// <summary>标识该实体实例是否从 <see cref="ObjectPool"/> 对象池中取出（决定 Dispose 时是否归还对象池）</summary>
        IsFromPool = 1,
        /// <summary>标识该实体是否已注册到所属 <see cref="Fiber"/> 的 <see cref="EntitySystem"/> 更新队列中（并且决定了编辑器下是否创建层级视图 ViewGO）</summary>
        IsRegister = 1 << 1,
        /// <summary>标识该实体是作为父节点的“组件（Component）”挂载（true），还是作为“子实体（Child）”挂载（false）</summary>
        IsComponent = 1 << 2,
        /// <summary>标识该实体在挂载到场景树时不需要执行 <see cref="IDeserializeSystem"/>（通常新创建的实体设为 true，反序列化出来的实体为 false）</summary>
        NoDeserializeSystem = 1 << 3,  // 不需要执行反序列化System
        /// <summary>标识该实体在父节点序列化（落库或跨进程传输）时，是否跟随父节点一起被序列化（实现 <see cref="ISerializeToEntity"/> 接口会自动开启）</summary>
        IsSerializeWithParent = 1 << 4,
    }

    /// <summary>
    /// ET10 框架万物之源：所有游戏实体（Scene、Unit、Item 等）与功能组件（Component）的抽象基类。
    /// </summary>
    /// <remarks>
    /// 【初学者指南 - ET10 ECS 核心概念】
    /// 1. <b>万物皆 Entity</b>：
    ///    在 ET 中，无论是场景（Scene）、玩家（Unit），还是挂载在它们身上的移动组件（MoveComponent）、数值组件（NumericComponent），都继承自 <see cref="Entity"/>。
    ///    - 通过 <c>AddChild</c> 挂载的节点存储在 <see cref="Children"/> 字典中（按 <see cref="Id"/> 索引，同类型可有多个，如场景下的多个怪物）；
    ///    - 通过 <c>AddComponent</c> 挂载的节点存储在 <see cref="Components"/> 字典中（按类型哈希索引，同一类型在同一个父实体上只能挂载一个）。
    /// 2. <b>Id 与 InstanceId 的区别（极重要！）</b>：
    ///    - <see cref="Id"/>：实体的持久化业务唯一标识（例如玩家的数据库角色 ID、道具 ID）。即使玩家下线再上线，或者跨服迁移，<c>Id</c> 保持不变。组件（Component）的 <c>Id</c> 默认等于其父实体的 <c>Id</c>。
    ///    - <see cref="InstanceId"/>：实体在当前进程、当前生命周期内的动态实例唯一标识（还编码了所在 <see cref="Fiber"/> 的信息）。
    ///      一旦实体 <see cref="Dispose"/> 销毁（或跨服销毁重建），<c>InstanceId</c> 就会清零并在下次创建时变成全新的值。
    ///      这也是 <see cref="EntityRef{T}"/> 能够安全检测“实体是否在 <c>await</c> 期间被销毁或被对象池复用”的核心依据！
    /// 3. <b>树形生命周期与单线程安全</b>：
    ///    每个挂载到树上的 Entity 都必然关联一个 <see cref="IScene"/>（进而关联一个 <see cref="Fiber"/> 纤程）。
    ///    父实体 <see cref="Dispose"/> 时，会自动递归销毁其下所有的 <see cref="Children"/> 和 <see cref="Components"/>，彻底杜绝内存泄漏。
    /// </remarks>
    [MemoryPackable(GenerateType.NoGenerate)]
    public abstract partial class Entity: DisposeObject
    {
        /// <summary>
        /// 供 SourceGenerator 生成代码调用的泛型对象池获取入口。
        /// </summary>
        // 给source generater调用的
        public static T Fetch<T>() where T : Entity
        {
            return ObjectPool.Fetch<T>();
        }
        
        /// <summary>
        /// 获取当前实体类型的 64 位唯一类型句柄值，作为 <see cref="Components"/> 字典的 Key，避免直接使用 <see cref="Type"/> 带来的字典查找开销。
        /// </summary>
        public virtual long GetLongHashCode()
        {
            return this.GetType().TypeHandle.Value.ToInt64();
        }
        
        /// <summary>
        /// 获取指定组件类型的 64 位唯一类型句柄值。
        /// </summary>
        public virtual long GetComponentLongHashCode(Type type)
        {
            return type.TypeHandle.Value.ToInt64();
        }
        
#if ENABLE_VIEW && UNITY_EDITOR
        /// <summary>
        /// 仅在 Unity 编辑器且开启 <c>ENABLE_VIEW</c> 宏时存在：用于在 Hierarchy 窗口实时映射显示 ECS 实体树结构的调试 GameObject。
        /// </summary>
        [BsonIgnore]
        [UnityEngine.HideInInspector]
        [MemoryPackIgnore]
        public UnityEngine.GameObject ViewGO;
#endif

        /// <summary>
        /// 实体在当前内存生命周期内的唯一实例 ID（包含所在 Fiber 路由信息）。
        /// 当 <c>InstanceId == 0</c> 时表示实体尚未挂载到场景树或已经被销毁（<see cref="IsDisposed"/> 为 true）。
        /// </summary>
        [MemoryPackIgnore]
        [BsonIgnore]
        public long InstanceId { get; protected set; }

        /// <summary>
        /// 内部位标志字段，聚合存储 <see cref="IsFromPool"/>、<see cref="IsRegister"/>、<see cref="IsComponent"/> 等状态。
        /// </summary>
        [BsonIgnore]
        private EntityStatus status = EntityStatus.None;

        /// <summary>
        /// 标识该实体是否从对象池中分配。若为 true，则在 <see cref="Dispose"/> 时会自动回收到对象池供后续复用。
        /// </summary>
        [MemoryPackIgnore]
        [BsonIgnore]
        public bool IsFromPool
        {
            get => (this.status & EntityStatus.IsFromPool) == EntityStatus.IsFromPool;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsFromPool;
                }
                else
                {
                    this.status &= ~EntityStatus.IsFromPool;
                }
            }
        }

        /// <summary>
        /// 标识该实体是否已注册到所属 Fiber 的 <see cref="EntitySystem"/> 中（注册后才能接收 Update/LateUpdate 轮询，并在编辑器 Hierarchy 下生成调试节点）。
        /// </summary>
        [BsonIgnore]
        protected bool IsRegister
        {
            get => (this.status & EntityStatus.IsRegister) == EntityStatus.IsRegister;
            set
            {
                if (this.IsRegister == value)
                {
                    return;
                }

                if (value)
                {
                    this.status |= EntityStatus.IsRegister;
                }
                else
                {
                    this.status &= ~EntityStatus.IsRegister;
                }

                if (value)
                {
                    this.RegisterSystem();
                }

#if ENABLE_VIEW && UNITY_EDITOR
                if (value)
                {
                    this.ViewGO = new UnityEngine.GameObject(this.ViewName);
                    this.ViewGO.AddComponent<ComponentView>().Component = this;
                    this.ViewGO.transform.SetParent(this.parent == null? 
                            UnityEngine.GameObject.Find("Global/Scenes").transform : this.parent.ViewGO.transform);
                }
                else
                {
                    UnityEngine.Object.Destroy(this.ViewGO);
                }
#endif
            }
        }

        /// <summary>
        /// 将当前实体注册到所在 <see cref="Fiber"/> 的 <see cref="EntitySystem"/> 队列中（供 Update / LateUpdate 等帧循环驱动）。
        /// </summary>
        protected virtual void RegisterSystem()
        {
            this.iScene.Fiber.EntitySystem.RegisterSystem(this);
        }

        /// <summary>
        /// 编辑器 Hierarchy 视图中显示的节点名称（默认为类的完整名称，派生类如 Scene 可重写以显示更有意义的名字）。
        /// </summary>
        protected virtual string ViewName
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        /// <summary>
        /// 标识当前实体是否是作为父实体的“组件（Component）”挂载。
        /// true 表示存储在父节点的 <see cref="Components"/> 中；false 表示作为子实体存储在父节点的 <see cref="Children"/> 中。
        /// </summary>
        [BsonIgnore]
        protected bool IsComponent
        {
            get => (this.status & EntityStatus.IsComponent) == EntityStatus.IsComponent;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsComponent;
                }
                else
                {
                    this.status &= ~EntityStatus.IsComponent;
                }
            }
        }

        /// <summary>
        /// 标识当前实体在设置 <see cref="IScene"/> 时是否跳过 <see cref="IDeserializeSystem"/> 回调。
        /// 正常通过 <c>AddComponent</c> / <c>AddChild</c> 新创建的实体会设为 true（只走 Awake）；而从数据库/网络反序列化恢复出来的实体为 false（会触发 Deserialize 回调）。
        /// </summary>
        [BsonIgnore]
        protected bool IsNoDeserializeSystem
        {
            get => (this.status & EntityStatus.NoDeserializeSystem) == EntityStatus.NoDeserializeSystem;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.NoDeserializeSystem;
                }
                else
                {
                    this.status &= ~EntityStatus.NoDeserializeSystem;
                }
            }
        }
        
        /// <summary>
        /// 标识当前实体在父实体序列化时是否一并被序列化保存。
        /// 只有实现了 <see cref="ISerializeToEntity"/> 接口（或显式将该属性设为 true）的子实体/组件，才会在存档或传送时随父实体一起序列化。
        /// </summary>
        [BsonIgnore]
        public bool IsSerializeWithParent
        {
            get => (this.status & EntityStatus.IsSerializeWithParent) == EntityStatus.IsSerializeWithParent;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsSerializeWithParent;
                }
                else
                {
                    this.status &= ~EntityStatus.IsSerializeWithParent;
                }
            }
        }

        /// <summary>
        /// 判断当前实体是否已被销毁（或尚未挂载初始化）。当 <c>InstanceId == 0</c> 时视为已销毁。
        /// </summary>
        [MemoryPackIgnore]
        [BsonIgnore]
        public bool IsDisposed => this.InstanceId == 0;
        
        /// <summary>
        /// 父实体引用（使用 <c>[AllowEntityMember]</c> 豁免分析器对直接持有 Entity 成员字段的检查）。
        /// </summary>
        [AllowEntityMember]
        private Entity parent;

        /// <summary>
        /// 获取或设置当前实体的父实体（以 Child 子实体身份挂载）。
        /// 注意：可以将实体从一个父节点迁移到另一个父节点，但绝不允许将 Parent 设置为 null 或自身，且目标父节点必须已处于场景树（<c>IScene != null</c>）上。
        /// </summary>
        // 可以改变parent，但是不能设置为null
        [MemoryPackIgnore]
        [BsonIgnore]
        public Entity Parent
        {
            get => this.parent;
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {this.GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {this.GetType().FullName}");
                }

                // 严格限制parent必须要有iSence,也就是说parent必须在数据树上面
                if (value.IScene == null)
                {
                    throw new Exception($"cant set parent because parent iSence is null: {this.GetType().FullName} {value.GetType().FullName}");
                }

                if (this.parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (this.parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                        return;
                    }

                    this.parent.RemoveChild(this.Id, false);
                }

                this.parent = value;
                this.IsComponent = false;
                this.parent.AddToChildren(this);

                if (this is IScene scene)
                {
                    scene.Fiber = this.parent.iScene.Fiber;
                    this.IScene = scene;
                }
                else
                {
                    this.IScene = this.parent.iScene;
                }

#if ENABLE_VIEW && UNITY_EDITOR
                this.ViewGO.GetComponent<ComponentView>().Component = this;
                this.ViewGO.transform.SetParent(this.parent == null ?
                        UnityEngine.GameObject.Find("Global").transform : this.parent.ViewGO.transform);
                foreach (Entity child in this.Children.Values)
                {
                    child.ViewGO.transform.SetParent(this.ViewGO.transform);
                }
                foreach (Entity comp in this.Components.Values)
                {
                    comp.ViewGO.transform.SetParent(this.ViewGO.transform);
                }
#endif
            }
        }

        /// <summary>
        /// 内部专用属性：将当前实体作为“组件（Component）”挂载到指定父实体上（仅限 <c>AddComponent</c> / <c>CreateComponent</c> 内部调用）。
        /// </summary>
        // 该方法只能在AddComponent中调用，其他人不允许调用
        [BsonIgnore]
        private Entity ComponentParent
        {
            set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {this.GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {this.GetType().FullName}");
                }

                // 严格限制parent必须要有iSence,也就是说parent必须在数据树上面
                if (value.IScene == null)
                {
                    throw new Exception($"cant set parent because parent iSence is null: {this.GetType().FullName} {value.GetType().FullName}");
                }

                if (this.parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (this.parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                        return;
                    }

                    this.parent.RemoveComponent(this.GetType(), false);
                }

                this.parent = value;
                this.IsComponent = true;
                this.parent.AddToComponents(this);
                
                if (this is IScene scene)
                {
                    scene.Fiber = this.parent.iScene.Fiber;
                    this.IScene = scene;
                }
                else
                {
                    this.IScene = this.parent.iScene;
                }
            }
        }

        /// <summary>
        /// 获取强类型的父实体。常用于在组件的 System 中通过 <c>self.GetParent&lt;Unit&gt;()</c> 拿到挂载该组件的宿主实体。
        /// </summary>
        public T GetParent<T>() where T : Entity
        {
            return this.parent as T;
        }

        /// <summary>
        /// 实体的业务唯一 ID（持久化 ID，同时作为 MongoDB 的 <c>_id</c> 主键）。
        /// 对于 Child 实体，通常由 <see cref="IdGenerater"/> 生成或外部指定；对于 Component 组件，默认与其父实体的 <c>Id</c> 一致。
        /// </summary>
        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        [BsonElement]
        [BsonId]
        public long Id { get; protected set; }

        /// <summary>
        /// 当前实体所属的场景接口引用。
        /// </summary>
        [BsonIgnore]
        protected IScene iScene;

        /// <summary>
        /// 获取或设置当前实体所在的 <see cref="IScene"/>。
        /// 当实体首次被设置 <c>IScene</c>（即正式挂载到实体树上）时，会自动分配 <see cref="InstanceId"/>、注册到 <see cref="EntitySystem"/>，
        /// 并递归恢复所有反序列化出来的子实体与组件的父子关系及 <see cref="IDeserializeSystem"/> 回调。
        /// </summary>
        [MemoryPackIgnore]
        [BsonIgnore]
        public IScene IScene
        {
            get
            {
                return this.iScene;
            }
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"iScene cant set null: {this.GetType().FullName}");
                }

                if (this.iScene == value)
                {
                    return;
                }

                if (this.iScene != null)
                {
                    this.iScene = value;
                    return;
                }

                this.iScene = value;
                
                // 首次挂到场景树上时，从所属 Fiber 生成唯一的 InstanceId
                if (this.InstanceId == 0)
                {
                    this.InstanceId = iScene.Fiber.NewInstanceId();
                }

                this.IsRegister = true;
                
                // 实现了 ISerializeToEntity 标记接口的实体会自动随父节点一起序列化
                if (this is ISerializeToEntity)
                {
                    this.IsSerializeWithParent = true;
                }

                // 反序列化出来的需要设置父子关系
                if (this.components != null)
                {
                    foreach ((long _, Entity component) in this.components)
                    {
                        component.IsComponent = true;
                        component.parent = this;
                        component.IScene = this.iScene;
                    }
                }

                if (this.children != null)
                {
                    foreach ((long _, Entity child) in this.children)
                    {
                        child.IsComponent = false;
                        child.parent = this;
                        child.IScene = this.iScene;
                    }
                }
                    
                if (!this.IsNoDeserializeSystem)
                {
                    EntitySystemSingleton.Instance.Deserialize(this);
                }
            }
        }

        /// <summary>
        /// 子实体集合（懒加载，仅在有子实体时从对象池分配 <see cref="ChildrenCollection"/>）。
        /// </summary>
        [MemoryPackInclude]
        [BsonElement]
        [BsonIgnoreIfNull]
        protected ChildrenCollection children;

        /// <summary>
        /// 获取当前实体的子实体字典（Key 为子实体的 <see cref="Id"/>，Value 为子实体）。若尚未初始化则自动从对象池获取。
        /// </summary>
        [MemoryPackIgnore]
        [BsonIgnore]
        public ChildrenCollection Children
        {
            get
            {
                return this.children ??= ObjectPool.Fetch<ChildrenCollection>();
            }
        }

        /// <summary>
        /// 将子实体加入 <see cref="Children"/> 集合中。
        /// </summary>
        private void AddToChildren(Entity entity)
        {
            this.Children.Add(entity.Id, entity);
        }

        /// <summary>
        /// 组件集合（懒加载，仅在挂载组件时从对象池分配 <see cref="ComponentsCollection"/>）。
        /// </summary>
        [MemoryPackInclude]
        [BsonElement]
        [BsonIgnoreIfNull]
        protected ComponentsCollection components;

        /// <summary>
        /// 获取当前实体挂载的所有组件字典（Key 为组件类型的 LongHashCode，Value 为组件实例）。若尚未初始化则自动从对象池获取。
        /// </summary>
        [MemoryPackIgnore]
        [BsonIgnore]
        public ComponentsCollection Components
        {
            get
            {
                return this.components ??= ObjectPool.Fetch<ComponentsCollection>();
            }
        }

        /// <summary>
        /// 获取当前实体挂载的组件数量（不会触发 <see cref="components"/> 集合的懒加载分配）。
        /// </summary>
        public int ComponentsCount()
        {
            if (this.components == null)
            {
                return 0;
            }
            return this.components.Count;
        }
        
        /// <summary>
        /// 获取当前实体挂载的子实体数量（不会触发 <see cref="children"/> 集合的懒加载分配）。
        /// </summary>
        public int ChildrenCount()
        {
            if (this.children == null)
            {
                return 0;
            }
            return this.children.Count;
        }

        /// <summary>
        /// 销毁当前实体：
        /// 1. 将 <see cref="InstanceId"/> 置 0（标记为已销毁，使所有指向它的 <see cref="EntityRef{T}"/> 立即失效）；
        /// 2. 递归销毁所有子实体（<see cref="children"/>）和所有组件（<see cref="components"/>）；
        /// 3. 触发当前实体对应的 <see cref="IDestroySystem"/> 回调；
        /// 4. 从父实体的集合中移除自身，并回收到 <see cref="ObjectPool"/> 对象池。
        /// </summary>
        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsRegister = false;
            this.InstanceId = 0;

            // 清理Children
            if (this.children != null)
            {
                foreach (Entity child in this.children.Values)
                {
                    child.Dispose();
                }

                this.children.Dispose();
                this.children = null;
            }

            // 清理Component
            if (this.components != null)
            {
                foreach (var kv in this.components)
                {
                    kv.Value.Dispose();
                }

                this.components.Dispose();
                this.components = null;
            }

            // 触发Destroy事件
            if (this is IDestroy)
            {
                EntitySystemSingleton.Instance.Destroy(this);
            }

            this.iScene = null;

            if (this.parent != null && !this.parent.IsDisposed)
            {
                if (this.IsComponent)
                {
                    this.parent.RemoveComponent(this.GetType(), false);
                }
                else
                {
                    this.parent.RemoveChild(this.Id, false);
                }
            }

            this.parent = null;
            
            bool isFromPool = this.IsFromPool;
            this.status = EntityStatus.None;
            this.IsFromPool = isFromPool;

            base.Dispose();
        }

        /// <summary>
        /// 将组件加入 <see cref="Components"/> 字典中。
        /// </summary>
        private void AddToComponents(Entity component)
        {
            this.Components.Add(component.GetLongHashCode(), component);
        }

        /// <summary>
        /// 根据业务 <paramref name="id"/> 查找并返回指定类型的子实体。若不存在则返回 null。
        /// </summary>
        public K GetChild<K>(long id) where K : Entity
        {
            this.CheckThread();
            
            if (this.children == null)
            {
                return null;
            }

            this.children.TryGetValue(id, out Entity child);
            return child as K;
        }

        /// <summary>
        /// 从当前实体下移除指定 <paramref name="id"/> 的子实体。
        /// </summary>
        /// <param name="id">要移除的子实体 Id</param>
        /// <param name="isDispose">是否在移除的同时彻底销毁该子实体（默认为 true；当子实体只是迁移到其他父节点时传入 false）</param>
        public bool RemoveChild(long id, bool isDispose = true)
        {
            if (this.children == null)
            {
                return false;
            }

            if (!this.children.Remove(id, out Entity child))
            {
                return false;
            }

            if (this.children.Count == 0)
            {
                this.children.Dispose();
                this.children = null;
            }

            if (isDispose)
            {
                child.Dispose();
            }
            else
            {
                child.Reset();
            }

            return true;
        }

        /// <summary>
        /// 从当前实体上移除泛型类型 <typeparamref name="K"/> 的组件。
        /// </summary>
        /// <param name="isDispose">是否在移除的同时销毁该组件（默认为 true）</param>
        public void RemoveComponent<K>(bool isDispose = true) where K : Entity
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.components == null)
            {
                return;
            }

            Type type = typeof (K);

            if (!this.components.Remove(this.GetComponentLongHashCode(type), out Entity c))
            {
                return;
            }

            if (isDispose)
            {
                c.Dispose();
            }
            else
            {
                c.Reset();
            }
        }

        /// <summary>
        /// 从当前实体上移除指定 <paramref name="type"/> 类型的组件。
        /// </summary>
        /// <param name="type">要移除的组件类型</param>
        /// <param name="isDispose">是否在移除的同时销毁该组件（默认为 true）</param>
        public void RemoveComponent(Type type, bool isDispose = true)
        {
            if (this.IsDisposed)
            {
                return;
            }
            
            if (this.components == null)
            {
                return;
            }

            if (!this.components.Remove(this.GetComponentLongHashCode(type), out Entity c))
            {
                return;
            }

            if (isDispose)
            {
                c.Dispose();
            }
            else
            {
                c.Reset();
            }
        }

        /// <summary>
        /// 仅在 DEBUG 模式下生效的线程安全断言：检查当前操作实体的线程是否与该实体所属 <see cref="Fiber"/> 绑定的单线程上下文一致，防止跨线程并发读写 Entity 树。
        /// </summary>
        [Conditional("DEBUG")]
        private void CheckThread()
        {
            Fiber fiber = this.IScene.Fiber;
            if (fiber.ThreadSynchronizationContext.ThreadId != Environment.CurrentManagedThreadId)
            {
                throw new Exception($"Fiber {fiber.Id} {fiber.Name} is not in fiber thread {fiber.ThreadSynchronizationContext.ThreadId} {Environment.CurrentManagedThreadId}");
            }
        }

        /// <summary>
        /// 获取当前实体上挂载的指定类型 <typeparamref name="K"/> 的组件。若未挂载则返回 null。
        /// </summary>
        public K GetComponent<K>() where K : Entity
        {
            this.CheckThread();
            
            if (this.components == null)
            {
                return null;
            }

            // 如果有IGetComponent接口，则触发GetComponentSystem
            if (this is IGetComponentSys)
            {
                EntitySystemSingleton.Instance.GetComponentSys(this, typeof(K));
            }
            
            Entity component;
            if (!this.components.TryGetValue(this.GetComponentLongHashCode(typeof (K)), out component))
            {
                return default;
            }

            return (K) component;
        }

        /// <summary>
        /// 根据运行时 <see cref="Type"/> 获取当前实体上挂载的组件。若未挂载则返回 null。
        /// </summary>
        public Entity GetComponent(Type type)
        {
            this.CheckThread();
            
            if (this.components == null)
            {
                return null;
            }

            // 如果有IGetComponent接口，则触发GetComponentSystem
            // 这个要在tryget之前调用，因为有可能components没有，但是执行GetComponentSystem后又有了
            if (this is IGetComponentSys)
            {
                EntitySystemSingleton.Instance.GetComponentSys(this, type);
            }
            
            Entity component;
            if (!this.components.TryGetValue(this.GetComponentLongHashCode(type), out component))
            {
                return null;
            }

            return component;
        }

        /// <summary>
        /// 内部工厂方法：通过 <see cref="ObjectPool"/> 创建指定类型的 Entity 实例并初始化基础状态。
        /// </summary>
        private static Entity Create(Type type, bool isFromPool)
        {
            Entity component = (Entity) ObjectPool.Fetch(type, isFromPool);
            component.IsFromPool = true;
            component.IsNoDeserializeSystem = true;
            component.Id = 0;
            return component;
        }

        /// <summary>
        /// 创建并挂载一个组件（尚未触发 Awake，供各 AddComponent 重载内部调用）。
        /// 同一个父实体上绝不允许重复挂载相同类型的组件。
        /// </summary>
        protected Entity CreateComponent(Type type, long id, bool isFromPool)
        {
            this.CheckThread();
            
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            return component;
        }

        /// <summary>
        /// 创建并挂载一个子实体（尚未触发 Awake，供各 AddChild 重载内部调用）。
        /// </summary>
        protected Entity CreateChild(Type type, long id, bool isFromPool)
        {
            this.CheckThread();
            
            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;
            return component;
        }

        /// <summary>
        /// 将一个已存在的实体实例作为组件挂载到当前实体上（注意：此重载不会触发 <c>Awake</c>）。
        /// </summary>
        public Entity AddComponent(Entity component)
        {
            this.CheckThread();
            
            Type type = component.GetType();
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            component.ComponentParent = this;

            return component;
        }

        /// <summary>
        /// 根据运行时 <see cref="Type"/> 创建并挂载组件，同时触发其无参 <c>Awake</c> 生命周期。
        /// </summary>
        public Entity AddComponent(Type type, bool isFromPool = false)
        {
            this.CheckThread();
            
            if (this.components != null && this.components.ContainsKey(this.GetComponentLongHashCode(type)))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = this.Id;
            component.ComponentParent = this;
            EntitySystemSingleton entitySystemSingleton = EntitySystemSingleton.Instance;
            entitySystemSingleton.Awake(component);

            return component;
        }

        /// <summary>
        /// 创建并挂载指定自定义 <paramref name="id"/> 的组件 <typeparamref name="K"/>，并触发无参 <see cref="IAwake"/>。
        /// </summary>
        public K AddComponentWithId<K>(long id, bool isFromPool = false) where K : Entity, IAwake, new()
        {
            Entity component = this.CreateComponent(typeof (K), id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component);
            return component as K;
        }

        /// <summary>
        /// 创建并挂载指定自定义 <paramref name="id"/> 的组件 <typeparamref name="K"/>，并触发带 1 个参数的 <see cref="IAwake{P1}"/>。
        /// </summary>
        public K AddComponentWithId<K, P1>(long id, P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            Entity component = this.CreateComponent(typeof (K), id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1);
            return component as K;
        }

        /// <summary>
        /// 创建并挂载指定自定义 <paramref name="id"/> 的组件 <typeparamref name="K"/>，并触发带 2 个参数的 <see cref="IAwake{P1, P2}"/>。
        /// </summary>
        public K AddComponentWithId<K, P1, P2>(long id, P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            Entity component = this.CreateComponent(typeof (K), id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1, p2);
            return component as K;
        }

        /// <summary>
        /// 创建并挂载指定自定义 <paramref name="id"/> 的组件 <typeparamref name="K"/>，并触发带 3 个参数的 <see cref="IAwake{P1, P2, P3}"/>。
        /// </summary>
        public K AddComponentWithId<K, P1, P2, P3>(long id, P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            Entity component = this.CreateComponent(typeof (K), id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1, p2, p3);
            return component as K;
        }

        /// <summary>
        /// 创建并挂载组件 <typeparamref name="K"/>（组件 Id 默认等于当前父实体的 Id），并触发无参 <see cref="IAwake"/>。
        /// </summary>
        public K AddComponent<K>(bool isFromPool = false) where K : Entity, IAwake, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.Id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component);
            return component as K;
        }

        /// <summary>
        /// 创建并挂载组件 <typeparamref name="K"/>，并触发带 1 个初始化参数的 <see cref="IAwake{P1}"/>。
        /// </summary>
        public K AddComponent<K, P1>(P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.Id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1);
            return component as K;
        }

        /// <summary>
        /// 创建并挂载组件 <typeparamref name="K"/>，并触发带 2 个初始化参数的 <see cref="IAwake{P1, P2}"/>。
        /// </summary>
        public K AddComponent<K, P1, P2>(P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.Id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1, p2);
            return component as K;
        }

        /// <summary>
        /// 创建并挂载组件 <typeparamref name="K"/>，并触发带 3 个初始化参数的 <see cref="IAwake{P1, P2, P3}"/>。
        /// </summary>
        public K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.Id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1, p2, p3);
            return component as K;
        }

        /// <summary>
        /// 将一个已存在的实体实例作为子实体（Child）挂载到当前实体下（注意：不会再次触发 Awake）。
        /// </summary>
        public Entity AddChild(Entity entity)
        {
            entity.Parent = this;
            return entity;
        }

        /// <summary>
        /// 创建并挂载子实体 <typeparamref name="T"/>（自动通过 <see cref="IdGenerater"/> 生成唯一业务 Id），并触发无参 <see cref="IAwake"/>。
        /// </summary>
        public T AddChild<T>(bool isFromPool = false) where T : Entity, IAwake
        {
            Type type = typeof (T);
            T component = (T) this.CreateChild(type, IdGenerater.Instance.GenerateId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        /// <summary>
        /// 创建并挂载子实体 <typeparamref name="T"/>（自动生成 Id），并触发带 1 个参数的 <see cref="IAwake{A}"/>。
        /// </summary>
        public T AddChild<T, A>(A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            Type type = typeof (T);
            T component = (T) this.CreateChild(type, IdGenerater.Instance.GenerateId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        /// <summary>
        /// 创建并挂载子实体 <typeparamref name="T"/>（自动生成 Id），并触发带 2 个参数的 <see cref="IAwake{A, B}"/>。
        /// </summary>
        public T AddChild<T, A, B>(A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            Type type = typeof (T);
            T component = (T) this.CreateChild(type, IdGenerater.Instance.GenerateId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        /// <summary>
        /// 创建并挂载子实体 <typeparamref name="T"/>（自动生成 Id），并触发带 3 个参数的 <see cref="IAwake{A, B, C}"/>。
        /// </summary>
        public T AddChild<T, A, B, C>(A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof (T);
            T component = (T) this.CreateChild(type, IdGenerater.Instance.GenerateId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        /// <summary>
        /// 使用指定的业务 <paramref name="id"/> 创建并挂载子实体 <typeparamref name="T"/>，并触发无参 <see cref="IAwake"/>。
        /// </summary>
        public T AddChildWithId<T>(long id, bool isFromPool = false) where T : Entity, IAwake
        {
            T component = (T) this.CreateChild(typeof (T), id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        /// <summary>
        /// 使用指定的业务 <paramref name="id"/> 创建并挂载子实体 <typeparamref name="T"/>，并触发带 1 个参数的 <see cref="IAwake{A}"/>。
        /// </summary>
        public T AddChildWithId<T, A>(long id, A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            T component = (T) this.CreateChild(typeof (T), id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        /// <summary>
        /// 使用指定的业务 <paramref name="id"/> 创建并挂载子实体 <typeparamref name="T"/>，并触发带 2 个参数的 <see cref="IAwake{A, B}"/>。
        /// </summary>
        public T AddChildWithId<T, A, B>(long id, A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            T component = (T) this.CreateChild(typeof (T), id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        /// <summary>
        /// 使用指定的业务 <paramref name="id"/> 创建并挂载子实体 <typeparamref name="T"/>，并触发带 3 个参数的 <see cref="IAwake{A, B, C}"/>。
        /// </summary>
        public T AddChildWithId<T, A, B, C>(long id, A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            T component = (T) this.CreateChild(typeof (T), id, isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        /// <summary>
        /// 序列化前置回调（在序列化实体前触发）：
        /// 递归调用当前实体及所有跟随序列化的子实体/组件的 <see cref="ISerializeSystem"/> 回调，用于将运行时临时数据整理到可序列化字段中。
        /// </summary>
        public override void BeginInit()
        {
            // 如果没有挂到树上，不用执行SerializeSystem
            if (this.iScene == null)
            {
                return;
            }
            
            EntitySystemSingleton.Instance.Serialize(this);
            
            if (this.components is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.components)
                {
                    if (entity.IsSerializeWithParent)
                    {
                        entity.BeginInit();
                    }
                }
            }

            if (this.children is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.children)
                {
                    if (entity.IsSerializeWithParent)
                    {
                        entity.BeginInit();
                    }
                }
            }
        }
        
        /// <summary>
        /// 当实体仅从原父节点摘除（不销毁，例如准备挂载到新父节点或跨场景转移）时调用，递归重置自身及子树的场景注册与 InstanceId 状态。
        /// </summary>
        private void Reset()
        {
            if (this.iScene == null)
            {
                return;
            }
            
            this.InstanceId = 0;
            this.iScene = null;
            this.IsNoDeserializeSystem = false;
            this.IsRegister = false;
            
            if (this.components is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.components)
                {
                    entity.Reset();
                }
            }

            if (this.children is { Count: > 0 })
            {
                foreach ((long _, Entity entity) in this.children)
                {
                    entity.Reset();
                }
            }
        }
        
        /// <summary>
        /// 递归移除并销毁所有未标记为跟随父实体序列化（<c>IsSerializeWithParent == false</c>）的组件与子实体。
        ///</summary>
        // 移除不需要序列化的Entity
        public void ClearUnSerialized()
        {
            if (this.components is { Count: > 0 })
            {
                using ListComponent<Type> types = ListComponent<Type>.Create();
                foreach ((long _, Entity entity) in this.components)
                {
                    if (!entity.IsSerializeWithParent)
                    {
                        types.Add(entity.GetType());
                        continue;
                    }
                    entity.ClearUnSerialized();
                }

                foreach (Type removeType in types)
                {
                    this.RemoveComponent(removeType);
                }
            }

            if (this.children is { Count: > 0 })
            {
                using ListComponent<long> ids = ListComponent<long>.Create();
                
                foreach ((long id, Entity entity) in this.children)
                {
                    if (!entity.IsSerializeWithParent)
                    {
                        ids.Add(id);
                        continue;
                    }
                    
                    entity.ClearUnSerialized();
                }
                
                foreach (long id in ids)
                {
                    this.RemoveChild(id);
                }
            }
        }
    }
}
