using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;

namespace ET
{
    /// <summary>
    /// Excel 文件缓存读取辅助类（基于 EPPlus 的 <see cref="ExcelPackage"/>）。
    /// </summary>
    public static class ExcelExporter
    {
        /// <summary>按文件路径缓存已打开的 <see cref="ExcelPackage"/> 实例，避免重复磁盘读取。</summary>
        private static Dictionary<string, ExcelPackage> packages = new();


        /// <summary>
        /// 获取或加载指定路径的 <see cref="ExcelPackage"/>（支持共享读写打开，防止 Excel 被打开锁定时报错）。
        /// </summary>
        /// <param name="filePath">Excel 文件完整路径。</param>
        /// <returns>对应的 <see cref="ExcelPackage"/> 实例。</returns>
        public static ExcelPackage GetPackage(string filePath)
        {
            if (!packages.TryGetValue(filePath, out var package))
            {
                using Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                package = new ExcelPackage(stream);
                packages[filePath] = package;
            }

            return package;
        }
    }
}
