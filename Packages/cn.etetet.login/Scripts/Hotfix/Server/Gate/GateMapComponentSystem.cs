namespace ET.Server
{
    /// <summary>
    /// 网关临时地图组件系统：用于在玩家首次进入地图前创建一个临时的 GateMap 纤程来承载初始 Unit 并执行跨服 Transfer。
    /// </summary>
    [EntitySystemOf(typeof(GateMapComponent))]
    public static partial class GateMapComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this GateMapComponent self)
        {

        }
        
        [EntitySystem]
        private static void Awake(this GateMapComponent self)
        {

        }

        /// <summary>
        /// 为指定玩家 ID 创建一个临时的 GateMap 场景纤程。
        /// </summary>
        public static async ETTask Create(this GateMapComponent self, long id)
        {
            self.Fiber = await self.Fiber().CreateFiber(id, SceneType.Map, $"GateMap@{id}");
        }
    }
}