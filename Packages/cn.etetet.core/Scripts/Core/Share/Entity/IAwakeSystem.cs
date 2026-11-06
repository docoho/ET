using System;

namespace ET
{
    /// <summary>
    /// 实体无参初始化标记接口。
    /// 在 Model 层的 Entity/Component 类上实现此接口后，即可使用 <c>AddComponent&lt;T&gt;()</c> 或 <c>AddChild&lt;T&gt;()</c> 创建并自动触发 Hotfix 层的 <c>Awake(this T self)</c> 方法。
    /// </summary>
    public interface IAwake
    {
    }

    /// <summary>
    /// 带 1 个初始化参数的实体标记接口（配合 <c>AddComponent&lt;T, A&gt;(a)</c> 或 <c>AddChild&lt;T, A&gt;(a)</c> 使用）。
    /// </summary>
    public interface IAwake<A>
    {
    }
	
    /// <summary>
    /// 带 2 个初始化参数的实体标记接口（配合 <c>AddComponent&lt;T, A, B&gt;(a, b)</c> 或 <c>AddChild&lt;T, A, B&gt;(a, b)</c> 使用）。
    /// </summary>
    public interface IAwake<A, B>
    {
    }
	
    /// <summary>
    /// 带 3 个初始化参数的实体标记接口（配合 <c>AddComponent&lt;T, A, B, C&gt;(a, b, c)</c> 或 <c>AddChild&lt;T, A, B, C&gt;(a, b, c)</c> 使用）。
    /// </summary>
    public interface IAwake<A, B, C>
    {
    }
	
    /// <summary>
    /// 带 4 个初始化参数的实体标记接口。
    /// </summary>
    public interface IAwake<A, B, C, D>
    {
    }
    
    /// <summary>
    /// 无参 Awake 系统内部调度接口（由 <see cref="EntitySystemSingleton.Awake(Entity)"/> 调用）。
    /// </summary>
    public interface IAwakeSystem: ISystemType
    {
        void Run(Entity o);
    }
	
    /// <summary>
    /// 单参数 Awake 系统内部调度接口。
    /// </summary>
    public interface IAwakeSystem<A>: ISystemType
    {
        void Run(Entity o, A a);
    }
	
    /// <summary>
    /// 双参数 Awake 系统内部调度接口。
    /// </summary>
    public interface IAwakeSystem<A, B>: ISystemType
    {
        void Run(Entity o, A a, B b);
    }
	
    /// <summary>
    /// 三参数 Awake 系统内部调度接口。
    /// </summary>
    public interface IAwakeSystem<A, B, C>: ISystemType
    {
        void Run(Entity o, A a, B b, C c);
    }
	
    /// <summary>
    /// 四参数 Awake 系统内部调度接口。
    /// </summary>
    public interface IAwakeSystem<A, B, C, D>: ISystemType
    {
        void Run(Entity o, A a, B b, C c, D d);
    }

    /// <summary>
    /// 无参 Awake 系统抽象基类（通常由 SourceGenerator 根据 <c>[EntitySystem]</c> 静态方法自动生成派生类）。
    /// </summary>
    [EntitySystem]
    public abstract class AwakeSystem<T> : SystemObject, IAwakeSystem where T: Entity, IAwake
    {
        Type ISystemType.Type()
        {
            return typeof(T);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IAwakeSystem);
        }

        void IAwakeSystem.Run(Entity o)
        {
            this.Awake((T)o);
        }

        protected abstract void Awake(T self);
    }
    
    /// <summary>
    /// 单参数 Awake 系统抽象基类（由 SourceGenerator 自动生成派生类）。
    /// </summary>
    [EntitySystem]
    public abstract class AwakeSystem<T, A> : SystemObject, IAwakeSystem<A> where T: Entity, IAwake<A>
    {
        Type ISystemType.Type()
        {
            return typeof(T);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IAwakeSystem<A>);
        }

        void IAwakeSystem<A>.Run(Entity o, A a)
        {
            this.Awake((T)o, a);
        }

        protected abstract void Awake(T self, A a);
    }

    /// <summary>
    /// 双参数 Awake 系统抽象基类（由 SourceGenerator 自动生成派生类）。
    /// </summary>
    [EntitySystem]
    public abstract class AwakeSystem<T, A, B> : SystemObject, IAwakeSystem<A, B> where T: Entity, IAwake<A, B>
    {
        Type ISystemType.Type()
        {
            return typeof(T);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IAwakeSystem<A, B>);
        }

        void IAwakeSystem<A, B>.Run(Entity o, A a, B b)
        {
            this.Awake((T)o, a, b);
        }

        protected abstract void Awake(T self, A a, B b);
    }

    /// <summary>
    /// 三参数 Awake 系统抽象基类（由 SourceGenerator 自动生成派生类）。
    /// </summary>
    [EntitySystem]
    public abstract class AwakeSystem<T, A, B, C> : SystemObject, IAwakeSystem<A, B, C> where T: Entity, IAwake<A, B, C>
    {
        Type ISystemType.Type()
        {
            return typeof(T);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IAwakeSystem<A, B, C>);
        }

        void IAwakeSystem<A, B, C>.Run(Entity o, A a, B b, C c)
        {
            this.Awake((T)o, a, b, c);
        }

        protected abstract void Awake(T self, A a, B b, C c);
    }
    
    /// <summary>
    /// 四参数 Awake 系统抽象基类（由 SourceGenerator 自动生成派生类）。
    /// </summary>
    [EntitySystem]
    public abstract class AwakeSystem<T, A, B, C, D> : SystemObject, IAwakeSystem<A, B, C, D> where T: Entity, IAwake<A, B, C, D>
    {
        Type ISystemType.Type()
        {
            return typeof(T);
        }

        Type ISystemType.SystemType()
        {
            return typeof(IAwakeSystem<A, B, C, D>);
        }

        void IAwakeSystem<A, B, C, D>.Run(Entity o, A a, B b, C c, D d)
        {
            this.Awake((T)o, a, b, c, d);
        }

        protected abstract void Awake(T self, A a, B b, C c, D d);
    }
}