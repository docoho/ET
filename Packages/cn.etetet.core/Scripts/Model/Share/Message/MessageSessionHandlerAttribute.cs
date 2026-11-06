using System;

namespace ET
{
    /// <summary>
    /// Session 网络消息处理器标记特性，用于将 <see cref="IMessageSessionHandler"/> 实现类绑定到指定的 <see cref="SceneType"/>（如 <c>SceneType.Client</c> 或 <c>SceneType.Gate</c>）。
    /// </summary>
    public class MessageSessionHandlerAttribute: BaseAttribute
    {
        /// <summary>
        /// 处理器生效的场景类型（参见 <see cref="ET.SceneType"/>）。
        /// </summary>
        public int SceneType { get; }

        public MessageSessionHandlerAttribute(int sceneType)
        {
            this.SceneType = sceneType;
        }
    }
}