namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端场景切换开始事件
    /// </summary>
    public struct LSSceneChangeStart
    {
        /// <summary>
        /// 目标帧同步房间引用
        /// </summary>
        public EntityRef<Room> Room;
    }
    
    /// <summary>
    /// 帧同步客户端场景与房间世界初始化完成事件
    /// </summary>
    public struct LSSceneInitFinish
    {
    }
    
    /// <summary>
    /// 客户端根场景创建完成事件
    /// </summary>
    public struct AfterCreateClientScene
    {
    }
    
    /// <summary>
    /// 客户端当前子场景创建完成事件
    /// </summary>
    public struct AfterCreateCurrentScene
    {
    }

    /// <summary>
    /// 客户端应用启动初始化完成事件
    /// </summary>
    public struct AppStartInitFinish
    {
    }

    /// <summary>
    /// 客户端进入地图完成事件
    /// </summary>
    public struct EnterMapFinish
    {
        
    }
}