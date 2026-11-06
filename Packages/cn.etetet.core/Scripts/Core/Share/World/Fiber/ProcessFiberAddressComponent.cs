namespace ET
{
    /// <summary>
    /// 进程级功能纤程（如 NetInner、ServiceDiscovery 等单进程唯一的后台服务纤程）地址自动注册组件。
    /// 挂载在对应纤程的 <see cref="Scene"/> 上时，会自动将该纤程的 <see cref="FiberInstanceId"/> 按 <see cref="SceneType"/> 注册到 <see cref="ProcessFiberAddressSingleton"/> 中，方便同进程其他 Fiber 快速寻址。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ProcessFiberAddressComponent : Entity, IAwake, IDestroy
    {
        public int SceneType;
        public FiberInstanceId FiberInstanceId;
    }
}
