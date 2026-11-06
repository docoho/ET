namespace ET.Server
{
    /// <summary>
    /// 帧同步房间管理器组件（挂载在 Map 服根 <see cref="Scene"/> 上）
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class RoomManagerComponent: Entity, IAwake
    {

    }
}