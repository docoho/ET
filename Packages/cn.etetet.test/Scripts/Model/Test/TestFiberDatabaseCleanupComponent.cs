using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// 测试 Fiber 临时数据库自动清理组件，记录测试期间使用过的逻辑数据库名称，在测试结束或 Fiber 销毁时自动删库清理。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class TestFiberDatabaseCleanupComponent : Entity, IAwake
    {
        /// <summary>
        /// 已注册需在测试结束时清理的逻辑数据库名称集合
        /// </summary>
        public HashSet<string> LogicalDbNames = new();
    }
}
