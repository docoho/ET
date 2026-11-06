using MemoryPack;

namespace ET
{
    /// <summary>
    /// MemoryPack 自定义格式化器注册入口。
    /// </summary>
    public static class MemoryPackRegister
    {
        /// <summary>
        /// 注册 ET 核心集合（<see cref="ChildrenCollection"/> 与 <see cref="ComponentsCollection"/>）的专属 MemoryPack 格式化器。
        /// </summary>
        public static void Init()
        {
            MemoryPackFormatterProvider.Register(new MemoryPackChildrenCollectionFormatter());
            MemoryPackFormatterProvider.Register(new MemoryPackComponentsCollectionFormatter());
        }
    }
}