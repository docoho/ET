namespace ET
{
    /// <summary>
    /// 单例无参初始化接口（配合 <c>AddSingleton&lt;T&gt;()</c> 使用）。
    /// </summary>
    public interface ISingletonAwake
    {
        void Awake();
    }
    
    /// <summary>
    /// 带 1 个初始化参数的单例接口（配合 <c>AddSingleton&lt;T, A&gt;(a)</c> 使用）。
    /// </summary>
    public interface ISingletonAwake<A>
    {
        void Awake(A a);
    }
    
    /// <summary>
    /// 带 2 个初始化参数的单例接口（配合 <c>AddSingleton&lt;T, A, B&gt;(a, b)</c> 使用）。
    /// </summary>
    public interface ISingletonAwake<A, B>
    {
        void Awake(A a, B b);
    }
    
    /// <summary>
    /// 带 3 个初始化参数的单例接口（配合 <c>AddSingleton&lt;T, A, B, C&gt;(a, b, c)</c> 使用）。
    /// </summary>
    public interface ISingletonAwake<A, B, C>
    {
        void Awake(A a, B b, C c);
    }
}