using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端 Unit 传送状态组件（记录传送起始坐标等信息）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class TransferComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 发起传送前的起始位置坐标。
        /// </summary>
        public float3 FromPos;
    }
}