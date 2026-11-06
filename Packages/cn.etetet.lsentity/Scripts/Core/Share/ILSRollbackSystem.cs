using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 帧同步回滚标记接口，实现该接口的常规表现层或实体组件在发生预测回滚时可收到 <see cref="LSRollbackSystem{T}"/> 通知
    /// </summary>
    public interface ILSRollback
    {
    }
    
    /// <summary>
    /// 帧同步回滚系统内部调度接口
    /// </summary>
    public interface ILSRollbackSystem: ISystemType
    {
        /// <summary>
        /// 执行目标实体的回滚同步逻辑
        /// </summary>
        /// <param name="o">目标实体</param>
        void Run(Entity o);
    }
    
    /// <summary>
    /// 帧同步回滚系统抽象基类，当客户端预测帧与权威帧不一致触发世界回滚重演后，用于修正表现层或关联实体状态
    /// </summary>
    /// <typeparam name="T">实现 <see cref="ILSRollback"/> 的实体类型</typeparam>
    [LSEntitySystem]
    public abstract class LSRollbackSystem<T>: SystemObject, ILSRollbackSystem where T: Entity, ILSRollback
    {
        void ILSRollbackSystem.Run(Entity o)
        {
            this.LSRollback((T)o);
        }

        Type ISystemType.Type()
        {
            return typeof(T);
        }

        Type ISystemType.SystemType()
        {
            return typeof(ILSRollbackSystem);
        }

        /// <summary>
        /// 子类实现的具体回滚修正逻辑
        /// </summary>
        /// <param name="self">目标实体实例</param>
        protected abstract void LSRollback(T self);
    }
}