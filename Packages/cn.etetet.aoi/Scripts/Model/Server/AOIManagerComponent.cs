namespace ET.Server
{
    /// <summary>
    /// 场景 AOI 网格管理组件（挂载在服务端 Map 场景上，管理所有 <see cref="Cell"/> 子实体）。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class AOIManagerComponent: Entity, IAwake
    {
        /// <summary>
        /// 单个 AOI 网格单元的边长（毫米，50 * 1000 即 50 米）。
        /// </summary>
        public const int CellSize = 50 * 1000;
    }
}