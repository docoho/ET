using System;

namespace ET
{
    /// <summary>
    /// 帧同步实体系统标记特性，用于标记 <see cref="LSUpdateSystem{T}"/>、<see cref="LSRollbackSystem{T}"/> 或帧同步实体静态扩展方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class LSEntitySystemAttribute: BaseAttribute
    {
    }
}