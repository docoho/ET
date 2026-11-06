using System;


namespace ET.Server
{
	/// <summary>
	/// 匹配服通知网关玩家匹配成功（<see cref="Match2G_NotifyMatchSuccess"/>）的 Actor 消息处理器
	/// </summary>
	[MessageHandler(SceneType.Gate)]
	public class Match2G_NotifyMatchSuccessHandler : MessageHandler<Player, Match2G_NotifyMatchSuccess>
	{
		/// <summary>
		/// 在网关 <see cref="Player"/> 实体上挂载 <see cref="PlayerRoomComponent"/> 记录所属房间 ActorId，并将匹配成功消息转发给客户端
		/// </summary>
		/// <param name="player">网关玩家实体</param>
		/// <param name="message">匹配成功通知消息（含房间 ActorId）</param>
		protected override async ETTask Run(Player player, Match2G_NotifyMatchSuccess message)
		{
			player.AddComponent<PlayerRoomComponent>().RoomActorId = message.ActorId;
			
			player.GetComponent<PlayerSessionComponent>().Session.Send(message);
			await ETTask.CompletedTask;
		}
	}
}