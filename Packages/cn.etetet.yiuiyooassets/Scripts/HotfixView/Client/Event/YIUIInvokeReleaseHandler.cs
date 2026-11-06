using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// YIUI 同步释放 Sprite 资源的 Invoke 回调处理器，委托给 YIUIYooAssetsSpriteComponent 释放对应图集或散图引用。
    /// </summary>
    [Invoke(EYIUIInvokeType.Sync)]
    public class YIUIInvokeReleaseSpriteSyncHandler : AInvokeEntityHandler<YIUIInvokeEntity_ReleaseSprite>
    {
        public override void Handle(Entity entity, YIUIInvokeEntity_ReleaseSprite args)
        {
            EntityRef<YIUILoadComponent> loadRef = entity.YIUILoad();
            if (loadRef.Entity == null)
            {
                return;
            }

            loadRef.Entity.GetComponent<YIUIYooAssetsSpriteComponent>()?.ReleaseSprite(args.obj);
        }
    }
}