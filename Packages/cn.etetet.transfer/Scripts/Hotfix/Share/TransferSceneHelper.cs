namespace ET
{
    /// <summary>
    /// 跨场景资源对比辅助类。
    /// </summary>
    public static class TransferSceneHelper
    {
        /// <summary>
        /// 对比前后两个场景的 <see cref="MapConfig.MapResName"/>，判断是否需要重新加载地图资源并重建客户端场景（若仅同图切换分线则无需重建）。
        /// </summary>
        public static bool IsChangeScene(Fiber fiber, string preSceneName, string nextSceneName)
        {
            MapConfig preMapConfig = null;
            if (preSceneName != null)
            {
                preMapConfig = fiber.GetSingleton<MapConfigCategory>().GetByName(preSceneName.GetSceneConfigName());
            }
            MapConfig mapConfig = fiber.GetSingleton<MapConfigCategory>().GetByName(nextSceneName.GetSceneConfigName());
            return preMapConfig?.MapResName != mapConfig?.MapResName;
        }
    }
}
