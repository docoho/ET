using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 帧同步匹配服务核心组件（挂载在 Match 服根 <see cref="Scene"/> 上），维护正在排队等待匹配的玩家列表
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MatchComponent: Entity, IAwake
    {
        /// <summary>
        /// 当前等待匹配的玩家 ID 列表
        /// </summary>
        public List<long> waitMatchPlayers = new List<long>();
    }

}