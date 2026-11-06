namespace ET.Client
{
    /// <summary>
    /// 服务端推送的装备更新通知（<see cref="M2C_UpdateEquipment"/>）处理器
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_UpdateEquipmentHandler : MessageHandler<Scene, M2C_UpdateEquipment>
    {
        /// <summary>
        /// 处理装备穿戴或卸下同步消息，更新客户端本地装备组件状态
        /// </summary>
        /// <param name="scene">客户端根场景</param>
        /// <param name="message">装备更新网络消息（SlotType 为 -1 表示卸下）</param>
        protected override async ETTask Run(Scene scene, M2C_UpdateEquipment message)
        {
            EquipmentComponent equipmentComponent = scene.GetComponent<EquipmentComponent>();
            if (equipmentComponent == null)
            {
                Log.Error("EquipmentComponent not found in scene");
                return;
            }

            // 更新装备数据
            equipmentComponent.UpdateEquipment(message.SlotType, message.ItemId, message.ConfigId);
            
            await ETTask.CompletedTask;
        }
    }
}