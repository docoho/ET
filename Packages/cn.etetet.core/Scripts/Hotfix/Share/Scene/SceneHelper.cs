namespace ET
{
    /// <summary>
    /// 场景辅助工具类，提供场景名称解析等扩展方法。
    /// </summary>
    public static class SceneHelper
    {
        /// <summary>
        /// 从可能带有实例后缀的场景名（例如 <c>"Map1@12345"</c>）中提取基础配置表场景名（即 <c>"@"</c> 符号前的部分 <c>"Map1"</c>）。
        /// </summary>
        /// <param name="sceneName">原始场景名称。</param>
        /// <returns>去除后缀后的场景配置名称。</returns>
        public static string GetSceneConfigName(this string sceneName)
        {
            return sceneName.Split("@")[0];
        }
    }
}