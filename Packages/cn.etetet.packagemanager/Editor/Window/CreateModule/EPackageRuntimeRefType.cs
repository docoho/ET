using System;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 新包 Runtime 程序集被哪些 ET 核心程序集（Hotfix / HotfixView / Model / ModelView）引用的位枚举
    /// </summary>
    [Flags]
    public enum EPackageRuntimeRefType
    {
        All        = -1,
        None       = 0,
        Hotfix     = 1,
        HotfixView = 1 << 1,
        Model      = 1 << 2,
        ModelView  = 1 << 3,
    }
}
