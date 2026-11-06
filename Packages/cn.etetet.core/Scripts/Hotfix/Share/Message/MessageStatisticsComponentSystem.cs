using System;
using System.Collections.Generic;
using System.Text;

namespace ET
{
    /// <summary>
    /// 消息统计组件系统（<see cref="MessageStatisticsComponent"/> 的逻辑扩展）。
    /// 用于按时间窗口（默认 1 秒）统计单个网络会话收到的各类消息数量，防止客户端高频发包攻击（DoS）或异常死循环发包。
    /// </summary>
    [EntitySystemOf(typeof(MessageStatisticsComponent))]
    public static partial class MessageStatisticsComponentSystem
    {
        #region 生命周期方法

        /// <summary>
        /// 组件唤醒时初始化当前统计时间窗口的起始时间戳。
        /// </summary>
        [EntitySystem]
        private static void Awake(this MessageStatisticsComponent self)
        {
            self.CurrentSecondStart = self.GetSingleton<TimeInfo>().ClientNow();
        }

        /// <summary>
        /// 组件销毁时清空消息统计字典。
        /// </summary>
        [EntitySystem]
        private static void Destroy(this MessageStatisticsComponent self)
        {
            self.MessageStats.Clear();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 记录消息类型并校验当前时间窗口内的消息总数是否超过阈值。
        /// </summary>
        /// <param name="self">当前组件实例</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="maxMessageCount">单个时间窗口允许的最大消息数量阈值</param>
        /// <returns>true: 正常, false: 超过阈值</returns>
        public static bool Check(this MessageStatisticsComponent self, Type messageType, int maxMessageCount)
        {
            long now = self.GetSingleton<TimeInfo>().ClientNow();

            // 检查是否超过统计周期，超过则清空重新统计
            if (now - self.CurrentSecondStart >= self.WindowSize)
            {
                self.MessageStats.Clear();
                self.TotalCount = 0;
                self.CurrentSecondStart = now;
            }

            // 累加消息计数
            int count = self.MessageStats.GetValueOrDefault(messageType, 0);
            self.MessageStats[messageType] = count + 1;
            
            // 累加消息总数
            self.TotalCount++;

            // 如果消息总数超过阈值，打印统计信息并重置
            if (self.TotalCount <= maxMessageCount)
            {
                return true;
            }

            self.PrintStatistics(maxMessageCount);
                
            // 重置统计
            self.MessageStats.Clear();
            self.TotalCount = 0;
            self.CurrentSecondStart = now;
                
            return false;
        }

        /// <summary>
        /// 打印统计信息
        /// 输出当前秒内的消息统计明细到错误日志，便于排查哪种消息刷包
        /// </summary>
        /// <param name="self">当前组件实例</param>
        /// <param name="maxMessageCount">触发告警的消息数量阈值</param>
        public static void PrintStatistics(this MessageStatisticsComponent self, int maxMessageCount)
        {
            Dictionary<Type, int> stats = self.MessageStats;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("========== Message Statistics ==========");
            sb.AppendLine($"Window: Current Second ({self.WindowSize}ms)");
            sb.AppendLine($"Total Messages: {self.TotalCount}, MaxMessageCount: {maxMessageCount}");

            if (stats.Count > 0)
            {
                sb.AppendLine("Message Types:");
                foreach (var kvp in stats)
                {
                    sb.AppendLine($"  {kvp.Key.FullName}: {kvp.Value}");
                }
            }
            else
            {
                sb.AppendLine("No messages in current second");
            }

            sb.AppendLine("========================================");
            Log.Error(sb.ToString());
        }

        #endregion
    }
}