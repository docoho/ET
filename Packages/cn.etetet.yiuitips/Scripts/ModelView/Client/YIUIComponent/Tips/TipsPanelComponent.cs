using System;
using System.Collections.Generic;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 公共弹窗容器面板实体，维护按UI类型索引的异步对象池、活动弹窗引用计数与最后使用时间戳。
    /// 文档: https://lib9kmxvq7k.feishu.cn/wiki/OdNgwu0KsiyJ6NkK8vCcwbjbn1g
    /// </summary>
    public partial class TipsPanelComponent : Entity,
                                              IYIUIClose,
                                              IYIUIOpen<Type, Entity, ParamVo>,
                                              IYIUIOpen<Type, Entity, long, ParamVo>,
                                              IDynamicEvent<EventPutTipsView>
    {
        public Dictionary<Type, ObjAsyncCache<EntityRef<Entity>>> _AllPool         = new();
        public int                                                _RefCount        = 0;
        public Dictionary<Type, float>                            _AllPoolLastTime = new();
        public float                                              _AutoDestroyTime = 60f; //(暂定X秒没人使用自动摧毁)
        public HashSet<EntityRef<Entity>>                         _AllRefView      = new();
    }
}
