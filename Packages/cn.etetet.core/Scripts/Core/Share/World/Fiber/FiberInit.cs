namespace ET
{
    /// <summary>
    /// 纤程创建时的初始化回调参数结构体。
    /// 各业务模块通过编写 <c>[Invoke(SceneType.XXX)] public class FiberInit_XXX : AInvokeHandler&lt;FiberInit, ETTask&gt;</c> 来为特定类型的场景纤程挂载初始组件！
    /// </summary>
    public struct FiberInit
    {
        /// <summary>正在初始化的目标纤程实例</summary>
        public Fiber Fiber { get; set; }
    }
}