namespace ET.Client
{
	/// <summary>
	/// 客户端网络纤程（NetClient）持有网关长连接 Session 的组件。
	/// </summary>
	[ComponentOf(typeof(Scene))]
	public class SessionComponent: Entity, IAwake, IDestroy
	{
		private EntityRef<Session> session;

		/// <summary>当前与网关（Gate）保持连接的 Session 实例</summary>
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
