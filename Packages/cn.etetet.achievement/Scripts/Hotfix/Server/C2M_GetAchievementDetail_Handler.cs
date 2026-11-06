namespace ET.Server
{
    /// <summary>
    /// 客户端请求获取指定成就详情（<see cref="C2M_GetAchievementDetail"/>）的 Location RPC 处理器
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_GetAchievementDetail_Handler : MessageLocationHandler<Unit, C2M_GetAchievementDetail, M2C_GetAchievementDetail>
    {
        /// <summary>
        /// 处理获取单个成就详情请求，组装成就名称、描述、图标、最大进度与点数等信息返回
        /// </summary>
        /// <param name="unit">玩家角色单位</param>
        /// <param name="request">获取成就详情请求消息</param>
        /// <param name="response">获取成就详情响应消息</param>
        protected override async ETTask Run(Unit unit, C2M_GetAchievementDetail request, M2C_GetAchievementDetail response)
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

                Achievement achievement = achievementComponent.GetAchievement(request.AchievementId);
                if (achievement == null)
                {
                    response.Error = ErrorCode.ERR_Cancel;
                    response.Message = "Achievement not found";
                    return;
                }

                // 创建成就详情
                AchievementDetailInfo detail = AchievementDetailInfo.Create();
                detail.AchievementId = achievement.ConfigId;
                detail.AchievementName = $"Achievement {achievement.ConfigId}";
                detail.Description = $"This is achievement {achievement.ConfigId}";
                detail.Icon = "achievement_icon";
                detail.CategoryId = achievement.CategoryId;
                detail.Type = (int)achievement.Type;
                detail.MaxProgress = achievement.MaxProgress;
                detail.Points = achievement.Points;
                
                // TODO: 从配置表获取前置成就和奖励信息
                
                response.Detail = detail;
                response.Error = ErrorCode.ERR_Success;
                Log.Debug($"Retrieved achievement detail for {request.AchievementId}");
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to get achievement detail: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to get achievement detail: {e.Message}";
            }
        }
    }
}