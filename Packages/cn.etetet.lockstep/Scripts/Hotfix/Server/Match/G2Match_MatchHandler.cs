using System;


namespace ET.Server
{
	/// <summary>
	/// 网关转发的玩家请求匹配（<see cref="G2Match_Match"/>）处理器
	/// </summary>
	[MessageHandler(SceneType.Match)]
	public class G2Match_MatchHandler : MessageHandler<Scene, G2Match_Match, Match2G_Match>
	{
		/// <summary>
		/// 将玩家加入 <see cref="MatchComponent"/> 匹配队列并触发匹配检查
		/// </summary>
		/// <param name="scene">匹配服根场景</param>
		/// <param name="request">匹配请求消息（包含玩家 ID）</param>
		/// <param name="response">匹配响应消息</param>
		protected override async ETTask Run(Scene scene, G2Match_Match request, Match2G_Match response)
		{
			MatchComponent matchComponent = scene.GetComponent<MatchComponent>();
			matchComponent.Match(request.Id).Coroutine();
			await ETTask.CompletedTask;
		}
	}
}