using MemoryPack;

namespace ET
{
    /// <summary>
    /// 帧同步单位操作输入组件（挂载在 <see cref="LSUnit"/> 上，存储该单位当前逻辑帧的操作输入并参与 <see cref="ILSUpdate"/> 帧更新）
    /// </summary>
    [ComponentOf(typeof(LSUnit))]
    [MemoryPackable]
    public partial class LSInputComponent: LSEntity, ILSUpdate, IAwake, ISerializeToEntity
    {
        /// <summary>
        /// 当前逻辑帧该单位的玩家输入数据
        /// </summary>
        public LSInput LSInput { get; set; }
    }
}