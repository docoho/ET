namespace ET.Server
{
    /// <summary>
    /// 服务端宠物与主人双向查询辅助工具类。
    /// </summary>
    public static class PetHelper
    {
        /// <summary>
        /// 根据宠物 Unit 查找其主人 Unit。
        /// </summary>
        public static Unit GetOwner(Unit pet)
        {
            long ownerId = pet.GetComponent<PetComponent>().OwnerId;
            Unit owner = pet.GetParent<UnitComponent>().Get(ownerId);
            return owner;
        }
        
        /// <summary>
        /// 根据主人 Unit 查找其当前召唤的宠物 Unit。
        /// </summary>
        public static Unit GetPet(Unit unit)
        {
            long petId = unit.GetComponent<UnitPetComponent>().PetId;
            Unit pet = unit.GetParent<UnitComponent>().Get(petId);
            return pet;
        }
    }
}