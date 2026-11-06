namespace ET
{
    /// <summary>
    /// Excel 导表前置自定义处理器接口。
    /// 在执行 Luban 导表前，<c>ET.ExcelExporter</c> 会反射扫描并依次执行所有实现了此接口的处理器（如多包同名表路径汇总、多语言常量生成等）。
    /// </summary>
    public interface IExcelHandler
    {
        /// <summary>
        /// 执行自定义 Excel 预处理逻辑。
        /// </summary>
        public void Run();
    }
}