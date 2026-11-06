using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 帧同步实体系统全局单例，负责收集所有 <see cref="LSEntitySystemAttribute"/> 系统、维护 <see cref="LSEntity"/> 派生类的确定性类型哈希映射，以及派发 <see cref="LSUpdate"/> 与 <see cref="LSRollback"/>
    /// </summary>
    [CodeProcess]
    public class LSEntitySystemSingleton: Singleton<LSEntitySystemSingleton>, ISingletonAwake
    {
        /// <summary>
        /// 帧同步实体类型与对应系统实例的映射容器
        /// </summary>
        private TypeSystems TypeSystems { get; set; }
        
        /// <summary>
        /// 帧同步实体类型与其全名确定性长整型哈希值的双向映射表
        /// </summary>
        private readonly DoubleMap<Type, long> lsEntityTypeLongHashCode = new();
        
        /// <summary>
        /// 单例初始化：扫描注册所有 <see cref="LSEntitySystemAttribute"/> 系统，并为所有 <see cref="LSEntity"/> 子类预计算跨端一致的 FullName 长整型哈希
        /// </summary>
        public void Awake()
        {
            this.TypeSystems = new();
            foreach (Type type in CodeTypes.Instance.GetTypes(typeof (LSEntitySystemAttribute)))
            {
                SystemObject obj = (SystemObject)Activator.CreateInstance(type);

                if (obj is not ISystemType iSystemType)
                {
                    continue;
                }

                TypeSystems.OneTypeSystems oneTypeSystems = this.TypeSystems.GetOrCreateOneTypeSystems(iSystemType.Type());
                oneTypeSystems.Map.Add(iSystemType.SystemType(), obj);
                if (iSystemType is IEventSystem)
                {
                    oneTypeSystems.ClassType.Add(iSystemType.SystemType());
                }
            }
            
            foreach (var kv in CodeTypes.Instance.GetTypes())
            {
                Type type = kv.Value;
                if (typeof(LSEntity).IsAssignableFrom(type))
                {
                    long hash = type.FullName.GetLongHashCode();
                    try
                    {
                        this.lsEntityTypeLongHashCode.Add(type, type.FullName.GetLongHashCode());
                    }
                    catch (Exception e)
                    {
                        Type sameHashType = this.lsEntityTypeLongHashCode.GetKeyByValue(hash);
                        throw new Exception($"long hash add fail: {type.FullName} {sameHashType.FullName}", e);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取指定 <see cref="LSEntity"/> 类型的确定性长整型哈希值
        /// </summary>
        /// <param name="type">帧同步实体类型</param>
        /// <returns>基于类型全名计算的 64 位哈希值</returns>
        public long GetLongHashCode(Type type)
        {
            return this.lsEntityTypeLongHashCode.GetValueByKey(type);
        }
        
        /// <summary>
        /// 获取指定类型的帧同步系统集合
        /// </summary>
        public TypeSystems.OneTypeSystems GetOneTypeSystems(Type type)
        {
            return this.TypeSystems.GetOneTypeSystems(type);
        }
        
        /// <summary>
        /// 触发目标实体的所有 <see cref="ILSRollbackSystem"/> 回滚修正回调
        /// </summary>
        /// <param name="entity">需要执行回滚修正的实体</param>
        public void LSRollback(Entity entity)
        {
            if (entity is not ILSRollback)
            {
                return;
            }
            
            List<SystemObject> iLSRollbackSystems = this.TypeSystems.GetSystems(entity.GetType(), typeof (ILSRollbackSystem));
            if (iLSRollbackSystems == null)
            {
                return;
            }

            foreach (ILSRollbackSystem iLSRollbackSystem in iLSRollbackSystems)
            {
                if (iLSRollbackSystem == null)
                {
                    continue;
                }

                try
                {
                    iLSRollbackSystem.Run(entity);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        /// <summary>
        /// 触发目标帧同步实体的所有 <see cref="ILSUpdateSystem"/> 逻辑帧更新回调
        /// </summary>
        /// <param name="entity">需要执行逻辑帧更新的帧同步实体</param>
        public void LSUpdate(LSEntity entity)
        {
            if (entity is not ILSUpdate)
            {
                return;
            }
            
            List<SystemObject> iLSUpdateSystems = TypeSystems.GetSystems(entity.GetType(), typeof (ILSUpdateSystem));
            if (iLSUpdateSystems == null)
            {
                return;
            }

            foreach (ILSUpdateSystem iLSUpdateSystem in iLSUpdateSystems)
            {
                try
                {
                    iLSUpdateSystem.Run(entity);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
    }
}