namespace ET.Client
{
	/// <summary>
	/// 客户端接收服务端广播的寻路路径结果（M2C_PathfindingResult）的消息处理器。
	/// </summary>
	[MessageHandler(SceneType.Current)]
	public class M2C_PathfindingResultHandler : MessageHandler<Scene, M2C_PathfindingResult>
	{
		/// <summary>
		/// 获取对应单位并按服务端下发的路径点列表与当前速度执行平滑移动。
		/// </summary>
		protected override async ETTask Run(Scene currentScene, M2C_PathfindingResult message)
		{
			Unit unit = currentScene.GetComponent<UnitComponent>().Get(message.Id);

			float speed = unit.NumericComponent.GetAsFloat(NumericType.Speed);

			await unit.GetComponent<MoveComponent>().MoveToAsync(message.Points, speed, 200);
		}
	}
}
