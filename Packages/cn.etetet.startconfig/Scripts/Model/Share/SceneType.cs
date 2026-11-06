namespace ET
{
	/// <summary>
	/// 场景类型常量扩展（定义网关、登录服、软路由等基础网络接入场景类型）。
	/// </summary>
	public static partial class SceneType
	{
		/// <summary>登录认证服（Realm）场景，负责账号校验与分配 Gate 地址。</summary>
		public const int Realm = PackageType.StartConfig * 1000 + 1;
		/// <summary>网关服（Gate）场景，负责维持客户端长连接并转发客户端与后端场景之间的消息。</summary>
		public const int Gate = PackageType.StartConfig * 1000 + 2;
		/// <summary>软路由节点（Router）场景，负责客户端与 Gate 之间的连接代理与断线重连转发。</summary>
		public const int Router = PackageType.StartConfig * 1000 + 3;
		/// <summary>软路由管理器（RouterManager）场景，提供 HTTP 接口下发可用 Router 节点列表。</summary>
		public const int RouterManager = PackageType.StartConfig * 1000 + 4;
	}
}