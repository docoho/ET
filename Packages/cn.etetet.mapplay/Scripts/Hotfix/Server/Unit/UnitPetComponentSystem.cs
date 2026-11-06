namespace ET.Server
{
    /// <summary>
    /// 单位持有宠物组件系统：挂载在主人 Unit 上，提供获取其当前召唤宠物 Unit 的方法。
    /// </summary>
    [EntitySystemOf(typeof(UnitPetComponent))]
    public static partial class UnitPetComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UnitPetComponent self)
        {
        }
        
        /// <summary>
        /// 获取该主人当前召唤的宠物 Unit 实体。
        /// </summary>
        public static Unit GetPet(this UnitPetComponent self)
        {
            if (self.PetId == 0)
            {
                return null;
            }

            Unit pet = self.Parent.GetParent<UnitComponent>().Get(self.PetId);
            return pet;
        }
    }
}