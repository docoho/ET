using YIUIFramework;
using YooAsset;

namespace ET.Client
{
    /// <summary>
    /// YIUI 资源加载器初始化 Invoke 处理器，为 YIUILoadComponent 挂载并初始化 YIUIYooAssetsLoadComponent。
    /// </summary>
    [Invoke]
    public class YIUIInvokeYooAssetsHandler : AInvokeEntityHandler<YIUIInvokeEntity_LoadInitialize, ETTask<bool>>
    {
        public override async ETTask<bool> Handle(Entity entity, YIUIInvokeEntity_LoadInitialize args)
        {
            EntityRef<YIUILoadComponent> loadComponentRef = (YIUILoadComponent)entity;
            if (loadComponentRef.Entity == null)
            {
                Log.Error($"LoadComponent is null!");
                return false;
            }

            return await loadComponentRef.Entity.AddComponent<YIUIYooAssetsLoadComponent>().Initialize();
        }
    }

    /// <summary>
    /// YIUI 根据资源定位地址（Location）同步查询 YooAsset 资源信息（AssetInfo）的 Invoke 处理器。
    /// </summary>
    [Invoke(EYIUIInvokeType.Sync)]
    public class YIUIInvokeGetAssetsInfoSyncHandler : AInvokeEntityHandler<YIUIInvokeEntity_GetAssetInfo, AssetInfo>
    {
        public override AssetInfo Handle(Entity entity, YIUIInvokeEntity_GetAssetInfo args)
        {
            return entity.YIUILoad()?.GetComponent<YIUIYooAssetsLoadComponent>().GetAssetInfo(args.Location, args.AssetType);
        }
    }

    /// <summary>
    /// YIUI 根据资源 GUID 同步查询 YooAsset 资源信息（AssetInfo）的 Invoke 处理器。
    /// </summary>
    [Invoke(EYIUIInvokeType.Sync)]
    public class YIUIInvokeGetAssetInfoByGUIDSyncHandler : AInvokeEntityHandler<YIUIInvokeEntity_GetAssetInfoByGUID, AssetInfo>
    {
        public override AssetInfo Handle(Entity entity, YIUIInvokeEntity_GetAssetInfoByGUID args)
        {
            return entity.YIUILoad()?.GetComponent<YIUIYooAssetsLoadComponent>().GetAssetInfoByGUID(args.AssetGUID, args.AssetType);
        }
    }
}