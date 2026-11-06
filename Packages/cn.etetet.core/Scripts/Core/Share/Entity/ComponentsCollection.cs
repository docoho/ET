using System.Diagnostics;

namespace ET
{
    /// <summary>
    /// 供 IDE 调试器使用的 <see cref="ComponentsCollection"/> 视图代理类，方便在调试窗口直接展开查看挂载的组件列表。
    /// </summary>
    internal sealed class ComponentsCollectionDebugView    
    {
        private readonly ComponentsCollection _collection;

        public ComponentsCollectionDebugView(ComponentsCollection collection)
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
    /// 实体的组件（Components）有序字典集合（Key 为组件类型的 64 位哈希值 <see cref="Entity.GetLongHashCode"/>，Value 为组件 <see cref="Entity"/>）。
    /// 支持对象池（<see cref="IPool"/>）复用，避免频繁挂载/卸载组件时产生字典分配的 GC。
    /// </summary>
    [DebuggerTypeProxy(typeof(ComponentsCollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    public class ComponentsCollection : SortedDictionary<long, Entity>, IPool
    {
        /// <summary>
        /// 从对象池获取或新建一个 <see cref="ComponentsCollection"/> 实例。
        /// </summary>
        public static ComponentsCollection Create(bool isFromPool = false)
        {
            return ObjectPool.Fetch<ComponentsCollection>(isFromPool);
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
