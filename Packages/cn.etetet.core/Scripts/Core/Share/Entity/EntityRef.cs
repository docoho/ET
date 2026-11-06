using System;
#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace ET
{
    /// <summary>
    /// 实体安全引用结构体（值类型包装器）。用于在组件字段或跨越 <c>await</c> 异步等待点时安全持有 <see cref="Entity"/> 引用。
    /// </summary>
    /// <remarks>
    /// 【初学者必读 —— 为什么不能直接用 <c>Unit unit;</c> 保存实体引用，而必须用 <c>EntityRef&lt;Unit&gt;</c>？】
    /// 1. <b>对象池 ABA 复用陷阱</b>：
    ///    在 ET 中，Entity 销毁（<c>Dispose</c>）后会被放回对象池。如果你直接持有 <c>Unit a</c> 的 C# 引用，
    ///    当 <c>a</c> 死亡销毁并被对象池分配给新刷出来的怪物 <c>b</c> 时，你手里的 <c>a</c> 引用实际上变成了 <c>b</c>！
    /// 2. <b>异步 <c>await</c> 悬空问题</b>：
    ///    当你执行 <c>await timer.WaitAsync(1000);</c> 时，在这 1 秒内该实体很可能因为玩家下线或切场景而被销毁（<c>InstanceId</c> 变为 0 或改变）。
    /// 3. <b><c>EntityRef&lt;T&gt;</c> 的工作原理</b>：
    ///    它在构造时同时记录了实体对象引用 <c>entity</c> 以及当时的 <c>instanceId</c> 快照。
    ///    每次通过隐式转换或 <see cref="Entity"/> 属性取值时，都会比对 <c>this.entity.InstanceId == this.instanceId</c>：
    ///    - 如果相等，说明还是原来的那个活着的实体，正常返回；
    ///    - 如果不相等（实体已 Dispose 归零，或已被对象池复用作其他实体），立即将内部引用置空并返回 <c>null</c>！
    /// 4. <b>使用示例</b>：
    ///    <code>
    ///    EntityRef&lt;Unit&gt; unitRef = unit; // 隐式转换保存快照
    ///    await root.GetComponent&lt;TimerComponent&gt;().WaitAsync(1000);
    ///    unit = unitRef; // 隐式转换还原，若在等待期间已销毁则自动得到 null
    ///    if (unit == null) return;
    ///    </code>
    /// </remarks>
    public struct EntityRef<T>: IDisposable, IEquatable<EntityRef<T>> where T: Entity
    {
        /// <summary>创建该引用时的目标实体 InstanceId 快照（只读）</summary>
        private readonly long instanceId;
        /// <summary>目标实体的底层引用（当检测到 InstanceId 不匹配时会自动置为 null 以释放 GC 引用）</summary>
        private T entity;
        
        /// <summary>
        /// 若引用的目标实体仍然有效（未被销毁），则调用其 <see cref="Entity.Dispose"/> 销毁该实体。
        /// 支持 <c>using EntityRef&lt;T&gt; x = ...</c> 语法糖。
        /// </summary>
        public void Dispose()
        {
            T t = this.Entity;
            
            if (t == null)
            {
                return;
            }

            t.Dispose();
        }

        public override int GetHashCode()
        {
            return this.instanceId.GetHashCode();
        }

        public bool Equals(EntityRef<T> obj)
        {
            return this.instanceId == obj.instanceId;
        }

        public override bool Equals(object obj)
        {
            if (obj is not EntityRef<T> entityRef)
            {
                return false;
            }

            return this.instanceId == entityRef.instanceId;
        }
        
        public static bool operator ==(EntityRef<T> left, EntityRef<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityRef<T> left, EntityRef<T> right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// 私有构造函数（外部通过隐式转换 <c>EntityRef&lt;T&gt; r = entity;</c> 创建）。
        /// 注意：不允许将一个已经 Dispose（<c>InstanceId == 0</c>）的非空实体赋值给 <see cref="EntityRef{T}"/>。
        /// </summary>
        private EntityRef(T t)
        {
            if (t == null)
            {
                this.instanceId = 0;
                this.entity = null;
                return;
            }
            if (t.InstanceId == 0)
            {
                throw new Exception("entity is disposed, instanceid == 0!");
            }

            this.instanceId = t.InstanceId;
            this.entity = t;
        }
        
        /// <summary>
        /// 安全获取目标实体：如果实体已销毁或已被对象池复用作其他实例（<c>InstanceId</c> 发生变化），会自动解除引用并返回 <c>null</c>。
        /// </summary>
        public T Entity
        {
            get
            {
                if (this.entity == null)
                {
                    return null;
                }
                if (this.entity.InstanceId != this.instanceId)
                {
                    // 这里instanceId变化了，设置为null，解除引用，好让runtime去gc
                    this.entity = null;
                }
                return this.entity;
            }
        }
        
        /// <summary>
        /// 支持从实体 <typeparamref name="T"/> 到 <see cref="EntityRef{T}"/> 的隐式转换。
        /// </summary>
        public static implicit operator EntityRef<T>(T t)
        {
            return new EntityRef<T>(t);
        }

        /// <summary>
        /// 支持从 <see cref="EntityRef{T}"/> 到实体 <typeparamref name="T"/> 的隐式转换（内部自动调用 <see cref="Entity"/> 属性进行有效性校验）。
        /// </summary>
        public static implicit operator T(EntityRef<T> v)
        {
            return v.Entity;
        }

        public override string ToString()
        {
            return this.instanceId.ToString();
        }


#if UNITY_EDITOR && ENABLE_VIEW
        [HorizontalGroup("EntityRefDebug")]
        [ShowInInspector, ReadOnly, LabelText("Target Entity")]
        private T DebugEntity => this.Entity;

        [HorizontalGroup("EntityRefDebug")]
        [Button("Select Entity"), ShowIf("@DebugEntity != null")]
        private void SelectEntityInHierarchy()
        {
            var target = DebugEntity;
            if (target != null && target.ViewGO != null)
            {
                UnityEditor.Selection.activeGameObject = target.ViewGO;
            }
            else
            {
                Log.Warning("[EntityRef] 无法选中，ViewGO 为空");
            }
        }
#endif
    }
    
    
    /// <summary>
    /// 实体弱引用结构体（基于 <see cref="WeakReference{T}"/> + <c>InstanceId</c> 双重校验）。
    /// 当不希望因为長期持有未访问的 <see cref="EntityRef{T}"/> 而阻止已销毁且未入池实体的 GC 回收时使用。
    /// </summary>
    public struct EntityWeakRef<T>: IDisposable, IEquatable<EntityWeakRef<T>> where T: Entity
    {
        private long instanceId;
        // 使用WeakReference，这样不会导致entity dispose了却无法gc的问题
        // 不过暂时没有测试WeakReference的性能
        private readonly WeakReference<T> weakRef;

        private EntityWeakRef(T t)
        {
            if (t == null)
            {
                this.instanceId = 0;
                this.weakRef = null;
                return;
            }

            if (t.InstanceId == 0)
            {
                throw new Exception("cant convert to entityref, entity instanceid == 0!");
            }
            this.instanceId = t.InstanceId;
            this.weakRef = new WeakReference<T>(t);
        }
        
        public void Dispose()
        {
            T t = this.Entity;
            
            if (t == null)
            {
                return;
            }

            t.Dispose();
        }
        
        /// <summary>
        /// 安全获取弱引用指向的目标实体。若已被 GC 回收或已被销毁（<c>InstanceId</c> 变化），则返回 <c>null</c>。
        /// </summary>
        public T Entity
        {
            get
            {
                if (this.instanceId == 0)
                {
                    return null;
                }

                if (this.weakRef == null)
                {
                    this.instanceId = 0;
                    return null;
                }

                if (!this.weakRef.TryGetTarget(out T entity))
                {
                    this.instanceId = 0;
                    return null;
                }

                if (entity.InstanceId != this.instanceId)
                {
                    this.instanceId = 0;
                    return null;
                }
                return entity;
            }
        }
        
        public static implicit operator EntityWeakRef<T>(T t)
        {
            return new EntityWeakRef<T>(t);
        }

        public static implicit operator T(EntityWeakRef<T> v)
        {
            return v.Entity;
        }
        
        public override int GetHashCode()
        {
            return this.instanceId.GetHashCode();
        }

        public bool Equals(EntityWeakRef<T> obj)
        {
            return this.instanceId == obj.instanceId;
        }

        public override bool Equals(object obj)
        {
            if (obj is not EntityWeakRef<T> entityRef)
            {
                return false;
            }

            return this.instanceId == entityRef.instanceId;
        }
        
        public static bool operator ==(EntityWeakRef<T> left, EntityWeakRef<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityWeakRef<T> left, EntityWeakRef<T> right)
        {
            return !left.Equals(right);
        }
        
#if UNITY_EDITOR && ENABLE_VIEW
        [HorizontalGroup("EntityWeakRefDebug")]
        [ShowInInspector, ReadOnly, LabelText("Target Entity")]
        private T DebugEntity => this.Entity;

        [HorizontalGroup("EntityWeakRefDebug")]
        [Button("Select Entity"), ShowIf("@DebugEntity != null")]
        private void SelectEntityInHierarchy()
        {
            var target = DebugEntity;
            if (target != null && target.ViewGO != null)
            {
                UnityEditor.Selection.activeGameObject = target.ViewGO;
            }
            else
            {
                Log.Warning("[EntityWeakRef] 无法选中，ViewGO 为空");
            }
        }
#endif
    }
}
