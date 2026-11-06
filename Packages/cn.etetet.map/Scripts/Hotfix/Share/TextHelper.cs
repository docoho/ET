namespace ET
{
    /// <summary>
    /// 多语言文本配置查询与格式化日志输出辅助类。
    /// </summary>
    public static class TextHelper
    {
        /// <summary>
        /// 按文本配置表 ID 获取中文文本内容（应传入 TextConstDefine 中定义的常量）。
        /// </summary>
        // 不允许直接传入int，应该传入TextConst的常量
        private static string GetText(Fiber fiber, int key)
        {
            return fiber.GetSingleton<TextConfigCategory>().Get(key).CN;
        }

        /// <summary>
        /// 输出无参数的文本配置错误日志。
        /// </summary>
        public static void OutputText(Fiber fiber, int key)
        {
            Log.Error(GetText(fiber, key));
        }

        /// <summary>
        /// 输出带 1 个格式化参数的文本配置错误日志。
        /// </summary>
        public static void OutputText(Fiber fiber, int key, object args1)
        {
            Log.Error(string.Format(GetText(fiber, key), args1));
        }
        
        /// <summary>
        /// 输出带 2 个格式化参数的文本配置错误日志。
        /// </summary>
        public static void OutputText(Fiber fiber, int key, object args1, object args2)
        {
            Log.Error(string.Format(GetText(fiber, key), args1, args2));
        }
        
        /// <summary>
        /// 输出带 3 个格式化参数的文本配置错误日志。
        /// </summary>
        public static void OutputText(Fiber fiber, int key, object args1, object args2, object args3)
        {
            Log.Error(string.Format(GetText(fiber, key), args1, args2, args3));
        }
        
        /// <summary>
        /// 输出带可变对象数组参数的文本配置错误日志。
        /// </summary>
        public static void OutputText(Fiber fiber, int key, params object[] args)
        {
            Log.Error(string.Format(GetText(fiber, key), args));
        }
        
        /// <summary>
        /// 输出带可变字符串数组参数的文本配置错误日志。
        /// </summary>
        public static void OutputText(Fiber fiber, int key, params string[] args)
        {
            Log.Error(string.Format(GetText(fiber, key), args));
        }
    }
}
