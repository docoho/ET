namespace ET
{
    /// <summary>
    /// 代码加载后自动初始化的全局单例标记特性。
    /// 任何标记了 <c>[CodeProcess]</c> 的 <see cref="ASingleton"/> 派生类（例如 <see cref="EntitySystemSingleton"/>、<see cref="EventSystem"/>、<c>MessageDispatcher</c>），
    /// 都会在 <see cref="CodeTypes.CodeProcess"/> 调用时被自动实例化、调用 <c>Awake()</c> 并注册到 <see cref="World.Instance"/> 中！
    /// </summary>
    public class CodeProcessAttribute: BaseAttribute
    {

    }
}