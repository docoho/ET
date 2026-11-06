namespace ET
{
    /// <summary>
    /// 场景类型常量扩展。
    /// </summary>
    public static partial class SceneType
    {
        /// <summary>客户端独立网络收发纤程场景类型</summary>
        public const int NetClient = PackageType.Login * 1000 + 1;
        /// <summary>网关为进图玩家创建的临时过渡地图场景类型</summary>
        public const int GateMap = PackageType.Login * 1000 + 2;
    }
}