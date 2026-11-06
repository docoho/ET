namespace ET.Server
{
    /// <summary>
    /// 客户端请求领取成就奖励（<see cref="C2M_ClaimAchievement"/>）的 Location RPC 处理器
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_ClaimAchievement_Handler : MessageLocationHandler<Unit, C2M_ClaimAchievement, M2C_ClaimAchievement>
    {
        /// <summary>
        /// 处理成就奖励领取请求，校验完成状态并返回发放的奖励列表
        /// </summary>
        /// <param name="unit">玩家角色单位</param>
        /// <param name="request">领取成就请求消息</param>
        /// <param name="response">领取成就响应消息</param>
        protected override async ETTask Run(Unit unit, C2M_ClaimAchievement request, M2C_ClaimAchievement response)
        {
            try
            {
                AchievementComponent achievementComponent = unit.GetComponent<AchievementComponent>();
                if (achievementComponent == null)
                {
                    response.Error = ErrorCode.ERR_Cancel;
                    response.Message = "Achievement component not found";
                    return;
                }

                // 尝试领取成就奖励
                bool success = achievementComponent.ClaimAchievementReward(request.AchievementId);
                if (!success)
                {
                    response.Error = ErrorCode.ERR_Cancel;
                    response.Message = "Achievement cannot be claimed";
                    return;
                }

                // TODO: 添加实际奖励到rewards列表
                // 这里模拟一些奖励
                AchievementReward reward = AchievementReward.Create();
                reward.Type = 1; // 经验
                reward.Count = 100;
                response.Rewards.Add(reward);
                
                response.Error = ErrorCode.ERR_Success;
                Log.Debug($"Achievement {request.AchievementId} reward claimed successfully");
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to claim achievement reward: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to claim achievement reward: {e.Message}";
            }
        }
    }
}