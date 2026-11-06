namespace ET.Test
{
    /// <summary>
    /// 测试区服号（Zone）自增分配器组件，挂载在 Test 主控场景上，确保并发或连续运行的每个测试用例拥有互不冲突的 Zone ID。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class TestZoneAllocatorComponent : Entity, IAwake
    {
        /// <summary>
        /// 测试专用起始 Zone 编号（从 100 开始分配，避免与默认配置冲突）
        /// </summary>
        public const int FirstTestZone = 100;

        /// <summary>
        /// 下一个待分配的 Zone 编号
        /// </summary>
        public int Zone = FirstTestZone;
    }
}
