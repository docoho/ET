namespace ET.Client
{
    /// <summary>
    /// 客户端处理服务端推送的背包容量更新通知（M2C_UpdateBagCapacity）。
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_UpdateBagCapacityHandler : MessageHandler<Scene, M2C_UpdateBagCapacity>
    {
        protected override async ETTask Run(Scene scene, M2C_UpdateBagCapacity message)
        {
            ItemComponent itemComponent = scene.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                Log.Error("ItemComponent not found in scene");
                return;
            }

            itemComponent.SetCapacity(message.Capacity);

            await ETTask.CompletedTask;
        }
    }
}