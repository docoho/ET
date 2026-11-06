using System;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 创建新包时包含的代码层级结构位枚举（Runtime / Editor / Hotfix / HotfixView / Model / ModelView）
    /// </summary>
    [Flags]
    public enum EPackageCreateType
    {
        All        = -1,
        Runtime    = 1,
        Editor     = 1 << 1,
        Hotfix     = 1 << 2,
        HotfixView = 1 << 3,
        Model      = 1 << 4,
        ModelView  = 1 << 5,
    }
}
