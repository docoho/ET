using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务端 <see cref="Unit"/> 辅助扩展方法。
    /// </summary>
    public static partial class UnitHelper
    {
        /// <summary>
        /// 获取当前能看见该 <paramref name="self"/> 单位的所有玩家 <see cref="AOIEntity"/> 字典（主要用于 AOI 消息广播）。
        /// </summary>
        // 获取看见unit的玩家，主要用于广播
        public static Dictionary<long, EntityRef<AOIEntity>> GetBeSeePlayers(this Unit self)
        {
            return self.GetComponent<AOIEntity>().GetBeSeePlayers();
        }
    }
}
