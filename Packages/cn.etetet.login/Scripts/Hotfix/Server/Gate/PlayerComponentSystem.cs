using System.Linq;

namespace ET.Server
{
    /// <summary>
    /// 网关玩家管理组件系统：按账号（Account）管理当前网关上的所有在线/待登出 Player 实体。
    /// </summary>
    public static partial class PlayerComponentSystem
    {
        /// <summary>
        /// 将玩家注册到账号字典中。
        /// </summary>
        public static void Add(this PlayerComponent self, Player player)
        {
            self.dictionary.Add(player.Account, player);
        }
        
        /// <summary>
        /// 从账号字典中移除并销毁指定玩家实体。
        /// </summary>
        public static void Remove(this PlayerComponent self, Player player)
        {
            self.dictionary.Remove(player.Account);
            player.Dispose();
        }
        
        /// <summary>
        /// 根据账号名称查找对应的 Player 实体。
        /// </summary>
        public static Player GetByAccount(this PlayerComponent self,  string account)
        {
            self.dictionary.TryGetValue(account, out EntityRef<Player> player);
            return player;
        }
    }
}