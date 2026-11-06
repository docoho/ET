using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// <see cref="MapConfigCategory"/> 配置表扩展，建立按地图名称（Name）快速检索 <see cref="MapConfig"/> 的字典索引。
    /// </summary>
    public partial class MapConfigCategory
    {
        private Dictionary<string, MapConfig> nameConfigs = new();

        /// <summary>
        /// 根据地图名称获取对应的 <see cref="MapConfig"/> 配置项。
        /// </summary>
        public MapConfig GetByName(string mapName)
        {
            return this.nameConfigs[mapName];
        }
        
        /// <summary>
        /// 配置表引用解析完成后的钩子，构建地图名称到配置对象的映射字典。
        /// </summary>
        partial void EndRef()
        {
            foreach (var kv in this.GetAll())
            {
                this.nameConfigs.Add(kv.Value.Name, kv.Value);
            }
        }
    }
}
