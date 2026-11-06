namespace ET.Server
{
    /// <summary>
    /// 客户端请求获取成就统计信息（<see cref="C2M_GetAchievementStats"/>）的 Location RPC 处理器
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_GetAchievementStats_Handler : MessageLocationHandler<Unit, C2M_GetAchievementStats, M2C_GetAchievementStats>
    {
        /// <summary>
        /// 处理获取角色成就总体统计信息（完成数、总点数、最近完成等）请求
        /// </summary>
        /// <param name="unit">玩家角色单位</param>
        /// <param name="request">获取成就统计请求消息</param>
        /// <param name="response">获取成就统计响应消息</param>
        protected override async ETTask Run(Unit unit, C2M_GetAchievementStats request, M2C_GetAchievementStats response)
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

                // 获取成就统计信息
                response.Stats = achievementComponent.GetAchievementStats();
                
                response.Error = ErrorCode.ERR_Success;
                Log.Debug($"Retrieved achievement stats: {response.Stats.CompletedAchievements}/{response.Stats.TotalAchievements}");
                
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to get achievement stats: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to get achievement stats: {e.Message}";
            }
        }
    }
}