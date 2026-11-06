using System;
using System.Collections.Generic;
using ET.Server;
using MongoDB.Driver;

namespace ET.Test
{
    /// <summary>
    /// 测试 Fiber 临时数据库清理组件系统，负责登记测试逻辑库并在测试结束后异步 Drop 掉对应的测试 Zone 数据库并轮询确认删除完成。
    /// </summary>
    [EntitySystemOf(typeof(TestFiberDatabaseCleanupComponent))]
    public static partial class TestFiberDatabaseCleanupComponentSystem
    {
        private const int VerifyRetryCount = 20;
        private const int VerifyRetryIntervalMs = 50;

        [EntitySystem]
        private static void Awake(this TestFiberDatabaseCleanupComponent self)
        {
            self.LogicalDbNames ??= new HashSet<string>(StringComparer.Ordinal);
            self.LogicalDbNames.Clear();
        }

        /// <summary>
        /// 登记需要在测试结束时自动清理的逻辑数据库名
        /// </summary>
        public static void RegisterLogicalDbName(this TestFiberDatabaseCleanupComponent self, string logicalDbName)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            if (string.IsNullOrWhiteSpace(logicalDbName))
            {
                throw new ArgumentException("logical db name is empty", nameof(logicalDbName));
            }

            self.LogicalDbNames.Add(logicalDbName);
        }

        [EnableGetComponent(typeof(TimerComponent))]
        [EnableGetComponent(typeof(CoroutineLockComponent))]
        [EnableGetComponent(typeof(DBManagerComponent))]
        public static async ETTask CleanupAsync(this TestFiberDatabaseCleanupComponent self, string reason, bool preserveInfrastructure = true)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            if (self.LogicalDbNames.Count == 0)
            {
                return;
            }

            Scene scene = self.Root();
            if (scene == null)
            {
                throw new Exception($"cleanup test fiber database failed: root scene is null, reason: {reason}");
            }

            TimerComponent timerComponent = scene.TimerComponent;
            DBManagerComponent dbManagerComponent = scene.GetComponent<DBManagerComponent>();

            EntityRef<DBManagerComponent> dbManagerRef = dbManagerComponent;
            EntityRef<TimerComponent> timerRef = timerComponent;
            int zone = scene.Fiber.Zone;

            HashSet<string> zoneDbNames = new(StringComparer.Ordinal);
            foreach (string logicalDbName in self.LogicalDbNames)
            {
                if (string.IsNullOrWhiteSpace(logicalDbName))
                {
                    continue;
                }

                zoneDbNames.Add(dbManagerComponent.GetZoneDBName(zone));
            }

            foreach (string zoneDbName in zoneDbNames)
            {
                dbManagerComponent = dbManagerRef;
                if (dbManagerComponent == null)
                {
                    throw new Exception($"cleanup test fiber database failed: db manager disposed, db: {zoneDbName}, reason: {reason}");
                }

                IMongoClient mongoClient = dbManagerComponent.GetZoneDB(zone).database.Client;
                await dbManagerComponent.DropDB(zone);

                timerComponent = timerRef;
                if (timerComponent == null)
                {
                    throw new Exception($"cleanup test fiber database failed: timer disposed, db: {zoneDbName}, reason: {reason}");
                }

                bool deleted = await WaitForDatabaseStateAsync(mongoClient, timerComponent, zoneDbName, false);
                if (!deleted)
                {
                    throw new Exception($"cleanup test fiber database failed: database still exists, db: {zoneDbName}, reason: {reason}");
                }
            }
        }

        private static async ETTask<bool> WaitForDatabaseStateAsync(IMongoClient mongoClient, TimerComponent timerComponent, string zoneDbName,
            bool shouldExist)
        {
            EntityRef<TimerComponent> timerRef = timerComponent;
            for (int retry = 0; retry < VerifyRetryCount; ++retry)
            {
                bool databaseExists = await ContainsDatabaseAsync(mongoClient, zoneDbName);
                if (databaseExists == shouldExist)
                {
                    return true;
                }

                timerComponent = timerRef;
                if (timerComponent == null)
                {
                    throw new Exception($"wait database state failed: timer disposed, db: {zoneDbName}, expectExists: {shouldExist}");
                }

                await timerComponent.WaitAsync(VerifyRetryIntervalMs);
            }

            return false;
        }

        private static async ETTask<bool> ContainsDatabaseAsync(IMongoClient mongoClient, string zoneDbName)
        {
            using IAsyncCursor<string> cursor = await mongoClient.ListDatabaseNamesAsync();
            List<string> databaseNames = await cursor.ToListAsync();
            return databaseNames.Contains(zoneDbName);
        }
    }
}
