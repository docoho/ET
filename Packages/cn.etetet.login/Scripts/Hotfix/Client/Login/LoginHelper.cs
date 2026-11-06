namespace ET.Client
{
    /// <summary>
    /// 客户端登录流程辅助入口类。
    /// </summary>
    public static class LoginHelper
    {
        /// <summary>
        /// 重建 ClientSenderComponent 并发起账号登录，保存返回的 PlayerId 并发布 LoginFinish 事件。
        /// </summary>
        public static async ETTask Login(Scene root, string address, string account, string password)
        {
            root.RemoveComponent<ClientSenderComponent>();
            
            ClientSenderComponent clientSenderComponent = root.AddComponent<ClientSenderComponent>();
            
            EntityRef<Scene> rootRef = root;
            long playerId = await clientSenderComponent.LoginAsync(address, account, password);

            root = rootRef;
            root.GetComponent<PlayerComponent>().MyId = playerId;
            
            await EventSystem.Instance.PublishAsync(root, new LoginFinish());
        }
    }
}
