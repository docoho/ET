namespace ET.Server
{
	/// <summary>
	/// 客户端向网关发送开始帧同步匹配请求（<see cref="C2G_Match"/>）的会话处理器
	/// </summary>
	[MessageSessionHandler(SceneType.Gate)]
	public class C2G_MatchHandler : MessageSessionHandler<C2G_Match, G2C_Match>
	{
		/// <summary>
		/// 获取当前会话关联的 <see cref="Player"/> 并向本区匹配服发送 <see cref="G2Match_Match"/> 请求
		/// </summary>
		/// <param name="session">客户端网关会话</param>
		/// <param name="request">客户端匹配请求</param>
		/// <param name="response">网关匹配响应</param>
		protected override async ETTask Run(Session session, C2G_Match request, G2C_Match response)
		{
			Player player = session.GetComponent<SessionPlayerComponent>().Player;
            Fiber fiber = session.Fiber();

			StartSceneConfig startSceneConfig = fiber.GetSingleton<StartSceneConfigCategory>().GetOneBySceneType(session.Zone(), SceneType.Match);

			G2Match_Match g2MatchMatch = G2Match_Match.Create();
			g2MatchMatch.Id = player.Id;
			await session.Root().GetComponent<MessageSender>().Call(startSceneConfig.GetActorId(fiber), g2MatchMatch);
		}
	}
}
