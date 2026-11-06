namespace ET
{
    /// <summary>
    /// 宠物组件：挂载在宠物 Unit 上，记录该宠物所属主人的 UnitId（OwnerId）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class PetComponent: Entity, IAwake
    {
        /// <summary>宠物主人的 Unit.Id</summary>
        public long OwnerId { get; set; }
    }
}