using System;
using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 客户端角色动画控制器（Animator）封装组件系统：缓存所有 AnimationClip 与状态机参数名，提供按动作枚举播放、按指定时长缩放播放、暂停/恢复及参数设置接口。
	/// </summary>
	[EntitySystemOf(typeof(AnimatorComponent))]
	public static partial class AnimatorComponentSystem
	{
		[EntitySystem]
		private static void Destroy(this AnimatorComponent self)
		{
			self.animationClips = null;
			self.Parameter = null;
			self.Animator = null;
		}
			
		[EntitySystem]
		private static void Awake(this AnimatorComponent self)
		{
			Animator animator = self.GetParent<Unit>().GetComponent<GameObjectComponent>().GameObject.GetComponent<Animator>();

			if (animator == null)
			{
				return;
			}

			if (animator.runtimeAnimatorController == null)
			{
				return;
			}

			if (animator.runtimeAnimatorController.animationClips == null)
			{
				return;
			}
			self.Animator = animator;
			foreach (AnimationClip animationClip in animator.runtimeAnimatorController.animationClips)
			{
				self.animationClips[animationClip.name] = animationClip;
			}
			foreach (AnimatorControllerParameter animatorControllerParameter in animator.parameters)
			{
				self.Parameter.Add(animatorControllerParameter.name);
			}
		}
		
		[EntitySystem]
		private static void Update(this AnimatorComponent self)
		{
			if (self.isStop)
			{
				return;
			}

			if (self.MotionType == MotionType.None)
			{
				return;
			}

			try
			{
				self.Animator.SetFloat("MotionSpeed", self.MontionSpeed);

				self.Animator.SetTrigger(self.MotionType.ToString());

				self.MontionSpeed = 1;
				self.MotionType = MotionType.None;
			}
			catch (Exception ex)
			{
				throw new Exception($"动作播放失败: {self.MotionType}", ex);
			}
		}

		/// <summary>
		/// 检查 Animator 状态机中是否存在指定名称的参数。
		/// </summary>
		public static bool HasParameter(this AnimatorComponent self, string parameter)
		{
			return self.Parameter.Contains(parameter);
		}

		/// <summary>
		/// 在指定的持续时间（time 秒）内完整播放完指定动作（自动计算并设置 MotionSpeed 倍速）。
		/// </summary>
		public static void PlayInTime(this AnimatorComponent self, MotionType motionType, float time)
		{
			AnimationClip animationClip;
			if (!self.animationClips.TryGetValue(motionType.ToString(), out animationClip))
			{
				throw new Exception($"找不到该动作: {motionType}");
			}

			float motionSpeed = animationClip.length / time;
			if (motionSpeed < 0.01f || motionSpeed > 1000f)
			{
				Log.Error($"motionSpeed数值异常, {motionSpeed}, 此动作跳过");
				return;
			}
			self.MotionType = motionType;
			self.MontionSpeed = motionSpeed;
		}

		/// <summary>
		/// 按指定倍速播放对应动作类型的动画。
		/// </summary>
		public static void Play(this AnimatorComponent self, MotionType motionType, float motionSpeed = 1f)
		{
			if (!self.HasParameter(motionType.ToString()))
			{
				return;
			}
			self.MotionType = motionType;
			self.MontionSpeed = motionSpeed;
		}

		/// <summary>
		/// 获取指定动作类型对应的 AnimationClip 原始时长（秒）。
		/// </summary>
		public static float AnimationTime(this AnimatorComponent self, MotionType motionType)
		{
			AnimationClip animationClip;
			if (!self.animationClips.TryGetValue(motionType.ToString(), out animationClip))
			{
				throw new Exception($"找不到该动作: {motionType}");
			}
			return animationClip.length;
		}

		/// <summary>
		/// 暂停当前 Animator 播放（保存当前倍速并将 speed 置为 0）。
		/// </summary>
		public static void PauseAnimator(this AnimatorComponent self)
		{
			if (self.isStop)
			{
				return;
			}
			self.isStop = true;

			if (self.Animator == null)
			{
				return;
			}
			self.stopSpeed = self.Animator.speed;
			self.Animator.speed = 0;
		}

		/// <summary>
		/// 恢复已暂停的 Animator 播放。
		/// </summary>
		public static void RunAnimator(this AnimatorComponent self)
		{
			if (!self.isStop)
			{
				return;
			}

			self.isStop = false;

			if (self.Animator == null)
			{
				return;
			}
			self.Animator.speed = self.stopSpeed;
		}

		/// <summary>
		/// 安全设置 Animator 的 bool 参数（若参数不存在则忽略）。
		/// </summary>
		public static void SetBool(this AnimatorComponent self, string name, bool state)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetBool(name, state);
		}

		/// <summary>
		/// 安全设置 Animator 的 float 参数（若参数不存在则忽略）。
		/// </summary>
		public static void SetFloat(this AnimatorComponent self, string name, float state)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetFloat(name, state);
		}

		/// <summary>
		/// 安全设置 Animator 的 int 参数（若参数不存在则忽略）。
		/// </summary>
		public static void SetInt(this AnimatorComponent self, string name, int value)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetInteger(name, value);
		}

		/// <summary>
		/// 安全触发 Animator 的 Trigger 参数（若参数不存在则忽略）。
		/// </summary>
		public static void SetTrigger(this AnimatorComponent self, string name)
		{
			if (!self.HasParameter(name))
			{
				return;
			}
			self.Animator.SetTrigger(name);
		}

		/// <summary>
		/// 修改 Animator 全局播放倍速并备份原倍速。
		/// </summary>
		public static void SetAnimatorSpeed(this AnimatorComponent self, float speed)
		{
			self.stopSpeed = self.Animator.speed;
			self.Animator.speed = speed;
		}

		/// <summary>
		/// 将 Animator 全局播放倍速恢复为上一次备份的倍速。
		/// </summary>
		public static void ResetAnimatorSpeed(this AnimatorComponent self)
		{
			self.Animator.speed = self.stopSpeed;
		}
	}
}