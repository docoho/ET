using System;


namespace ET.Server
{
    /// <summary>
    /// 客户端使用 Realm 下发的 Key 登录网关（C2G_LoginGate）的消息处理器。
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_LoginGateHandler : MessageSessionHandler<C2G_LoginGate, G2C_LoginGate>
    {
        protected override async ETTask Run(Session session, C2G_LoginGate request, G2C_LoginGate response)
        {
            Scene root = session.Root();
            // 校验 Realm 在 Gate 上预注册的登录凭据 Key
            string account = root.GetComponent<GateSessionKeyComponent>().Get(request.Key);
            if (account == null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                response.Message = "Gate key验证失败!";
                return;
            }
            
            // 登录验证通过，移除未认证连接的超时断开组件
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            Player player = playerComponent.GetByAccount(account);
            EntityRef<Session> sessionRef = session;
            
            if (player == null)
            {
                // 首次登录：创建 Player 实体并挂载网关会话邮箱（MailBoxType.GateSession）以支持内网直接向客户端推送消息
                player = playerComponent.AddChild<Player, string>(account);
                EntityRef<Player> playerRef = player;
                playerComponent.Add(player);
                PlayerSessionComponent playerSessionComponent = player.AddComponent<PlayerSessionComponent>();
                playerSessionComponent.AddComponent<MailBoxComponent, int>(MailBoxType.GateSession);
                
                player = playerRef;
                session = sessionRef;
                session.AddComponent<SessionPlayerComponent>().Player = player;
                playerSessionComponent.Session = session;
            }
            else
            {
                // 断线重连/顶号：取消待登出倒计时，重新绑定新 Session 与已有 Player
                player.RemoveComponent<WaitLogoutComponent>();
                session.AddComponent<SessionPlayerComponent>().Player = player;
                player.GetComponent<PlayerSessionComponent>().Session = session;
            }

            response.PlayerId = player.Id;
            await ETTask.CompletedTask;
        }
    }
}