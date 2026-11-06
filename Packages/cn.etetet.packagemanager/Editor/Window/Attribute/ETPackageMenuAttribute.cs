#if ODIN_INSPECTOR
using System;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// ET 包管理窗口自动菜单注册特性（标记在 BasePackageToolModule 子类上以自动挂载到菜单树）
    /// </summary>
    public class ETPackageMenuAttribute : Attribute
    {
        /// <summary>
        /// 菜单显示名称（支持 '/' 分隔的多级路径）
        /// </summary>
        public string MenuName;

        /// <summary>
        /// 菜单排序权重（越小越靠前）
        /// </summary>
        public int Order;

        public ETPackageMenuAttribute(string menuName, int order = 0)
        {
            MenuName = menuName;
            Order    = order;
        }
    }

    /// <summary>
    /// 反射收集到的自动菜单项元数据
    /// </summary>
    public class ETPackageAutoMenuData
    {
        public Type Type;

        public string MenuName;

        public int Order;
    }
}
#endif
