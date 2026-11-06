namespace ET
{
    /// <summary>
    /// 实体序列化格式化器静态注册入口（由 Roslyn SourceGenerator 在编译期自动实现分部方法 <see cref="Register"/>）。
    /// </summary>
    public static partial class EntitySerializeRegister
    {
        /// <summary>
        /// 由源码生成器自动生成实现，注册当前程序集内所有 <see cref="Entity"/> 子类的 MemoryPack / BSON 序列化映射。
        /// </summary>
        static partial void Register();

        /// <summary>
        /// 执行实体序列化注册。
        /// </summary>
        public static void Init()
        {
            Register();
        }
    }
}

