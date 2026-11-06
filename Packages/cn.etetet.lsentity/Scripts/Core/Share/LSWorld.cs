using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using MemoryPack;
using TrueSync;

namespace ET
{
    /// <summary>
    /// <see cref="LSWorld"/> 与 <see cref="LSEntity"/> 的扩展工具类，提供快速访问所属帧同步世界、确定性 ID 生成器及定点数随机源的方法
    /// </summary>
    public static class LSWorldSystem
    {
        /// <summary>
        /// 获取当前帧同步实体所属的 <see cref="ET.LSWorld"/> 实例
        /// </summary>
        public static LSWorld LSWorld(this LSEntity entity)
        {
            return entity.IScene as LSWorld;
        }

        /// <summary>
        /// 从所属 <see cref="ET.LSWorld"/> 生成一个确定性自增实体 ID
        /// </summary>
        public static long GetId(this LSEntity entity)
        {
            return entity.LSWorld().GetId();
        }
        
        /// <summary>
        /// 获取所属 <see cref="ET.LSWorld"/> 的确定性定点数随机数生成器（<see cref="TSRandom"/>）
        /// </summary>
        public static TSRandom GetRandom(this LSEntity entity)
        {
            return entity.LSWorld().Random;
        }
    }

    /// <summary>
    /// 帧同步逻辑世界实体（实现 <see cref="IScene"/>），作为一局帧同步战斗所有 <see cref="LSEntity"/> 的根容器。
    /// <para>支持通过 MemoryPack 或 Bson 对整棵帧同步世界树进行完整快照序列化与反序列化，从而实现帧同步回滚与断线重连追帧。</para>
    /// </summary>
    [EnableMethod]
    [ChildOf]
    [MemoryPackable]
    public partial class LSWorld: Entity, IAwake, IScene
    {
        /// <summary>
        /// MemoryPack 反序列化专用无参构造函数
        /// </summary>
        [MemoryPackConstructor]
        public LSWorld()
        {
        }
        
        /// <summary>
        /// 指定场景类型构造帧同步世界实例
        /// </summary>
        /// <param name="sceneType">场景类型标识</param>
        public LSWorld(int sceneType)
        {
            this.Id = this.GetId();

            this.SceneType = sceneType;
        }

        /// <summary>
        /// 帧同步确定性逻辑帧更新调度器
        /// </summary>
        private readonly LSUpdater updater = new();
        
        /// <summary>
        /// 所属纤程实例（不参与快照序列化）
        /// </summary>
        [BsonIgnore]
        [MemoryPackIgnore]
        public Fiber Fiber { get; set; }
        
        /// <summary>
        /// 世界内部确定性自增 ID 计数器（参与快照序列化，保证回滚后生成的 ID 序列完全一致）
        /// </summary>
        [BsonElement]
        [MemoryPackInclude]
        private long idGenerator;

        /// <summary>
        /// 分配下一个确定性自增实体 ID
        /// </summary>
        public long GetId()
        {
            return ++this.idGenerator;
        }

        /// <summary>
        /// 确定性定点数伪随机数生成器（随世界快照一起序列化）
        /// </summary>
        public TSRandom Random { get; set; }
        
        /// <summary>
        /// 场景类型标识（不参与快照序列化）
        /// </summary>
        [BsonIgnore]
        [MemoryPackIgnore]
        public int SceneType { get; set; }
        
        /// <summary>
        /// 当前世界所处的逻辑帧号
        /// </summary>
        public int Frame { get; set; }

        /// <summary>
        /// 驱动世界向前推进一个逻辑帧：执行所有注册实体的 <see cref="ILSUpdateSystem"/> 并递增帧号
        /// </summary>
        public void Update()
        {
            this.updater.Update();
            ++this.Frame;
        }

        /// <summary>
        /// 将帧同步实体注册到当前世界的逻辑帧更新调度器中
        /// </summary>
        public void RegisterSystem(LSEntity entity)
        {
            this.updater.Add(entity);
        }
        
        // lsworld自己时挂在entity上的，但是自己的组件却是LSEntity，所以这里两个get hash code的实现不一样
        public override long GetLongHashCode()
        {
            return this.GetType().TypeHandle.Value.ToInt64();
        }
        
        public override long GetComponentLongHashCode(Type type)
        {
            return LSEntitySystemSingleton.Instance.GetLongHashCode(type);
        }
        
        
        public new K AddComponent<K>(bool isFromPool = false) where K : LSEntity, IAwake, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component);
            return component as K;
        }

        public new K AddComponent<K, P1>(P1 p1, bool isFromPool = false) where K : LSEntity, IAwake<P1>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1);
            return component as K;
        }

        public new K AddComponent<K, P1, P2>(P1 p1, P2 p2, bool isFromPool = false) where K : LSEntity, IAwake<P1, P2>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1, p2);
            return component as K;
        }

        public new K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : LSEntity, IAwake<P1, P2, P3>, new()
        {
            Entity component = this.CreateComponent(typeof (K), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, p1, p2, p3);
            return component as K;
        }

        public new T AddChild<T>(bool isFromPool = false) where T : LSEntity, IAwake
        {
            T component = (T) this.CreateChild(typeof (T), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        public new T AddChild<T, A>(A a, bool isFromPool = false) where T : LSEntity, IAwake<A>
        {
            T component = (T) this.CreateChild(typeof (T), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        public new T AddChild<T, A, B>(A a, B b, bool isFromPool = false) where T : LSEntity, IAwake<A, B>
        {
            T component = (T) this.CreateChild(typeof (T), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        public new T AddChild<T, A, B, C>(A a, B b, C c, bool isFromPool = false) where T : LSEntity, IAwake<A, B, C>
        {
            T component = (T) this.CreateChild(typeof (T), this.GetId(), isFromPool);
            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

    }
}
