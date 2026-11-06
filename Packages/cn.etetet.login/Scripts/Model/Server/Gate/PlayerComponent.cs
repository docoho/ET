using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
	/// <summary>
	/// 网关玩家管理组件：挂载在 Gate Scene 上，维护账号名到 Player 实体的映射。
	/// </summary>
	[ComponentOf(typeof(Scene))]
	public class PlayerComponent : Entity, IAwake, IDestroy
	{
		/// <summary>账号名称 -> Player 实体引用的字典</summary>
		public Dictionary<string, EntityRef<Player>> dictionary = new Dictionary<string, EntityRef<Player>>();
	}
}