using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 场景类型（<c>SceneType</c>）映射与匹配管理单例。
    /// 提供场景类型编号与名称字符串的互转，以及事件/消息分发时的场景类型匹配判断（<see cref="IsSame"/>）。
    /// </summary>
    public class SceneTypeSingleton: EnumSingleton<SceneTypeSingleton>, ISingletonAwake<Type>
    {
        /// <summary>
        /// 判断两个 <c>SceneType</c> 是否匹配：
        /// 当两者相等，或者其中任意一方为 <c>0</c>（即 <c>SceneType.All = 0</c>，通配所有场景）时返回 true。
        /// </summary>
        public static bool IsSame(int a, int b)
        {
            if (a == b)
            {
                return true;
            }

            if (a == 0)
            {
                return true;
            }

            if (b == 0)
            {
                return true;
            }

            return false;
        }
        
        public void Awake(Type type)
        {
            Init(type);
        }
		
        /// <summary>
        /// 根据场景类型编号获取可读的场景类型名称（如 "Gate"、"Map"）。
        /// </summary>
        public string GetSceneName(int sceneType)
        {
            return this.GetStringByValue(sceneType);
        }
		
        /// <summary>
        /// 根据场景类型名称解析对应的场景类型编号（常用于解析 StartSceneConfig 配置表中的 SceneType 字符串）。
        /// </summary>
        public int GetSceneType(string sceneName)
        {
            int type = this.GetValueByName(sceneName);
            if (type == 0)
            {
                throw new Exception($"not found scene type: {type} {sceneName}");
            }
            return type;
        }

        /// <summary>
        /// 获取所有已注册的 SceneType 编号到名称的字典。
        /// </summary>
        public Dictionary<int, string> GetAll()
        {
            return this.enumValueString.GetAll();
        }
    }
}