using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 客户端单位动画组件：持有 Unity Animator 引用、动画片段字典及待播放的动作状态。
	/// </summary>
	[ComponentOf]
	public class AnimatorComponent : Entity, IAwake, IUpdate, IDestroy
	{
		/// <summary>动画片段名称 -> AnimationClip 字典</summary>
		public Dictionary<string, AnimationClip> animationClips = new();
		/// <summary>Animator Controller 中定义的所有参数名称集合</summary>
		public HashSet<string> Parameter = new();

		/// <summary>下一帧待触发播放的动作类型</summary>
		public MotionType MotionType;
		/// <summary>下一帧触发动作时的播放倍速</summary>
		public float MontionSpeed;
		/// <summary>动画是否处于暂停状态</summary>
		public bool isStop;
		/// <summary>暂停前备份的 Animator 播放倍速</summary>
		public float stopSpeed;
		/// <summary>Unity Animator 组件引用</summary>
		public Animator Animator;
	}
}