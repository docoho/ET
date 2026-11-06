using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 客户端单位开始移动事件（MoveStart）的表现层处理器：将单位当前移动速度设置到 Animator 的 MoveSpeed 参数以播放跑步/行走动画。
	/// </summary>
	[Event(SceneType.Current)]
	public class MoveStart_SetAnimatorMoveSpeed : AEvent<Scene, MoveStart>
	{
		protected override async ETTask Run(Scene scene, MoveStart a)
		{
			Unit unit = a.Unit;

			float speed = unit.NumericComponent.GetAsFloat(NumericType.Speed);
			
			// 动作
			AnimatorComponent animator = unit.GetComponent<AnimatorComponent>();
			if (animator != null)
			{
				animator.SetFloat(MotionType.MoveSpeed.ToString(), speed);
			}

			await ETTask.CompletedTask;
		}
	}
}
