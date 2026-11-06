namespace ET
{
    /// <summary>
    /// 逻辑取反装饰节点，当其唯一子节点成功时返回配置的 ErrorCode，子节点失败时返回 0（成功）。
    /// </summary>
    public class BTNot: BTDecorate
    {
        public int ErrorCode = 1;
    }
}
