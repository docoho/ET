using System;

namespace ET
{
    /// <summary>
    /// 帧同步专用实体抽象基类（LockStep Entity）。
    /// <para>与普通 <see cref="Entity"/> 的核心区别：</para>
    /// <para>1. 所有子实体与组件必须同样继承自 <see cref="LSEntity"/>，且实体 ID 由所属 <see cref="LSWorld.GetId"/> 确定性自增分配；</para>
    /// <para>2. 类型哈希码基于类型全名字符串计算（而非运行时 TypeHandle 指针），保证跨机器、跨进程序列化与遍历顺序完全一致；</para>
    /// <para>3. 自动向所属 <see cref="LSWorld"/> 注册 <see cref="ILSUpdateSystem"/> 逻辑帧更新。</para>
    /// </summary>
    [EnableMethod]
    public abstract partial class LSEntity: Entity
    {
        /// <summary>
        /// 添加无参初始化的帧同步组件（使用 <see cref="LSWorld"/> 确定性 ID 生成器分配 ID）
        /// </summary>
        public new K AddComponent<K>(bool isFromPool = false) where K : LSEntity, IAwake, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component);
            return component as K;
        }

        /// <summary>
        /// 添加带 1 个初始化参数的帧同步组件
        /// </summary>
        public new K AddComponent<K, P1>(P1 p1, bool isFromPool = false) where K : LSEntity, IAwake<P1>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1);
            return component as K;
        }

        /// <summary>
        /// 添加带 2 个初始化参数的帧同步组件
        /// </summary>
        public new K AddComponent<K, P1, P2>(P1 p1, P2 p2, bool isFromPool = false) where K : LSEntity, IAwake<P1, P2>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1, p2);
            return component as K;
        }

        /// <summary>
        /// 添加带 3 个初始化参数的帧同步组件
        /// </summary>
        public new K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : LSEntity, IAwake<P1, P2, P3>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1, p2, p3);
            return component as K;
        }

        /// <summary>
        /// 添加无参初始化的帧同步子实体（使用 <see cref="LSWorld"/> 确定性 ID 生成器分配 ID）
        /// </summary>
        [EnableAccessEntiyChild]
        public new T AddChild<T>(bool isFromPool = false) where T : LSEntity, IAwake
        {
            T component = (T) this.CreateChild(typeof (T), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        /// <summary>
        /// 添加带 1 个初始化参数的帧同步子实体
        /// </summary>
        [EnableAccessEntiyChild]
        public new T AddChild<T, A>(A a, bool isFromPool = false) where T : LSEntity, IAwake<A>
        {
            T component = (T) this.CreateChild(typeof (T), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        /// <summary>
        /// 添加带 2 个初始化参数的帧同步子实体
        /// </summary>
        [EnableAccessEntiyChild]
        public new T AddChild<T, A, B>(A a, B b, bool isFromPool = false) where T : LSEntity, IAwake<A, B>
        {
            T component = (T) this.CreateChild(typeof (T), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        /// <summary>
        /// 添加带 3 个初始化参数的帧同步子实体
        /// </summary>
        [EnableAccessEntiyChild]
        public new T AddChild<T, A, B, C>(A a, B b, C c, bool isFromPool = false) where T : LSEntity, IAwake<A, B, C>
        {
            T component = (T) this.CreateChild(typeof (T), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        // LSEntity自身跟Component都是LSEntity，所以这里两个实现一样
        /// <summary>
        /// 获取当前帧同步实体类型的确定性长整型哈希值（用于跨端一致性序列化与排序）
        /// </summary>
        public override long GetLongHashCode()
        {
            return LSEntitySystemSingleton.Instance.GetLongHashCode(this.GetType());
        }
        
        /// <summary>
        /// 获取挂载组件类型的确定性长整型哈希值
        /// </summary>
        public override long GetComponentLongHashCode(Type type)
        {
            return LSEntitySystemSingleton.Instance.GetLongHashCode(type);
        }

        /// <summary>
        /// 将当前帧同步实体注册到所属 <see cref="LSWorld"/> 的逻辑帧更新队列中
        /// </summary>
        protected override void RegisterSystem()
        {
            LSWorld lsWorld = (LSWorld)this.IScene;
            TypeSystems.OneTypeSystems oneTypeSystems = LSEntitySystemSingleton.Instance.GetOneTypeSystems(this.GetType());
            if (oneTypeSystems == null)
            {
                return;
            }

            if (oneTypeSystems.ClassType.Contains(typeof(ILSUpdateSystem)))
            {
                lsWorld.RegisterSystem(this);
            }
        }
    }
}
