namespace ET
{
    /// <summary>
    /// 顺序组合节点（逻辑“与”）：按顺序执行子节点，遇到第一个返回非 0（失败）的子节点即停止并返回该错误码。
    /// </summary>
    public class BTSequence: BTComposite
    {
    }
}