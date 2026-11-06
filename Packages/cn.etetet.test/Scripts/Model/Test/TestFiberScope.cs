using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试专用隔离 Fiber 作用域（实现 IAsyncDisposable），为每个测试用例自动分配独立 Zone 与数据库配置，并在 await using 结束时自动销毁测试 Fiber。
    /// </summary>
    public struct TestFiberScope : IAsyncDisposable
    {
        /// <summary>
        /// 当前测试用例创建的隔离 Fiber 实例
        /// </summary>
        public Fiber TestFiber { get; private set; }
        private readonly Fiber fiber;

        public TestFiberScope(Fiber fiber)
        {
            this.fiber = fiber;
            this.TestFiber = null;
        }

        /// <summary>
        /// 分配独立测试区服（Zone）、准备 StartZoneConfig 模板，并异步创建指定场景类型的隔离测试 Fiber
        /// </summary>
        public static async ETTask<TestFiberScope> Create(Fiber fiber, int sceneType, string testName)
        {
            TestFiberScope scope = new(fiber);
            int zone = AllocateZone(fiber);
            EnsureStartZoneConfig(fiber, zone);
            scope.TestFiber = await fiber.CreateFiber(zone, IdGenerater.Instance.GenerateId(), sceneType, testName);
            if (sceneType == SceneType.TestEmpty)
            {
                scope.TestFiber.AddSingleton<ProcessFiberAddressSingleton>();
            }
            return scope;
        }

        public async ValueTask DisposeAsync()
        {
            if (this.fiber == null || this.TestFiber == null)
            {
                return;
            }

            // 注意这里因为是在ValueTask里面，ValueTask不带上下文，所以必须设置上下文，否则会卡住await无法回调回来
            await fiber.RemoveFiber(this.TestFiber.Id).NewContext(null);
        }

        private static int AllocateZone(Fiber fiber)
        {
            TestZoneAllocatorComponent allocator = GetAllocator(fiber);
            int zone = allocator.Zone;
            if (zone > FiberIdHelper.MaxZone)
            {
                throw new Exception($"test zone exhausted: {zone}");
            }

            allocator.Zone = zone + 1;
            return zone;
        }

        private static void EnsureStartZoneConfig(Fiber fiber, int zone)
        {
            if (fiber == null)
            {
                throw new ArgumentNullException(nameof(fiber));
            }

            FiberIdHelper.ValidateZone(zone);

            StartZoneConfigCategory category = fiber.GetSingleton<StartZoneConfigCategory>();
            if (category == null)
            {
                throw new Exception("StartZoneConfigCategory is not initialized before creating test fiber.");
            }

            lock (category)
            {
                if (category.GetOrDefault(zone) != null)
                {
                    return;
                }

                StartZoneConfig template = GetTemplateConfig(category);
                category.GetAll()[zone] = new StartZoneConfig(zone, template.ZoneType, template.DBConnection, template.DBName);
            }
        }

        private static StartZoneConfig GetTemplateConfig(StartZoneConfigCategory category)
        {
            StartZoneConfig template = category.GetOrDefault(0);
            if (IsUsableTemplate(template))
            {
                return template;
            }

            Dictionary<int, StartZoneConfig> configs = category.GetAll();
            foreach (StartZoneConfig config in configs.Values)
            {
                if (IsUsableTemplate(config))
                {
                    return config;
                }
            }

            throw new Exception("StartZoneConfigCategory has no usable DB template for test zone mock.");
        }

        private static bool IsUsableTemplate(StartZoneConfig config)
        {
            return config != null &&
                   !string.IsNullOrEmpty(config.DBConnection) &&
                   !string.IsNullOrEmpty(config.DBName);
        }

        private static TestZoneAllocatorComponent GetAllocator(Fiber fiber)
        {
            TestZoneAllocatorComponent allocator = fiber?.Root?.GetComponent<TestZoneAllocatorComponent>();
            if (allocator == null)
            {
                throw new Exception("TestZoneAllocatorComponent is not initialized on test fiber before running tests");
            }

            return allocator;
        }
    }
}
