using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 服务端地图单位（Unit）创建工厂类：负责根据配置表初始化数值属性、位置朝向、移动/AOI/技能/Buff/背包/任务/寻路等组件，并启动初始 AI Buff。
    /// </summary>
    public static partial class UnitFactory
    {
        /// <summary>
        /// 在指定场景中创建并初始化一个服务端 Unit 实体。
        /// </summary>
        public static Unit Create(Scene scene, long id, int configId)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();

            Unit unit = unitComponent.AddChildWithId<Unit, int>(id, configId);
            UnitConfig unitConfig = unit.Config();

            unit.UnitType = unitConfig.UnitType;

            NumericComponent numericComponent = unit.AddComponent<NumericComponent>();
            foreach ((NumericType k, long v) in unitConfig.KV)
            {
                numericComponent.SetNoEvent(k, v);
            }

            if (unit.UnitType != UnitType.Player)
            {
                MapUnitConfig mapUnitConfig = scene.Fiber().GetSingleton<MapUnitConfigCategory>().Get((int)id);
                if (mapUnitConfig != null)
                {
                    foreach ((NumericType k, long v) in mapUnitConfig.KV)
                    {
                        numericComponent.SetNoEvent(k, v);
                    }
                }
            }

            unit.Position = new float3(numericComponent.GetAsFloat(NumericType.X), numericComponent.GetAsFloat(NumericType.Y), numericComponent.GetAsFloat(NumericType.Z));
            unit.Rotation = quaternion.Euler(0, math.radians(numericComponent.Get(NumericType.Yaw)), 0);

            unit.AddComponent<MoveComponent>();
            unit.AddComponent<TurnComponent>();
            unit.AddComponent<AOIEntity>();
            unit.AddComponent<TargetComponent>();
            unit.AddComponent<SpellComponent>();
            unit.AddComponent<BuffComponent>();

            switch (unit.UnitType)
            {
                case UnitType.Player:
                {
                    unit.AddComponent<ItemComponent>();
                    unit.AddComponent<QuestComponent>();
                    break;
                }
                case UnitType.Monster:
                {
                    unit.AddComponent<ThreatComponent>();
                    unit.AddComponent<PathfindingComponent, string>(scene.Name.GetSceneConfigName());
                    break;
                }
                case UnitType.NPC:
                {
                    break;
                }
                case UnitType.Virtual:
                {
                    break;
                }
            }

            int ai = numericComponent.GetAsInt(NumericType.AI);
            if (ai != 0)
            {
                BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), ai, null);
            }

            unitComponent.Add(unit);
            return unit;
        }

        /// <summary>
        /// 为指定主人（owner）创建宠物单位，并双向绑定 PetComponent 与 UnitPetComponent。
        /// </summary>
        public static Unit CreatePet(Scene scene, Unit owner, long id, int configId)
        {
            Unit pet = Create(scene, id, configId);
            pet.AddComponent<PetComponent>().OwnerId = owner.Id;

            UnitPetComponent unitPetComponent = owner.GetComponent<UnitPetComponent>() ?? owner.AddComponent<UnitPetComponent>();
            unitPetComponent.PetId = pet.Id;
            return pet;
        }
    }
}
