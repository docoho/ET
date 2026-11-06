using System.Collections.Generic;
using System.Text;

namespace ET.PackageManager.Editor
{
    /// <summary>
    /// StringBuilder池（线程安全对象池，避免高频字符串拼接产生 GC 分配）
    /// </summary>
    public static class SbPool
    {
        /// <summary>
        /// 内部缓存 StringBuilder 实例的栈容器
        /// </summary>
        private static Stack<StringBuilder> g_pool = new Stack<StringBuilder>(5);

        /// <summary>
        /// 从对象池中获取一个空的 StringBuilder 实例
        /// </summary>
        public static StringBuilder Get()
        {
            lock (g_pool)
            {
                return g_pool.Count == 0
                        ? new StringBuilder()
                        : g_pool.Pop();
            }
        }

        /// <summary>
        /// 清空 StringBuilder 内容并将其归还到对象池中
        /// </summary>
        public static void Put(StringBuilder sb)
        {
            sb.Clear();
            lock (g_pool)
            {
                g_pool.Push(sb);
            }
        }

        /// <summary>
        /// 生成最终字符串并将 StringBuilder 实例归还到对象池中
        /// </summary>
        public static string PutAndToStr(StringBuilder sb)
        {
            var str = sb.ToString();
            Put(sb);
            return str;
        }
    }
}
