namespace ET
{
    /// <summary>
    /// 跨程序集静态方法调用抽象包装基类（通常用于 Loader 层调用热更新程序集入口方法）。
    /// </summary>
    public abstract class IStaticMethod
    {
        /// <summary>
        /// 调用无参静态方法。
        /// </summary>
        public abstract void Run();
        /// <summary>
        /// 调用含 1 个参数的静态方法。
        /// </summary>
        public abstract void Run(object a);
        /// <summary>
        /// 调用含 2 个参数的静态方法。
        /// </summary>
        public abstract void Run(object a, object b);
        /// <summary>
        /// 调用含 3 个参数的静态方法。
        /// </summary>
        public abstract void Run(object a, object b, object c);
    }
}