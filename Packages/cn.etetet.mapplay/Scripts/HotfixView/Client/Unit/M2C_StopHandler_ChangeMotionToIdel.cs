using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
	/// <summary>
	/// 客户端收到服务端移动停止消息（M2C_Stop）的表现层处理器：将角色 Animator 的 MoveSpeed 参数归零以切回待机动画。
	/// </summary>
	[MessageHandler(SceneType.Current)]
	public class M2C_StopHandler_ChangeMotionToIdel : MessageHandler<Scene, M2C_Stop>
	{
		protected override async ETTask Run(Scene currentScene, M2C_Stop message)
		{
			Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.Id);
			if (unit == null)
			{
				return;
			}

			AnimatorComponent animator = unit.GetComponent<AnimatorComponent>();
			if (animator != null)
			{
				animator.SetFloat(nameof(MotionType.MoveSpeed), 0);
			}
			await ETTask.CompletedTask;
		}
	}
}
