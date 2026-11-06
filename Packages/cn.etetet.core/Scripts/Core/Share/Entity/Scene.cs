using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    /// <summary>
    /// 场景实体（Scene）：ECS 实体树的根节点或子场景容器。
    /// </summary>
    /// <remarks>
    /// 【初学者指南】
    /// 1. 在 ET10 中，每个 <see cref="Fiber"/>（纤程）在创建时都会拥有一个顶层的根场景 <c>Fiber.Root</c>（它本身就是一个 <see cref="Scene"/>）。
    /// 2. 所有的业务组件（如 <c>NetComponent</c>、<c>UnitComponent</c>）或子场景（如客户端的 <c>CurrentScene</c>）都是挂载在 <see cref="Scene"/> 下面的。
    /// 3. <see cref="SceneType"/> 决定了该场景的业务职能（例如 <c>Gate</c> 网关服、<c>Map</c> 地图服、<c>Realm</c> 登录服、<c>Client</c> 客户端主场景等），
    ///    事件系统（<see cref="EventSystem"/>）和消息处理器（<see cref="MessageDispatcher"/>）都会根据 <see cref="SceneType"/> 进行精准过滤与分发！
    /// </remarks>
    [EnableMethod]
    [ChildOf]
    public partial class Scene: Entity, IScene
    {
        /// <summary>
        /// 当前场景所属的 <see cref="ET.Fiber"/> 纤程实例。
        /// </summary>
        [BsonIgnore]
        public Fiber Fiber { get; set; }
        
        /// <summary>
        /// 场景名称（如 "Gate1"、"Map1"、"Client" 等，主要用于日志打印和 Hierarchy 视图调试）。
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 场景类型枚举值（对应各个包 <c>SceneType</c> 中定义的常量，用于区分不同服务器职责或客户端阶段）。
        /// </summary>
        public int SceneType
        {
            get;
            set;
        }

        public Scene()
        {
        }

        /// <summary>
        /// 创建一个顶层或指定 Fiber 下的 <see cref="Scene"/> 实例，并自动完成 <see cref="Entity.InstanceId"/> 分配与 <see cref="EntitySystem"/> 注册。
        /// </summary>
        /// <param name="fiber">所属纤程</param>
        /// <param name="id">场景实体持久化 ID</param>
        /// <param name="sceneType">场景类型编号</param>
        /// <param name="name">场景名称</param>
        public Scene(Fiber fiber, long id, int sceneType, string name)
        {
            this.Id = id;
            this.Name = name;
            this.InstanceId = fiber.NewInstanceId();
            this.SceneType = sceneType;
            this.IsNoDeserializeSystem = true;
            this.Fiber = fiber;
            this.IScene = this;
            this.IsRegister = true;
            Log.Info($"scene create: {SceneTypeSingleton.Instance.GetSceneName(this.SceneType)} {this.SceneType} {this.Id} {this.InstanceId}");
        }

        public override void Dispose()
        {
            base.Dispose();
            
            Log.Info($"scene dispose: {SceneTypeSingleton.Instance.GetSceneName(this.SceneType)} {this.SceneType} {this.Id} {this.InstanceId}");
        }
        
        protected override string ViewName
        {
            get
            {
                return $"{this.GetType().Name} ({this.Name})";
            }
        }
    }
}