using System;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 创建新包时的 CodeMode 代码目录划分类型位枚举（客户端 / 服务端 / 双端共享）
    /// </summary>
    [Flags]
    public enum EPackageCreateFolderType
    {
        All       = -1,
        Client    = 1 << 1,
        Server    = 1 << 2,
        Share     = 1 << 3,
    }
}
