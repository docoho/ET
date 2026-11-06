using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 客户端单位停止移动事件（MoveStop）的表现层处理器：将角色 Animator 的 MoveSpeed 参数置为 0。
	/// </summary>
	[Event(SceneType.Current)]
	public class MoveStop_SetAnimatorMoveSpeed : AEvent<Scene, MoveStop>
	{
		protected override async ETTask Run(Scene scene, MoveStop a)
		{
			Unit unit = a.Unit;

			// 动作
			AnimatorComponent animator = unit.GetComponent<AnimatorComponent>();
			if (animator != null)
			{
				animator.SetFloat(MotionType.MoveSpeed.ToString(), 0);
			}

			await ETTask.CompletedTask;
		}
	}
}
