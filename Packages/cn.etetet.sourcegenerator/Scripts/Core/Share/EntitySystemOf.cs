using System;

namespace ET
{
    /// <summary>
    /// 标记 <see cref="Entity"/> 的 System 静态扩展类，用于触发 <c>ETSystemGenerator</c> 在编译期自动生成对应的 <c>IAwakeSystem</c> / <c>IUpdateSystem</c> / <c>IDestroySystem</c> 等包装类。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EntitySystemOfAttribute: EnableClassAttribute
    {
        /// <summary>该静态 System 类所服务的目标 <see cref="Entity"/> 子类类型。</summary>
        public Type type;

        /// <summary>
        /// 标记Entity的System静态类 用于自动生成System函数
        /// </summary>
        /// <param name="type">Entity类型</param>
        /// <param name="ignoreAwake">是否忽略生成AwakeSystem</param>
        public EntitySystemOfAttribute(Type type, bool ignoreAwake = false)
        {
            this.type = type;
        }
    }

    /// <summary>
    /// 标记帧同步实体（<c>LSEntity</c>）的 System 静态扩展类，用于触发 <c>ETSystemGenerator</c> 自动生成对应的帧同步生命周期 System 包装类。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LSEntitySystemOfAttribute: EnableClassAttribute
    {
        /// <summary>该静态 System 类所服务的目标 <c>LSEntity</c> 子类类型。</summary>
        public Type type;

        /// <summary>
        /// 标记LSEntity的System静态类 用于自动生成System函数
        /// </summary>
        /// <param name="type">LSEntity类型</param>
        /// <param name="ignoreAwake">是否忽略生成AwakeSystem</param>
        public LSEntitySystemOfAttribute(Type type, bool ignoreAwake = false)
        {
            this.type = type;
        }
    }
}