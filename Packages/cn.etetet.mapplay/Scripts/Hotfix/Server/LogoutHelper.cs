namespace ET.Server
{
    /// <summary>
    /// 服务端玩家登出辅助工具类。
    /// </summary>
    public static class LogoutHelper
    {
        /// <summary>
        /// 从网关 PlayerComponent 移除玩家实体，并通过 ActorLocation 通知玩家当前所在的地图服销毁对应的 Unit。
        /// </summary>
        public static async ETTask Logout(Player player)
        {
            long playerId = player.Id;
            
            Scene root = player.Root();
            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            
            playerComponent.Remove(player);
            
            // 从地图中移除玩家
            G2Map_Logout g2MapLogout = G2Map_Logout.Create();
            await root.GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit).Call(playerId, g2MapLogout);
        }
    }
}