using System.Reflection;

namespace ET
{
    /// <summary>
    /// 基于反射（<see cref="MethodInfo"/>）的静态方法调用包装实现。
    /// <remarks>
    /// 在构造时预先分配好参数数组 <see cref="param"/>，避免每次调用 <c>Run</c> 时重复分配 <c>object[]</c> 数组。
    /// </remarks>
    /// </summary>
    public class StaticMethod : IStaticMethod
    {
        /// <summary>
        /// 反射获取的目标静态方法元数据。
        /// </summary>
        private readonly MethodInfo methodInfo;

        /// <summary>
        /// 预分配的参数数组缓存。
        /// </summary>
        private readonly object[] param;

        /// <summary>
        /// 从指定程序集中查找指定类型和名称的静态方法并缓存其反射信息。
        /// </summary>
        public StaticMethod(Assembly assembly, string typeName, string methodName)
        {
            this.methodInfo = assembly.GetType(typeName).GetMethod(methodName);
            this.param = new object[this.methodInfo.GetParameters().Length];
        }

        public override void Run()
        {
            this.methodInfo.Invoke(null, param);
        }

        public override void Run(object a)
        {
            this.param[0] = a;
            this.methodInfo.Invoke(null, param);
        }

        public override void Run(object a, object b)
        {
            this.param[0] = a;
            this.param[1] = b;
            this.methodInfo.Invoke(null, param);
        }

        public override void Run(object a, object b, object c)
        {
            this.param[0] = a;
            this.param[1] = b;
            this.param[2] = c;
            this.methodInfo.Invoke(null, param);
        }
    }
}

