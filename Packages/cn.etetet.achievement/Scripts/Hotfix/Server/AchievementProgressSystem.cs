using System;

namespace ET.Server
{
    /// <summary>
    /// 服务端成就进度实体系统，负责单个成就进度实体的初始化、递增、重置与文本格式化
    /// </summary>
    [EntitySystemOf(typeof(AchievementProgress))]
    public static partial class AchievementProgressSystem
    {
        /// <summary>
        /// 初始化成就进度实体
        /// </summary>
        /// <param name="self">成就进度实体实例</param>
        /// <param name="achievementId">关联的成就配置 ID</param>
        /// <param name="maxProgress">目标最大进度值</param>
        [EntitySystem]
        private static void Awake(this AchievementProgress self, int achievementId, int maxProgress)
        {
            self.AchievementId = achievementId;
            self.MaxProgress = maxProgress;
            self.Progress = 0;
            self.LastUpdateTime = self.GetSingleton<TimeInfo>().ServerNow();
        }

        /// <summary>
        /// 更新进度值（不超过最大进度）并刷新最后更新时间戳
        /// </summary>
        /// <param name="self">成就进度实体实例</param>
        /// <param name="progress">新的进度值</param>
        public static void UpdateProgress(this AchievementProgress self, int progress)
        {
            int oldProgress = self.Progress;
            self.Progress = Math.Min(progress, self.MaxProgress);
            self.LastUpdateTime = self.GetSingleton<TimeInfo>().ServerNow();

            // 如果进度有变化，记录日志
            if (oldProgress != self.Progress)
            {
                Log.Debug($"Achievement progress updated: {self.AchievementId} from {oldProgress} to {self.Progress}");
            }
        }

        /// <summary>
        /// 在当前进度基础上增加指定进度值
        /// </summary>
        /// <param name="self">成就进度实体实例</param>
        /// <param name="value">增加的进度增量</param>
        public static void AddProgress(this AchievementProgress self, int value)
        {
            self.UpdateProgress(self.Progress + value);
        }

        /// <summary>
        /// 将进度清零并刷新最后更新时间戳
        /// </summary>
        /// <param name="self">成就进度实体实例</param>
        public static void Reset(this AchievementProgress self)
        {
            self.Progress = 0;
            self.LastUpdateTime = self.GetSingleton<TimeInfo>().ServerNow();
        }

        /// <summary>
        /// 检查当前进度是否已达到最大进度要求
        /// </summary>
        /// <param name="self">成就进度实体实例</param>
        /// <returns>若已完成则返回 true</returns>
        public static bool CheckComplete(this AchievementProgress self)
        {
            return self.IsCompleted;
        }

        /// <summary>
        /// 获取格式化后的进度文本（格式为 "当前进度/最大进度"）
        /// </summary>
        /// <param name="self">成就进度实体实例</param>
        /// <returns>进度文本字符串</returns>
        public static string GetProgressText(this AchievementProgress self)
        {
            return $"{self.Progress}/{self.MaxProgress}";
        }
    }
}