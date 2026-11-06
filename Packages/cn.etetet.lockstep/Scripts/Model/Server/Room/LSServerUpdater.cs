using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 帧同步服务端房间逻辑帧更新器组件（挂载在服务端 <see cref="Room"/> 上）
    /// </summary>
    [ComponentOf(typeof(Room))]
    public class LSServerUpdater: Entity, IAwake, IUpdate
    {
    }
}