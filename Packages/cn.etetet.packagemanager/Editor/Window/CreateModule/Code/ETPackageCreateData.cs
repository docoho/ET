namespace ET.PackageManager.Editor
{
    /// <summary>
    /// 创建 ET 新包所需的完整配置参数上下文
    /// </summary>
    public class ETPackageCreateData
    {
        /// <summary>
        /// 包作者名称
        /// </summary>
        public string                   PackageAuthor;
        /// <summary>
        /// 包在工程中的相对路径（如 Packages/cn.etetet.xxx）
        /// </summary>
        public string                   PackagePath;
        /// <summary>
        /// 包唯一数字 ID（用于生成 PackageType 常量）
        /// </summary>
        public int                      PackageId;
        /// <summary>
        /// 包简短名称（不含 cn.etetet. 前缀）
        /// </summary>
        public string                   PackageName;
        /// <summary>
        /// Runtime 程序集名称
        /// </summary>
        public string                   AssemblyName;
        /// <summary>
        /// 在 Unity Package Manager 中的显示名称
        /// </summary>
        public string                   DisplayName;
        /// <summary>
        /// 包功能描述
        /// </summary>
        public string                   Description;
        /// <summary>
        /// 包含的代码层级结构类型
        /// </summary>
        public EPackageCreateType       PackageCreateType;
        /// <summary>
        /// Runtime 程序集被哪些 ET 程序集引用
        /// </summary>
        public EPackageRuntimeRefType   RuntimeRefType;
        /// <summary>
        /// Client / Server / Share 目录划分类型
        /// </summary>
        public EPackageCreateFolderType FolderType;
    }
}
