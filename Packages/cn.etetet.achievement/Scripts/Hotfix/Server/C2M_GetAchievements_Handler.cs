namespace ET.Server
{
    /// <summary>
    /// 客户端请求获取成就列表（<see cref="C2M_GetAchievements"/>）的 Location RPC 处理器
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_GetAchievements_Handler : MessageLocationHandler<Unit, C2M_GetAchievements, M2C_GetAchievements>
    {
        /// <summary>
        /// 处理获取指定分类（或全部分类）成就列表请求
        /// </summary>
        /// <param name="unit">玩家角色单位</param>
        /// <param name="request">获取成就列表请求消息</param>
        /// <param name="response">获取成就列表响应消息</param>
        protected override async ETTask Run(Unit unit, C2M_GetAchievements request, M2C_GetAchievements response)
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

                // 获取成就列表
                var achievements = achievementComponent.GetAchievementList(request.CategoryId);
                response.Achievements.AddRange(achievements);
                
                response.Error = ErrorCode.ERR_Success;
                Log.Debug($"Retrieved {achievements.Count} achievements for category {request.CategoryId}");
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to get achievements: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to get achievements: {e.Message}";
            }
        }
    }
}