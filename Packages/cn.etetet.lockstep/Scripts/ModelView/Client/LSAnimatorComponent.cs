using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 帧同步角色动作类型枚举
	/// </summary>
	public enum MotionType
	{
		/// <summary>
		/// 无动作
		/// </summary>
		None,

		/// <summary>
		/// 待机动作
		/// </summary>
		Idle,

		/// <summary>
		/// 奔跑动作
		/// </summary>
		Run,
	}
	
	/// <summary>
	/// 帧同步单位视图动画控制器组件，封装对 Unity <see cref="UnityEngine.Animator"/> 的参数缓存与播放状态管理
	/// </summary>
	[ComponentOf]
	public class LSAnimatorComponent : Entity, IAwake, IUpdate, IDestroy
	{
		/// <summary>
		/// 动画片段名称到 <see cref="AnimationClip"/> 的映射字典
		/// </summary>
		public Dictionary<string, AnimationClip> animationClips = new();

		/// <summary>
		/// Animator 控制器中包含的所有参数名称集合
		/// </summary>
		public HashSet<string> Parameter = new();

		/// <summary>
		/// 当前待触发的动作类型
		/// </summary>
		public MotionType MotionType;

		/// <summary>
		/// 动作播放速度倍率
		/// </summary>
		public float MontionSpeed;

		/// <summary>
		/// 动画是否处于暂停状态
		/// </summary>
		public bool isStop;

		/// <summary>
		/// 暂停前记录的动画播放速度
		/// </summary>
		public float stopSpeed;

		/// <summary>
		/// 关联的 Unity <see cref="UnityEngine.Animator"/> 组件引用
		/// </summary>
		public Animator Animator;
	}
}