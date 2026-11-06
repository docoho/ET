using System;

namespace ET
{
	/// <summary>
	/// ECS 实体系统标记特性。
	/// <para>
	/// 【初学者指南】
	/// 当我们在 Hotfix 层编写实体的生命周期方法（如 <c>Awake</c>、<c>Update</c>、<c>Destroy</c>）时，
	/// 只需要在静态方法上标注 <c>[EntitySystem]</c>，并把静态类声明为 <c>[EntitySystemOf(typeof(XXXComponent))]</c>，
	/// ET 的 SourceGenerator 就会在编译期自动生成对应的 <see cref="SystemObject"/> 派生类并打上此 <c>[EntitySystem]</c> 特性，
	/// 运行时由 <see cref="EntitySystemSingleton"/> 自动扫描并完成注册！
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class EntitySystemAttribute: BaseAttribute
	{
	}
}