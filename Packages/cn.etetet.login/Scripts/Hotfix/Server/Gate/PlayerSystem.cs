namespace ET.Server
{
    /// <summary>
    /// 网关玩家实体（Player）生命周期系统。
    /// </summary>
    [EntitySystemOf(typeof(Player))]
    public static partial class PlayerSystem
    {
        [EntitySystem]
        private static void Awake(this Player self, string a)
        {
            self.Account = a;
        }
    }
}