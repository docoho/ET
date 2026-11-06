using System;
using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 帧同步单位视图动画控制器组件系统，负责收集 Unity <see cref="Animator"/> 的动画片段与参数并驱动动作播放、变速与暂停恢复
	/// </summary>
	[EntitySystemOf(typeof(LSAnimatorComponent))]
	[FriendOf(typeof(LSAnimatorComponent))]
	public static partial class LSAnimatorComponentSystem
	{
		/// <summary>
		/// 组件销毁时清理对 Unity Animator 及动画片段字典的引用
		/// </summary>
		[EntitySystem]
		private static void Destroy(this LSAnimatorComponent self)
		{
			self.animationClips = null;
			self.Parameter = null;
			self.Animator = null;
		}
		
		/// <summary>
		/// 组件初始化时获取父级 <see cref="LSUnitView"/> 挂载的 <see cref="Animator"/>，缓存所有动画片段与参数名
		/// </summary>
		[EntitySystem]
		private static void Awake(this LSAnimatorComponent self)
		{
			Animator animator = self.GetParent<LSUnitView>().GameObject.GetComponent<Animator>();

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
		
		/// <summary>
		/// 每帧检测是否有待触发的动作类型（<see cref="LSAnimatorComponent.MotionType"/>），若有则设置动作速度与 Trigger
		/// </summary>
		[EntitySystem]
		private static void Update(this LSAnimatorComponent self)
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
		/// 检查 Animator 中是否存在指定名称的参数
		/// </summary>
		public static bool HasParameter(this LSAnimatorComponent self, string parameter)
		{
			return self.Parameter.Contains(parameter);
		}

		/// <summary>
		/// 在指定时长内播放完指定动作（自动根据片段原长与目标时长计算播放倍速）
		/// </summary>
		public static void PlayInTime(this LSAnimatorComponent self, MotionType motionType, float time)
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
		/// 按指定速度播放指定动作类型
		/// </summary>
		public static void Play(this LSAnimatorComponent self, MotionType motionType, float motionSpeed = 1f)
		{
			if (!self.HasParameter(motionType.ToString()))
			{
				return;
			}
			self.MotionType = motionType;
			self.MontionSpeed = motionSpeed;
		}

		/// <summary>
		/// 获取指定动作类型动画片段的原始时长（秒）
		/// </summary>
		public static float AnimationTime(this LSAnimatorComponent self, MotionType motionType)
		{
			AnimationClip animationClip;
			if (!self.animationClips.TryGetValue(motionType.ToString(), out animationClip))
			{
				throw new Exception($"找不到该动作: {motionType}");
			}
			return animationClip.length;
		}

		/// <summary>
		/// 暂停当前 Animator 播放并记录暂停前的播放倍速
		/// </summary>
		public static void PauseAnimator(this LSAnimatorComponent self)
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
		/// 恢复已暂停的 Animator 播放倍速
		/// </summary>
		public static void RunAnimator(this LSAnimatorComponent self)
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
		/// 设置 Animator 的布尔型参数值
		/// </summary>
		public static void SetBoolValue(this LSAnimatorComponent self, string name, bool state)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetBool(name, state);
		}

		/// <summary>
		/// 设置 Animator 的浮点型参数值
		/// </summary>
		public static void SetFloatValue(this LSAnimatorComponent self, string name, float state)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetFloat(name, state);
		}

		/// <summary>
		/// 设置 Animator 的整型参数值
		/// </summary>
		public static void SetIntValue(this LSAnimatorComponent self, string name, int value)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetInteger(name, value);
		}

		/// <summary>
		/// 触发 Animator 的 Trigger 参数
		/// </summary>
		public static void SetTrigger(this LSAnimatorComponent self, string name)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetTrigger(name);
		}

		/// <summary>
		/// 修改 Animator 的全局播放速度并备份原速度
		/// </summary>
		public static void SetAnimatorSpeed(this LSAnimatorComponent self, float speed)
		{
			self.stopSpeed = self.Animator.speed;
			self.Animator.speed = speed;
		}

		/// <summary>
		/// 将 Animator 的全局播放速度恢复为备份值
		/// </summary>
		public static void ResetAnimatorSpeed(this LSAnimatorComponent self)
		{
			self.Animator.speed = self.stopSpeed;
		}
	}
}