namespace ET.Server
{
    /// <summary>
    /// 网关 Session 关联 Player 的组件系统：当客户端连接断开导致 Session 销毁时，为 Player 挂载 WaitLogoutComponent 触发延迟登出。
    /// </summary>
    [EntitySystemOf(typeof(SessionPlayerComponent))]
    public static partial class SessionPlayerComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this SessionPlayerComponent self)
        {
            Scene root = self.Root();
            if (root== null)
            {
                return;
            }

            Player player = self.Player;
            if (player == null)
            {
                return;
            }

            // 客户端断线时先进入待登出宽限期，若宽限期内未重连则正式登出清理
            player.AddComponent<WaitLogoutComponent>();
        }
        
        [EntitySystem]
        private static void Awake(this SessionPlayerComponent self)
        {

        }
    }
}