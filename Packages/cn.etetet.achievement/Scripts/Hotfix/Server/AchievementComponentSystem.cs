using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    /// <summary>
    /// 服务端成就组件系统，管理角色所有激活成就的生命周期、进度更新、完成判定、奖励领取及数据统计
    /// </summary>
    [EntitySystemOf(typeof(AchievementComponent))]
    public static partial class AchievementComponentSystem
    {
        /// <summary>
        /// 组件初始化回调
        /// </summary>
        /// <param name="self">成就组件实例</param>
        [EntitySystem]
        private static void Awake(this AchievementComponent self)
        {
            // 初始化成就系统
        }

        /// <summary>
        /// 组件销毁回调
        /// </summary>
        /// <param name="self">成就组件实例</param>
        [EntitySystem]
        private static void Destroy(this AchievementComponent self)
        {
            // 清理资源
        }

        /// <summary>
        /// 初始化成就系统（加载配置并激活满足前置条件的成就）
        /// </summary>
        /// <param name="self">成就组件实例</param>
        public static void Initialize(this AchievementComponent self)
        {
            // TODO: 从配置加载所有成就
            // 激活符合条件的成就
            self.ActivateEligibleAchievements();
        }

        /// <summary>
        /// 检查配置表并激活符合前置条件的成就
        /// </summary>
        /// <param name="self">成就组件实例</param>
        public static void ActivateEligibleAchievements(this AchievementComponent self)
        {
            // TODO: 从配置表获取所有成就，检查前置条件，激活符合条件的成就
        }

        /// <summary>
        /// 根据成就配置 ID 获取已激活的成就实体
        /// </summary>
        /// <param name="self">成就组件实例</param>
        /// <param name="achievementId">成就配置 ID</param>
        /// <returns>成就实体，若未激活则返回 null</returns>
        public static Achievement GetAchievement(this AchievementComponent self, int achievementId)
        {
            if (self.ActiveAchievements.TryGetValue(achievementId, out var achievementRef))
            {
                return achievementRef;
            }
            return null;
        }

        /// <summary>
        /// 添加并激活一个指定配置 ID 的成就子实体
        /// </summary>
        /// <param name="self">成就组件实例</param>
        /// <param name="configId">成就配置 ID</param>
        /// <returns>新创建的成就实体</returns>
        public static Achievement AddAchievement(this AchievementComponent self, int configId)
        {
            Achievement achievement = self.AddChild<Achievement, int>(configId);
            self.ActiveAchievements[configId] = achievement;
            return achievement;
        }

        /// <summary>
        /// 更新指定成就的当前进度，发布进度事件并在达到最大进度时自动触发完成逻辑
        /// </summary>
        /// <param name="self">成就组件实例</param>
        /// <param name="achievementId">成就配置 ID</param>
        /// <param name="progress">最新进度值</param>
        public static void UpdateAchievementProgress(this AchievementComponent self, int achievementId, int progress)
        {
            Achievement achievement = self.GetAchievement(achievementId);
            if (achievement == null)
            {
                return;
            }

            int oldProgress = achievement.Progress;
            achievement.Progress = progress;
            self.AchievementProgress[achievementId] = progress;

            // 发布进度更新事件
            EventSystem.Instance.Publish(self.GetParent<Unit>().Scene(), new AchievementProgressEvent
            {
                UnitId = self.GetParent<Unit>().Id,
                AchievementId = achievementId,
                Progress = progress,
                MaxProgress = achievement.MaxProgress
            });

            // 检查是否完成
            if (achievement.Progress >= achievement.MaxProgress && achievement.Status == AchievementStatus.InProgress)
            {
                self.CompleteAchievement(achievementId);
            }
        }

        /// <summary>
        /// 将指定成就标记为已完成，记录完成时间、最近完成列表并发布完成事件
        /// </summary>
        /// <param name="self">成就组件实例</param>
        /// <param name="achievementId">成就配置 ID</param>
        public static void CompleteAchievement(this AchievementComponent self, int achievementId)
        {
            Achievement achievement = self.GetAchievement(achievementId);
            if (achievement == null || achievement.Status != AchievementStatus.InProgress)
            {
                return;
            }

            achievement.Status = AchievementStatus.Completed;
            achievement.CompleteTime = self.GetSingleton<TimeInfo>().ServerNow();
            self.CompletedAchievements.Add(achievementId);

            // 添加到最近完成列表
            self.RecentAchievements.Add(achievementId);
            if (self.RecentAchievements.Count > 10)
            {
                self.RecentAchievements.RemoveAt(0);
            }

            // 发布完成事件
            EventSystem.Instance.Publish(self.GetParent<Unit>().Scene(), new AchievementCompleteEvent
            {
                UnitId = self.GetParent<Unit>().Id,
                AchievementId = achievementId,
                CompleteTime = achievement.CompleteTime
            });

            Log.Debug($"Achievement completed: {achievementId}");
        }

        /// <summary>
        /// 领取已完成成就的奖励，累加成就点数并发布领取事件
        /// </summary>
        /// <param name="self">成就组件实例</param>
        /// <param name="achievementId">成就配置 ID</param>
        /// <returns>是否成功领取</returns>
        public static bool ClaimAchievementReward(this AchievementComponent self, int achievementId)
        {
            Achievement achievement = self.GetAchievement(achievementId);
            if (achievement == null || achievement.Status != AchievementStatus.Completed)
            {
                return false;
            }

            achievement.Status = AchievementStatus.Claimed;
            achievement.ClaimTime = self.GetSingleton<TimeInfo>().ServerNow();
            self.ClaimedAchievements.Add(achievementId);
            self.EarnedPoints += achievement.Points;

            // 发放奖励
            self.GrantAchievementReward(achievementId);

            // 发布领取事件
            EventSystem.Instance.Publish(self.GetParent<Unit>().Scene(), new AchievementClaimedEvent
            {
                UnitId = self.GetParent<Unit>().Id,
                AchievementId = achievementId,
                ClaimTime = achievement.ClaimTime
            });

            Log.Debug($"Achievement reward claimed: {achievementId}");
            return true;
        }

        /// <summary>
        /// 根据成就配置向玩家发放具体成就奖励
        /// </summary>
        /// <param name="self">成就组件实例</param>
        /// <param name="achievementId">成就配置 ID</param>
        public static void GrantAchievementReward(this AchievementComponent self, int achievementId)
        {
            // TODO: 根据成就配置发放奖励
            // 这里只做结构占位，具体实现需结合奖励系统
        }

        /// <summary>
        /// 获取指定分类（或全部分类）的成就信息协议列表
        /// </summary>
        /// <param name="self">成就组件实例</param>
        /// <param name="categoryId">分类 ID（0 表示获取全部分类）</param>
        /// <returns>成就网络协议信息列表</returns>
        public static List<AchievementInfo> GetAchievementList(this AchievementComponent self, int categoryId = 0)
        {
            List<AchievementInfo> result = new List<AchievementInfo>();

            foreach (var kvp in self.ActiveAchievements)
            {
                Achievement achievement = kvp.Value;
                if (achievement == null)
                {
                    continue;
                }

                if (categoryId > 0 && achievement.CategoryId != categoryId)
                {
                    continue;
                }

                AchievementInfo info = AchievementInfo.Create();
                info.AchievementId = achievement.ConfigId;
                info.Status = (int)achievement.Status;
                info.Progress = achievement.Progress;
                info.MaxProgress = achievement.MaxProgress;
                info.CompleteTime = achievement.CompleteTime;
                info.ClaimTime = achievement.ClaimTime;
                result.Add(info);
            }

            return result;
        }

        /// <summary>
        /// 构建并返回角色成就汇总统计信息（总成就数、已完成数、点数及最近完成列表）
        /// </summary>
        /// <param name="self">成就组件实例</param>
        /// <returns>成就统计协议对象</returns>
        public static AchievementStatsInfo GetAchievementStats(this AchievementComponent self)
        {
            AchievementStatsInfo stats = AchievementStatsInfo.Create();
            stats.TotalAchievements = self.ActiveAchievements.Count;
            stats.CompletedAchievements = self.CompletedAchievements.Count;
            stats.TotalPoints = self.TotalPoints;
            stats.EarnedPoints = self.EarnedPoints;
            stats.RecentAchievements.AddRange(self.RecentAchievements);
            return stats;
        }

    }
}