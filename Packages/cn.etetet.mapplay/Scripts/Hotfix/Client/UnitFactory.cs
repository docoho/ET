using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 客户端地图单位（Unit）创建工厂类：根据服务端同步的 UnitInfo 组装客户端 Unit 实体并发布 AfterUnitCreate 事件。
    /// </summary>
    public static partial class UnitFactory
    {
        /// <summary>
        /// 在客户端当前地图场景（currentScene）中创建并初始化一个 Unit 实体。
        /// </summary>
        public static Unit Create(Scene currentScene, UnitInfo unitInfo)
        {
	        UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
	        Unit unit = unitComponent.AddChildWithId<Unit, int>(unitInfo.UnitId, unitInfo.ConfigId);
	        unitComponent.Add(unit);
	        
	        unit.Position = unitInfo.Position;
	        unit.Forward = unitInfo.Forward;
	        unit.UnitType = (UnitType)unitInfo.Type;
	        NumericComponent numericComponent = unit.AddComponent<NumericComponent>();

			foreach (var kv in unitInfo.KV)
			{
				numericComponent.SetNoEvent(kv.Key, kv.Value);
			}
	        
			unit.AddComponent<TurnComponent>();
	        unit.AddComponent<MoveComponent>();
	        if (unitInfo.MoveInfo != null)
	        {
		        if (unitInfo.MoveInfo.Points.Count > 0)
				{
					unitInfo.MoveInfo.Points[0] = unit.Position;
					unit.MoveToAsync(unitInfo.MoveInfo.Points).Coroutine();
				}
	        }

	        unit.AddComponent<BuffComponent>();
	        unit.AddComponent<ObjectWait>();
	        
	        EventSystem.Instance.Publish(unit.Scene(), new AfterUnitCreate() {Unit = unit});
            return unit;
        }
    }
}
