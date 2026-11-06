using System;

namespace ET
{
    /// <summary>
    /// Actor 消息处理器标记特性，用于将 <see cref="IMHandler"/> 实现类绑定到指定的 <see cref="SceneType"/>。
    /// </summary>
    public class MessageHandlerAttribute: BaseAttribute
    {
        /// <summary>
        /// 处理器生效的场景类型（参见 <see cref="ET.SceneType"/>）。
        /// </summary>
        public int SceneType { get; }

        public MessageHandlerAttribute(int sceneType)
        {
            this.SceneType = sceneType;
        }
    }
}