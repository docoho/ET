using System;

namespace ET.Server
{
    /// <summary>
    /// 服务端成就处理器标记特性，用于将具体成就逻辑处理类与对应的 <see cref="AchievementType"/> 绑定
    /// </summary>
    [EnableClass]
    [AttributeUsage(AttributeTargets.Class)]
    public class AchievementHandlerAttribute: Attribute
    {
        /// <summary>
        /// 该处理器负责处理的成就类型
        /// </summary>
        public AchievementType AchievementType { get; }

        /// <summary>
        /// 构造成就处理器标记特性
        /// </summary>
        /// <param name="achievementType">对应的成就类型枚举</param>
        public AchievementHandlerAttribute(AchievementType achievementType)
        {
            AchievementType = achievementType;
        }
    }
}