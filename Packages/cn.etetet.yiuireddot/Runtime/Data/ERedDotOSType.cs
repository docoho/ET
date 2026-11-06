using Sirenix.OdinInspector;

namespace YIUIFramework
{
    /// <summary>
    /// 红点变更操作类型枚举（用于调试堆栈记录是由修改数量还是修改提示开关触发）。
    /// </summary>
    public enum ERedDotOSType
    {
        [LabelText("改变数量")]
        Count = 0,

        [LabelText("改变提示")]
        Tips = 1,
    }
}