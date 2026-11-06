namespace ET
{
    /// <summary>
    /// Buff 效果行为树根节点抽象基类，不同的派生类对应 Buff 不同的生命周期钩子（Add/Remove/Tick/Hitted）。
    /// </summary>
    [System.Serializable]
    public abstract class EffectNode: BTRoot
    {
        
    }
}