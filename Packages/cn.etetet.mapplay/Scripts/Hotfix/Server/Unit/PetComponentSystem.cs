namespace ET.Server
{
    /// <summary>
    /// 宠物组件系统：挂载在宠物 Unit 上，提供查询其主人（Owner）Unit 的方法。
    /// </summary>
    [EntitySystemOf(typeof(PetComponent))]
    public static partial class PetComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PetComponent self)
        {

        }
        
        /// <summary>
        /// 获取该宠物所属的主人 Unit 实体。
        /// </summary>
        public static Unit GetOwner(this PetComponent self)
        {
            if (self.OwnerId == 0)
            {
                return null;
            }
            Unit pet = self.Parent.GetParent<UnitComponent>().Get(self.OwnerId);
            return pet;
        }
    }
}