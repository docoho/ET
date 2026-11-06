namespace ET.Server
{
    /// <summary>
    /// <see cref="RouterManagerAddressComponent"/> 的生命周期系统类。
    /// </summary>
    [EntitySystemOf(typeof(RouterManagerAddressComponent))]
    public static partial class RouterManagerAddressComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RouterManagerAddressComponent self)
        {
        }
    }
}
