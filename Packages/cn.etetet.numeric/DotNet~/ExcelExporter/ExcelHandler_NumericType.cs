using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OfficeOpenXml;

namespace ET
{
    /// <summary>
    /// 数值类型（NumericType）Excel 导出预处理器。
    /// <remarks>
    /// 扫描所有 <c>Packages/cn.etetet.*</c> 包下的 <c>Luban/Config/Datas/NumericType.xlsx</c>，
    /// 汇总校验唯一性（防止跨包 ID 或名称冲突），并自动为勾选了二级属性（<c>GenSecondAttr == "1"</c>）的数值生成
    /// <c>Base</c>（×10+1）、<c>Add</c>（×10+2）、<c>Pct</c>（×10+3）、<c>FinalAdd</c>（×10+4）、<c>FinalPct</c>（×10+5）五项子枚举，
    /// 最终输出到 <c>Packages/cn.etetet.numeric/Luban/Config/Defines/NumericType.xml</c> 供 Luban 导出 <see cref="NumericType"/> 枚举。
    /// </remarks>
    /// </summary>
    public class ExcelHandler_NumericType : IExcelHandler
    {
        private const string NumericTypePackageName = "cn.etetet.numeric";
        private const string NumericTypeXmlPath = "Luban/Config/Defines/NumericType.xml";

        /// <summary>
        /// 单条数值类型配置项数据结构。
        /// </summary>
        private sealed class NumericTypeEntry
        {
            /// <summary>
            /// 数值枚举名称（如 Speed、MaxHP）。
            /// </summary>
            public string Name { get; init; }

            /// <summary>
            /// 数值枚举整数 ID 字符串（如 1000、1002）。
            /// </summary>
            public string Id { get; init; }

            /// <summary>
            /// 数值注释说明。
            /// </summary>
            public string Comment { get; init; }

            /// <summary>
            /// 是否自动生成 5 个二级属性分量（"1" 表示生成 Base/Add/Pct/FinalAdd/FinalPct）。
            /// </summary>
            public string GenSecondAttr { get; init; }
        }

        /// <summary>
        /// 执行所有包内 NumericType.xlsx 的扫描解析与 Luban XML 枚举定义生成。
        /// </summary>
        public void Run()
        {
            // 遍历所有Packages目录，查找NumericType.xlsx文件
            string packagesDir = Path.GetFullPath("Packages");
            string[] packageDirs = Directory.GetDirectories(packagesDir, "cn.etetet.*");
            
            HashSet<string> globalProcessedIds = new HashSet<string>(); // 全局ID检查，防止跨包重复
            HashSet<string> globalProcessedNames = new HashSet<string>(StringComparer.Ordinal);
            List<NumericTypeEntry> allEntries = new();
            int totalEntries = 0;
            int processedPackages = 0;

            foreach (string packageDir in packageDirs)
            {
                string numericTypeExcelPath = Path.Combine(packageDir, "Luban/Config/Datas/NumericType.xlsx");
                if (File.Exists(numericTypeExcelPath))
                {
                    string packageName = Path.GetFileName(packageDir);
                    List<NumericTypeEntry> packageEntries = new();
                    
                    if (ProcessNumericTypeExcel(numericTypeExcelPath, packageName, packageEntries, globalProcessedIds, globalProcessedNames))
                    {
                        allEntries.AddRange(packageEntries);
                        totalEntries += packageEntries.Count;
                        processedPackages++;
                    }
                }
            }

            GenerateNumericTypeXml(allEntries);
            Log.Console($"NumericType xml generation completed! Processed {totalEntries} entries from {processedPackages} packages.");
        }

