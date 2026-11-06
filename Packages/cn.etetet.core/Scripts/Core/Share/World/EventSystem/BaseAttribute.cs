using System;

namespace ET
{
	/// <summary>
	/// 允许单例通过 <see cref="Singleton{T}.Instance"/> 静态属性直接访问的标记特性。
	/// 只有进程级全局单例（且线程安全或只读）才应标注此特性；纤程级单例禁止标注，以防跨纤程误用静态单例。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public sealed class AllowInstanceAttribute : Attribute
	{
	}

	/// <summary>
	/// ET 框架所有需要被 <see cref="CodeTypes"/> 在启动时自动反射扫描收集的特性基类（继承自 <see cref="EnableClassAttribute"/>）。
	/// 例如 <see cref="EntitySystemAttribute"/>、<see cref="EventAttribute"/>、<see cref="InvokeAttribute"/>、<see cref="MessageHandlerAttribute"/> 等均派生自此类。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	[EnableClass]
	public class BaseAttribute: EnableClassAttribute
	{
	}
}
