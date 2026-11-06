namespace ET
{
    /// <summary>
    /// <see cref="ProcessFiberAddressComponent"/> 生命周期系统：在 Awake 时向 <see cref="ProcessFiberAddressSingleton"/> 注册当前 Fiber 地址，Destroy 时自动注销。
    /// </summary>
    [EntitySystemOf(typeof(ProcessFiberAddressComponent))]
    public static partial class ProcessFiberAddressComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ProcessFiberAddressComponent self)
        {
            Scene root = self.Root();
            self.SceneType = root.SceneType;
            self.FiberInstanceId = new FiberInstanceId(root.Fiber.Id);
            self.GetSingleton<ProcessFiberAddressSingleton>().Register(self.SceneType, self.FiberInstanceId);
        }

        [EntitySystem]
        private static void Destroy(this ProcessFiberAddressComponent self)
        {
            self.GetSingleton<ProcessFiberAddressSingleton>().Unregister(self.SceneType, self.FiberInstanceId);
        }
    }
}