        /// <summary>
        /// 读取并解析单个包中的 NumericType.xlsx 工作表数据。
        /// </summary>
        private bool ProcessNumericTypeExcel(
            string excelPath,
            string packageName,
            List<NumericTypeEntry> packageEntries,
            HashSet<string> globalProcessedIds,
            HashSet<string> globalProcessedNames)
        {
            try
            {
                ExcelPackage excelPackage = ExcelExporter.GetPackage(excelPath);
                ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets[0];
                
                if (worksheet.Dimension == null)
                {
                    Log.Warning($"Empty worksheet in {excelPath}");
                    return false;
                }

                for (int i = 6; i <= worksheet.Dimension.End.Row; ++i)
                {
                    string name = worksheet.Cells[i, 3].Text.Trim();
                    string id = worksheet.Cells[i, 2].Text.Trim();
                    string comment = worksheet.Cells[i, 4].Text.Trim();
                    string genSecondAttr = worksheet.Cells[i, 5].Text.Trim();

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
                    {
                        continue;
                    }

                    if (globalProcessedIds.Contains(id))
                    {
                        Log.Warning($"Duplicate ID {id} found in {packageName}, skipping...");
                        continue;
                    }

                    if (!globalProcessedNames.Add(name))
                    {
                        Log.Warning($"Duplicate name {name} found in {packageName}, skipping...");
                        continue;
                    }

                    globalProcessedIds.Add(id);
                    packageEntries.Add(new NumericTypeEntry
                    {
                        Name = name,
                        Id = id,
                        Comment = comment,
                        GenSecondAttr = genSecondAttr,
                    });
                }
                
                Log.Debug($"Processed {excelPath}: found {packageEntries.Count} entries");
                return packageEntries.Count > 0;
            }
            catch (System.Exception e)
            {
                Log.Error($"Error processing {excelPath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 根据汇总的数值条目列表生成 Luban 的 NumericType.xml 枚举定义文件。
        /// </summary>
        private void GenerateNumericTypeXml(List<NumericTypeEntry> entries)
        {
            entries.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.Ordinal));

            XElement enumElement = new("enum",
                new XAttribute("name", "NumericType"),
                new XAttribute("group", "numeric"),
                new XAttribute("comment", "Numeric Type"));

            HashSet<string> generatedNames = new(StringComparer.Ordinal);
            foreach (NumericTypeEntry entry in entries)
            {
                AddEnumVar(enumElement, generatedNames, entry.Name, entry.Id, entry.Comment);

                if (entry.GenSecondAttr == "1")
                {
                    AddEnumVar(enumElement, generatedNames, $"{entry.Name}Base", $"{entry.Id}1", "");
                    AddEnumVar(enumElement, generatedNames, $"{entry.Name}Add", $"{entry.Id}2", "");
                    AddEnumVar(enumElement, generatedNames, $"{entry.Name}Pct", $"{entry.Id}3", "");
                    AddEnumVar(enumElement, generatedNames, $"{entry.Name}FinalAdd", $"{entry.Id}4", "");
                    AddEnumVar(enumElement, generatedNames, $"{entry.Name}FinalPct", $"{entry.Id}5", "");
                }
            }

            XElement moduleElement = new("module",
                new XAttribute("name", ""),
                enumElement);

            XDocument document = new(new XDeclaration("1.0", "utf-8", null), moduleElement);

            string numericPackageDir = Path.Combine("Packages", NumericTypePackageName);
            string outputPath = Path.Combine(numericPackageDir, NumericTypeXmlPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            document.Save(outputPath);

            Log.Console($"Generated {outputPath} with {entries.Count} source entries");
        }

        /// <summary>
        /// 向 XML enum 节点添加一个 var 枚举项并校验名称唯一性。
        /// </summary>
        private void AddEnumVar(XElement enumElement, HashSet<string> generatedNames, string name, string value, string comment)
        {
            if (!generatedNames.Add(name))
            {
                throw new Exception($"Duplicate NumericType enum item: {name}");
            }

            XElement varElement = new("var",
                new XAttribute("name", name),
                new XAttribute("value", value));

            if (!string.IsNullOrEmpty(comment))
            {
                varElement.Add(new XAttribute("comment", comment));
            }

            enumElement.Add(varElement);
        }
    }
}
