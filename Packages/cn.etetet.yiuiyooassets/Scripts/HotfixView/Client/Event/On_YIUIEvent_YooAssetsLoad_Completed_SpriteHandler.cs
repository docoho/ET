using System;

namespace ET.Client
{
    /// <summary>
    /// 监听 YooAsset 加载器初始化完成事件，自动为 YIUILoadComponent 挂载图集精灵管理组件（YIUIYooAssetsSpriteComponent）并加载全局图集映射表。
    /// </summary>
    [Event(SceneType.All)]
    public class On_YIUIEvent_YooAssetsLoad_Completed_SpriteHandler : AEvent<Scene, YIUIEvent_YooAssetsLoad_Completed>
    {
        protected override async ETTask Run(Scene scene, YIUIEvent_YooAssetsLoad_Completed args)
        {
            var yiuiAtlas = args.YooAssetsLoadRef.Entity.GetParent<YIUILoadComponent>().AddComponent<YIUIYooAssetsSpriteComponent>();
            await yiuiAtlas.LoadAtlasAsync();
        }
    }
}