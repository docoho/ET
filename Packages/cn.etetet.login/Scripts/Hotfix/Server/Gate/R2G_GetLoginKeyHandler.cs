using System;


namespace ET.Server
{
	/// <summary>
	/// 网关处理 Realm 请求生成登录凭据 Key（R2G_GetLoginKey）的 RPC 处理器。
	/// </summary>
	[MessageHandler(SceneType.Gate)]
	public class R2G_GetLoginKeyHandler : MessageHandler<Scene, R2G_GetLoginKey, G2R_GetLoginKey>
	{
		protected override async ETTask Run(Scene scene, R2G_GetLoginKey request, G2R_GetLoginKey response)
		{
			long key = RandomGenerator.RandInt64();
			scene.GetComponent<GateSessionKeyComponent>().Add(key, request.Account);
			response.Key = key;
			response.GateId = scene.Id;
			await ETTask.CompletedTask;
		}
	}
}