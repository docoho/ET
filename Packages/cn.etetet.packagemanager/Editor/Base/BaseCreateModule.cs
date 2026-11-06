#if ODIN_INSPECTOR
using Sirenix.OdinInspector;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 基类 自动创建模块
    /// 由其他模块继承后实现
    /// </summary>
    [HideReferenceObjectPicker]
    public abstract class BaseCreateModule
    {
        /// <summary>
        /// 模块初始化生命周期钩子
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// 模块销毁生命周期钩子
        /// </summary>
        public virtual void OnDestroy()
        {
        }
    }
}
#endif
