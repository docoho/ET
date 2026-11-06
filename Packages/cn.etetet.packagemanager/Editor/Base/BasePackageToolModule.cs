#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// ET 包管理窗口功能模块抽象基类（持有所属 OdinMenuEditorWindow、OdinMenuTree、模块名称及自定义上下文数据）
    /// </summary>
    [HideReferenceObjectPicker]
    public abstract class BasePackageToolModule : BaseCreateModule
    {
        /// <summary>
        /// 所属的主窗口实例
        /// </summary>
        [HideInInspector]
        public OdinMenuEditorWindow AutoTool { get; internal set; }

        /// <summary>
        /// 所属的 Odin 左侧菜单树实例
        /// </summary>
        [HideInInspector]
        public OdinMenuTree Tree { get; internal set; }

        /// <summary>
        /// 当前模块在菜单树中的名称或路径
        /// </summary>
        [HideInInspector]
        public string ModuleName { get; internal set; }

        /// <summary>
        /// 附加的用户自定义上下文数据
        /// </summary>
        [HideInInspector]
        public object UserData { get; internal set; }

        /// <summary>
        /// 当该模块对应的菜单项被选中时触发的回调
        /// </summary>
        public virtual void SelectionMenu()
        {
        }
    }
}
#endif