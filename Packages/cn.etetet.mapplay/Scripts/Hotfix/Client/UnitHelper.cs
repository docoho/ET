namespace ET.Client
{
    /// <summary>
    /// 客户端本地玩家主角（MyUnit）查询辅助工具类。
    /// </summary>
    public static partial class UnitHelper
    {
        /// <summary>
        /// 从客户端根场景（root）获取当前地图中的本地玩家主角 Unit。
        /// </summary>
        public static Unit GetMyUnitFromClientScene(Scene root)
        {
            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            if (playerComponent == null)
            {
                Log.Error($"playerComponent null");
                return default;
            }

            Scene currentScene = root.GetComponent<CurrentScenesComponent>()?.Scene;
            if (currentScene != null)
            {
                Log.Error($"currentScene null");
                return default;
            }

            var unit = currentScene.GetComponent<UnitComponent>().Get(playerComponent.MyId);
            if (unit == null)
            {
                return default;
            }

            return unit;
        }

        /// <summary>
        /// 从当前地图子场景（currentScene）获取本地玩家主角 Unit。
        /// </summary>
        public static Unit GetMyUnitFromCurrentScene(Scene currentScene)
        {
            PlayerComponent playerComponent = currentScene.Root().GetComponent<PlayerComponent>();
            return currentScene.GetComponent<UnitComponent>().Get(playerComponent.MyId);
        }

        /// <summary>
        /// 判断指定 Unit 是否为当前客户端登录的玩家主角。
        /// </summary>
        public static bool IsMyUnit(this Unit self)
        {
            if (self.Root().GetComponent<PlayerComponent>().MyId == self.Id)
            {
                return true;
            }

            return false;
        }
    }
}