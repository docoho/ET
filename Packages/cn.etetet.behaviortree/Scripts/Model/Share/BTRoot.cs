using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 行为树根节点抽象基类，持有全局唯一的 TreeId，用于编辑器运行时路径调试匹配。
    /// </summary>
    public abstract class BTRoot: BTNode
    {
        [DisplayAsString]
        public long TreeId = RandomGenerator.RandInt64();
    }
}