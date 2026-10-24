using System;
using System.Net;
using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 程序启动入口：负责主 Fiber 创建前的全局初始化
    /// 流程：注册序列化器 -> 创建框架基础单例 -> 加载配置表 -> 创建主 Fiber
    /// </summary>
    public static class Entry
    {
        /// <summary>
        /// 同步入口，供服务器/客户端 Loader 调用，启动异步初始化流程
        /// </summary>
        public static void Start()
        {
            // Coroutine() 类似 UniTask 的 Forget()：启动异步任务后立即返回，不阻塞当前调用
            StartAsync().Coroutine();
        }

        /// <summary>
        /// 异步初始化流程
        /// </summary>
        private static async ETTask StartAsync()
        {
            // Windows 平台调用 timeBeginPeriod(1) 将系统定时器精度提升到 1ms，
            // 保证 Thread.Sleep/定时器更精确；非 Windows 平台无操作
            WinPeriod.Init();

            // 注册Mongo type：注册消息、实体等类型的多态映射，供 MongoDB 存取使用
            MongoRegister.Init();

            // 注册 MemoryPack 自定义 Formatter（Entity 的 Children/Components 集合），
            // 用于网络消息与 Entity 的二进制序列化
            MemoryPackRegister.Init();

            // 注册Entity序列化器：注册编译期(SourceGenerator)生成的 ETEntitySerializeFormatter，
            // 支持所有 Entity 子类的多态序列化/反序列化
            EntitySerializeRegister.Init();

            // Unity.Mathematics 数学结构体没有 Mongo 原生序列化器，
            // 手动注册后才能随实体数据(如 Position/Rotation)一起存入数据库
            MongoRegister.RegisterStruct<float2>();
            MongoRegister.RegisterStruct<float3>();
            MongoRegister.RegisterStruct<float4>();
            MongoRegister.RegisterStruct<quaternion>();

            // ---------- 创建框架级基础单例（挂在世界根 World 上，进程唯一） ----------

            // 场景类型单例：反射 SceneType 类(各包以 partial 扩展)的常量，
            // 建立 "场景名 <-> 场景类型Id" 的双向映射
            World.Instance.AddSingleton<SceneTypeSingleton, Type>(typeof(SceneType));
            // 对象池：复用对象，减少 GC
            World.Instance.AddSingleton<ObjectPool>();
            // 全局唯一 Id 生成器
            World.Instance.AddSingleton<IdGenerater>();
            // 协议号映射：网络消息类型 <-> Opcode
            World.Instance.AddSingleton<OpcodeType>();
            // 数值属性枚举：建立 NumericType 枚举值 <-> 属性名 的映射(数值系统用)
            World.Instance.AddSingleton<NumericTypeEnum>();
            // 跨 Fiber/跨线程的消息投递队列(生产者-消费者模型)：
            // Send() 投递到目标 Fiber 的队列，目标 Fiber 在 Update 循环中批量取出处理
            World.Instance.AddSingleton<MessageQueue>();

            // DEBUG 消息日志器：被 AddIgnore 的消息类型不打收发日志(不影响消息本身收发)，
            // 此处屏蔽高频心跳与通用响应，避免刷屏；Release 下 [Conditional("DEBUG")] 使其零开销
            LogMsg logMsg = World.Instance.AddSingleton<LogMsg>();
            logMsg.AddIgnore(typeof(C2G_Ping));        // 客户端->网关 心跳请求
            logMsg.AddIgnore(typeof(G2C_Ping));        // 网关->客户端 心跳响应
            logMsg.AddIgnore(typeof(MessageResponse)); // 通用 RPC 响应


            // 创建需要reload的code singleton
            // 扫描所有程序集中带 [CodeProcess] 特性的类型(EventSystem、EntitySystemSingleton、
            // MessageSessionDispatcher 等)，实例化并 Awake 后注册为 World 单例；
            // 这些单例会随热更新代码一起重建
            CodeTypes.Instance.CodeProcess();

            // 加载全部 Luban 配置表：加载 ET.Config.dll(服务器读 ./Bin，客户端走资源系统)，
            // 由其中的工厂类创建配置单例注册进 World，并解析表间引用
            await World.Instance.AddSingleton<ConfigLoader>().LoadAsync();
            // 导航网格组件：服务端寻路支持
            World.Instance.AddSingleton<NavmeshComponent>();

            // Options 单例来自命令行参数解析，此处按场景名查出场景类型 Id(查不到会抛异常)
            int sceneType = SceneTypeSingleton.Instance.GetSceneType(Options.Instance.SceneName);
            // 创建本进程唯一的主 Fiber，名称 "场景名@进程号@副本索引" 仅用于日志与监控；
            // 创建后通过 FiberInit 事件按 sceneType 分发到对应的 [Invoke] 处理器，
            // 在 Fiber 内构建 Root Scene 并挂载组件，正式进入各场景自己的初始化流程
            await FiberManager.Instance.CreateMainFiber(sceneType, $"{Options.Instance.SceneName}@{Options.Instance.Process}@{Options.Instance.ReplicaIndex}");
        }
    }
}
