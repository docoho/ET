using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    /// <summary>
    /// 行为树组合控制节点抽象基类（如 BTSequence、BTSelector），用于按特定控制流规则驱动多个子节点。
    /// </summary>
    public abstract class BTComposite: BTNode
    {
    }
}