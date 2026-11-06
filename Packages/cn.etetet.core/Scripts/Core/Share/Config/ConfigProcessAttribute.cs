namespace ET
{
    /// <summary>
    /// 配置处理器标记特性，用于标记不同类型配置（如 Luban 导表配置、代码内定义配置等）的加载与初始化处理器。
    /// </summary>
    public class ConfigProcessAttribute: BaseAttribute
    {
        /// <summary>
        /// 配置类型标识（参见 <see cref="ET.ConfigType"/>）。
        /// </summary>
        public int ConfigType { get; }

        public ConfigProcessAttribute(int configType = 0)
        {
            this.ConfigType = configType;
        }
    }
}