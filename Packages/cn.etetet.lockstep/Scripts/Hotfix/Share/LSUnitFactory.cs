namespace ET
{
    /// <summary>
    /// 帧同步逻辑单位（<see cref="LSUnit"/>）创建工厂
    /// </summary>
    public static partial class LSUnitFactory
    {
        /// <summary>
        /// 根据单位初始信息在 <see cref="LSWorld"/> 中创建 <see cref="LSUnit"/> 实体，初始化定点数坐标/旋转并挂载 <see cref="LSInputComponent"/>
        /// </summary>
        /// <param name="lsWorld">所属帧同步世界</param>
        /// <param name="unitInfo">单位初始同步信息</param>
        /// <returns>创建的帧同步逻辑单位</returns>
        public static LSUnit Init(LSWorld lsWorld, LockStepUnitInfo unitInfo)
        {
	        LSUnitComponent lsUnitComponent = lsWorld.GetComponent<LSUnitComponent>();
	        LSUnit lsUnit = lsUnitComponent.AddChildWithId<LSUnit>(unitInfo.PlayerId);
			
	        lsUnit.Position = unitInfo.Position;
	        lsUnit.Rotation = unitInfo.Rotation;

			lsUnit.AddComponent<LSInputComponent>();
            return lsUnit;
        }
    }
}
