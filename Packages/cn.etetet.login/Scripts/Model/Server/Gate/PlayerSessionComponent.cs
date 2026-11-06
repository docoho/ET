namespace ET.Server
{
	/// <summary>
	/// 玩家会话组件：挂载在 Gate 的 Player 实体上，持有与客户端通信的 Session 引用，并挂载 GateSession 邮箱供内网 Actor 直接向客户端发包。
	/// </summary>
	[ComponentOf(typeof(Player))]
	public class PlayerSessionComponent : Entity, IAwake
	{
		private EntityRef<Session> session;

		/// <summary>当前绑定的客户端外网连接 Session</summary>
		public Session Session
		{
			get
			{
				return this.session;
			}
			set
			{
				this.session = value;
			}
		}
	}
}