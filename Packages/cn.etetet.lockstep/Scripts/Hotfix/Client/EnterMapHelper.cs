using System;


namespace ET.Client
{
    /// <summary>
    /// 帧同步客户端进入地图与发起匹配的辅助工具类
    /// </summary>
    public static partial class EnterMapHelper
    {
        /// <summary>
        /// 请求进入普通地图场景并等待场景切换完成
        /// </summary>
        /// <param name="root">客户端根场景</param>
        public static async ETTask EnterMapAsync(Scene root)
        {
            try
            {
                EntityRef<Scene> rootRef = root;
                G2C_EnterMap g2CEnterMap = await root.GetComponent<ClientSenderComponent>().Call(C2G_EnterMap.Create()) as G2C_EnterMap;

                root = rootRef;
                // 等待场景切换完成
                await root.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();

                root = rootRef;
                EventSystem.Instance.Publish(root, new EnterMapFinish());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }	
        }
        
        /// <summary>
        /// 向网关发送开始帧同步匹配请求（<see cref="C2G_Match"/>）
        /// </summary>
        /// <param name="fiber">客户端纤程实例</param>
        public static async ETTask Match(Fiber fiber)
        {
            try
            {
                G2C_Match g2CEnterMap = await fiber.Root.GetComponent<ClientSenderComponent>().Call(C2G_Match.Create()) as G2C_Match;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }	
        }
    }
}