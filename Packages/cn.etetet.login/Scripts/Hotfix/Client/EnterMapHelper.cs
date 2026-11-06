using System;


namespace ET.Client
{
    /// <summary>
    /// 客户端请求进入地图的辅助工具类。
    /// </summary>
    public static partial class EnterMapHelper
    {
        /// <summary>
        /// 向网关发送 C2G_EnterMap 请求，等待场景切换信号（Wait_SceneChangeFinish）完成后发布 EnterMapFinish 事件。
        /// </summary>
        public static async ETTask EnterMapAsync(Scene root)
        {
            try
            {
                EntityRef<Scene> rootRef = root;
                G2C_EnterMap g2CEnterMap = await root.GetComponent<ClientSenderComponent>().Call(C2G_EnterMap.Create()) as G2C_EnterMap;
                // 等待场景切换完成
                root = rootRef;
                await root.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
                root = rootRef;
                EventSystem.Instance.Publish(root, new EnterMapFinish());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }	
        }
    }
}