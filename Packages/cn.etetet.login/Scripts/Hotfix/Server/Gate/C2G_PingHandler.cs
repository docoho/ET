using System;


namespace ET.Server
{
	/// <summary>
	/// 网关处理客户端心跳与对时请求（C2G_Ping）的消息处理器。
	/// </summary>
	[MessageSessionHandler(SceneType.Gate)]
	public class C2G_PingHandler : MessageSessionHandler<C2G_Ping, G2C_Ping>
	{
		protected override async ETTask Run(Session session, C2G_Ping request, G2C_Ping response)
		{
			using C2G_Ping _ = request; // 这里用完调用Dispose可以回收到池，不调用的话GC会回收
			
			// 返回当前服务器时间戳供客户端计算 RTT 与时间差
			response.Time = session.GetSingleton<TimeInfo>().ClientNow();
			await ETTask.CompletedTask;
			
			//response会在函数返回发送完消息回收到池
		}
	}
}