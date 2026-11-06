namespace ET
{
    /// <summary>
    /// 主人持有宠物组件：挂载在主人 Unit 上，记录当前召唤出的宠物 UnitId（PetId）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class UnitPetComponent: Entity, IAwake
    {
        /// <summary>当前召唤的宠物 Unit.Id</summary>
        public long PetId { get; set; }
    }
}