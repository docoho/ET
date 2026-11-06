using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 按协程锁类型（<c>CoroutineLockType</c>）分类的中间层容器实体（其 <see cref="Entity.Id"/> 即为 <c>coroutineLockType</c>）。
    /// 它的 Children 子实体集合中按业务 <c>key</c> 挂载着各个 <see cref="CoroutineLockQueue"/>。
    /// </summary>
    [ChildOf(typeof(CoroutineLockComponent))]
    public class CoroutineLockQueueType: Entity, IAwake
    {
    }
}