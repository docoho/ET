using ET.Client;

namespace ET.Server
{
    /// <summary>
    /// 控制台通知机器人登出下线请求（<see cref="Console2Robot_LogoutRequest"/>）处理器
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class Console2Robot_LogoutRequestHandler: MessageHandler<Scene, Console2Robot_LogoutRequest, Console2Robot_LogoutResponse>
    {
        /// <summary>
        /// 驱动机器人客户端向网关发送 <see cref="C2G_Logout"/> 登出请求
        /// </summary>
        protected override async ETTask Run(Scene root, Console2Robot_LogoutRequest request, Console2Robot_LogoutResponse response)
        {
            C2G_Logout c2GLogout = C2G_Logout.Create();
            G2C_Logout g2CLogout = await root.GetComponent<ClientSenderComponent>().Call(c2GLogout) as G2C_Logout;
        }
    }
}