namespace ET.Client
{
    /// <summary>
    /// 机器人客户端纤程接收自动登录指令（<see cref="Robot_LoginRequest"/>）的处理器
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class Robot_LoginRequestHandler: MessageHandler<Scene, Robot_LoginRequest, Robot_LoginResponse>
    {
        /// <summary>
        /// 依次执行机器人账号登录（<see cref="LoginHelper.Login"/>）与请求进入地图（<see cref="EnterMapHelper.EnterMapAsync"/>）
        /// </summary>
        protected override async ETTask Run(Scene root, Robot_LoginRequest request, Robot_LoginResponse response)
        {
            EntityRef<Scene> rootRef = root;
            await LoginHelper.Login(root, request.Address, request.Account, request.Password);
            root = rootRef;
            await EnterMapHelper.EnterMapAsync(root);
        }
    }
}
