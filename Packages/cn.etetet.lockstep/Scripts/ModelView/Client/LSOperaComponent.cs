namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端玩家操作输入采样组件（挂载在 <see cref="Room"/> 上）
    /// </summary>
    [ComponentOf(typeof(Room))]
    public class LSOperaComponent: Entity, IAwake, IUpdate
    {
    }
}