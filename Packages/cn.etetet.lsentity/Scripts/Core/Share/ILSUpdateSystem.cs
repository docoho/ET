using System;

namespace ET
{
    /// <summary>
    /// 帧同步逻辑帧更新标记接口，实现该接口的 <see cref="LSEntity"/> 会在每个逻辑帧按确定性顺序触发更新
    /// </summary>
    public interface ILSUpdate
    {
    }
    
    /// <summary>
    /// 帧同步逻辑帧更新系统内部调度接口
    /// </summary>
    public interface ILSUpdateSystem: ISystemType, IEventSystem
    {
        /// <summary>
        /// 执行指定帧同步实体的逻辑帧更新
        /// </summary>
        /// <param name="o">帧同步实体</param>
        void Run(LSEntity o);
    }

    /// <summary>
    /// 帧同步逻辑帧更新系统抽象基类，由 <see cref="LSUpdater"/> 在每个逻辑帧按实体 ID 升序确定性调用
    /// </summary>
    /// <typeparam name="T">实现 <see cref="ILSUpdate"/> 的帧同步实体类型</typeparam>
    [LSEntitySystem]
    public abstract class LSUpdateSystem<T>: SystemObject, ILSUpdateSystem where T: LSEntity, ILSUpdate
    {
        void ILSUpdateSystem.Run(LSEntity o)
        {
            this.LSUpdate((T)o);
        }

        Type ISystemType.Type()
        {
            return typeof(T);
        }

        Type ISystemType.SystemType()
        {
            return typeof(ILSUpdateSystem);
        }

        /// <summary>
        /// 子类实现的单帧确定性逻辑更新方法
        /// </summary>
        /// <param name="self">帧同步实体实例</param>
        protected abstract void LSUpdate(T self);
    }
}