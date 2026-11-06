using System.Diagnostics;

namespace ET
{
    /// <summary>
    /// 供 IDE 调试器使用的 <see cref="ChildrenCollection"/> 视图代理类，方便在调试窗口直接展开查看子实体数组。
    /// </summary>
    internal sealed class ChildrenCollectionDebugView    
    {
        private readonly ChildrenCollection _collection;

        public ChildrenCollectionDebugView(ChildrenCollection collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            _collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Entity[] Items
        {
            get
            {
                Entity[] items = new Entity[_collection.Count];
                _collection.Values.CopyTo(items, 0);
                return items;
            }
        }
    }
    
    /// <summary>
    /// 实体的子实体（Children）有序字典集合（Key 为子实体的 <see cref="Entity.Id"/>，Value 为子实体 <see cref="Entity"/>）。
    /// 支持对象池（<see cref="IPool"/>）复用，且采用 <see cref="SortedDictionary{TKey,TValue}"/> 保证遍历顺序的确定性（如帧同步或序列化场景）。
    /// </summary>
    [DebuggerTypeProxy(typeof(ChildrenCollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    public class ChildrenCollection : SortedDictionary<long, Entity>, IPool
    {
        /// <summary>
        /// 从对象池获取或新建一个 <see cref="ChildrenCollection"/> 实例。
        /// </summary>
        public static ChildrenCollection Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ChildrenCollection>(isFromPool);
        }
        
        public bool IsFromPool { get; set; }

        void IPool.Clear()
        {
            base.Clear();
        }
        
        /// <summary>
        /// 清空并将当前集合回收到 <see cref="ObjectPool"/> 对象池。
        /// </summary>
        public void Dispose()
        {
            ObjectPool.Recycle(this);
        }
    }
}
