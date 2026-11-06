namespace ET.Server
{
    /// <summary>
    /// 网关玩家待登出倒计时组件：客户端断线时挂载在 Player 上，若在倒计时结束前重连则移除该组件，否则触发正式下线清理。
    /// </summary>
    [ComponentOf(typeof(Player))]
    public class WaitLogoutComponent: Entity, IAwake
    {
        
    }
}