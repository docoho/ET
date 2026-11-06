namespace ET.Server
{
	/// <summary>
	/// 会话关联玩家组件：挂载在 Gate 的外网 Session 上，用于在收到客户端上行消息或连接断开时找到对应的 Player 实体。
	/// </summary>
	[ComponentOf(typeof(Session))]
	public class SessionPlayerComponent : Entity, IAwake, IDestroy
	{
		private EntityRef<Player> player;

		/// <summary>当前 Session 绑定的网关 Player 实体</summary>
		public Player Player
		{
			get
			{
				return this.player;
			}
			set
			{
				this.player = value;
			}
		}
	}
}