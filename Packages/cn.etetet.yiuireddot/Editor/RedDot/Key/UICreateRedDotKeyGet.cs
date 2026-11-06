#if UNITY_EDITOR

using System.Collections.Generic;
using System.Text;

namespace YIUIFramework.Editor
{
    /// <summary>
    /// 将 RedDotKeyData 列表拼接为 C# 常量字段定义代码片段的辅助类。
    /// </summary>
    internal static class UICreateRedDotKeyGet
    {
        public static string Get(List<RedDotKeyData> dataList)
        {
            var sb = SbPool.Get();
            foreach (var data in dataList)
            {
                data.AddKeyData(sb);
            }

            return SbPool.PutAndToStr(sb);
        }

        private const string m_EnumContent = @"
        [LabelText(""{0}"")]
        public const int Key{1} = {1};
";

        private static void AddKeyData(this RedDotKeyData data, StringBuilder sb)
        {
            var des = string.IsNullOrEmpty(data.Des) ? data.Id.ToString() : data.Des;
            sb.AppendFormat(m_EnumContent, des, data.Id);
        }
    }
}
#endif