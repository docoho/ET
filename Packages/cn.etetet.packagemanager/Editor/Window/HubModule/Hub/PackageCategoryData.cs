#if ODIN_INSPECTOR
using System.Collections.Generic;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 包仓库（Hub）分类目录类型枚举
    /// </summary>
    public enum EPackageCategoryType
    {
        All    = 0, //所有
        Custom = 1, //自定义
        Other  = 2, //没有分类的
    }

    /// <summary>
    /// 包仓库（Hub）单个分类菜单节点绑定的上下文数据
    /// </summary>
    public class PackageCategoryData
    {
        /// <summary>
        /// 分类类型（全部 / 自定义多级分类 / 未分类）
        /// </summary>
        public EPackageCategoryType CategoryType;
        /// <summary>
        /// 当前层级分类名称
        /// </summary>
        public string               Category;
        /// <summary>
        /// 菜单树完整路径
        /// </summary>
        public string               CategoryPath;
        /// <summary>
        /// 分类所处的树深度层级（从 1 开始）
        /// </summary>
        public int                  Layer;
        /// <summary>
        /// 该分类下包含的所有包数据列表
        /// </summary>
        public List<PackageHubData> AllPackages;
    }
}
#endif