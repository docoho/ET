using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 帧同步房间服务端玩家管理组件（挂载在服务端 <see cref="Room"/> 上，管理所有 <see cref="RoomPlayer"/> 子实体）
    /// </summary>
    [ComponentOf(typeof(Room))]
    public class RoomServerComponent: Entity, IAwake<List<long>>
    {
    }
}