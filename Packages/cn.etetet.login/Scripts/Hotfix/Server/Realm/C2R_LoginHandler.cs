using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;


namespace ET.Server
{
	/// <summary>
	/// 登录认证服（Realm）处理客户端登录请求（C2R_Login）的消息处理器：
	/// 按账号哈希分配同区（Zone）的 Gate 网关，向 Gate 申请登录 Key 后返回给客户端，并延时断开 Realm 会话。
	/// </summary>
	[MessageSessionHandler(SceneType.Realm)]
	public class C2R_LoginHandler : MessageSessionHandler<C2R_Login, R2C_Login>
	{
		protected override async ETTask Run(Session session, C2R_Login request, R2C_Login response)
		{
			EntityRef<Session> sessionRef = session;
			// 随机分配一个Gate
			Scene root = session.Root();
			EntityRef<Scene> rootRef = root;
			ulong hash = (ulong)request.Account.GetLongHashCode();
			
			ServiceDiscoveryProxy serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxy>();

			List<ServiceInfo> gates = serviceDiscoveryProxy.GetBySceneTypeAndZone(SceneType.Gate, session.Fiber().Zone);
			ServiceInfo gateServiceInfo = gates[(int)(hash % (ulong)gates.Count)];
			Log.Debug($"gate address: {gateServiceInfo.SceneName}");
			
			// 向gate请求一个key,客户端可以拿着这个key连接gate
			R2G_GetLoginKey r2GGetLoginKey = R2G_GetLoginKey.Create();
			r2GGetLoginKey.Account = request.Account;
			response.Address = gateServiceInfo.Metadata[ServiceMetaKey.InnerIPOuterPort];
			
			root = rootRef;
			MessageSender messageSender = root.GetComponent<MessageSender>();
			G2R_GetLoginKey g2RGetLoginKey = (G2R_GetLoginKey) await messageSender.Call(gateServiceInfo.ActorId, r2GGetLoginKey);
			
			response.Key = g2RGetLoginKey.Key;
			response.GateId = g2RGetLoginKey.GateId;
			session = sessionRef;
			CloseSession(session).Coroutine();
		}

		/// <summary>
		/// 延迟 1 秒关闭客户端与 Realm 的临时登录连接（确保 R2C_Login 响应已顺利发出）。
		/// </summary>
		private async ETTask CloseSession(Session session)
		{
			EntityRef<Session> sessionRef = session;
			await session.Root().TimerComponent.WaitAsync(1000);
			session = sessionRef;
			session?.Dispose();
		}
	}
}
