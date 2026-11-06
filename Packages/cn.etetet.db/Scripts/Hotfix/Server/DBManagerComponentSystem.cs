using System;
using MongoDB.Driver;

namespace ET.Server
{
    /// <summary>
    /// 多区服数据库管理组件（<see cref="DBManagerComponent"/>）的业务系统类。
    /// <para>负责根据区服编号（<c>zone</c>）从 <see cref="StartZoneConfigCategory"/> 获取数据库连接配置，按需创建并缓存每个区服对应的 <see cref="DBComponent"/> 子实体。</para>
    /// </summary>
    public static partial class DBManagerComponentSystem
    {
        private const string ZoneDBNameSeparator = "_zone_";
        private const int ZoneDBNameWidth = 4;

        /// <summary>
        /// 获取指定区服编号（<paramref name="zone"/>）对应的 <see cref="StartZoneConfig"/> 配置。
        /// </summary>
        private static StartZoneConfig GetStartZoneConfig(this DBManagerComponent self, int zone)
        {
            StartZoneConfigCategory startZoneConfigCategory = self.Fiber().GetSingleton<StartZoneConfigCategory>();
            StartZoneConfig startZoneConfig = startZoneConfigCategory.Get(zone);
            return startZoneConfig;
        }

        /// <summary>
        /// 获取指定区服格式化后的 MongoDB 数据库名称（格式如：<c>{DBName}_zone_0001</c>）。
        /// </summary>
        public static string GetZoneDBName(this DBManagerComponent self, int zone)
        {
            StartZoneConfigCategory startZoneConfigCategory =
                    self.Fiber().GetSingleton<StartZoneConfigCategory>();

            StartZoneConfig zoneConfig = startZoneConfigCategory.Get(zone);

            if (string.IsNullOrEmpty(zoneConfig.DBName))
            {
                return string.Empty;
            }

            string formattedZone = zone.ToString($"D{ZoneDBNameWidth}");
            return $"{zoneConfig.DBName}{ZoneDBNameSeparator}{formattedZone}";
        }

        /// <summary>
        /// 异步删除（Drop）指定区服的整个 MongoDB 数据库（通常用于测试环境清理数据）。
        /// </summary>
        public static async ETTask DropDB(this DBManagerComponent self, int zone)
        {
            StartZoneConfig startZoneConfig = self.GetStartZoneConfig(zone);
            string dbName = self.GetZoneDBName(zone);
            if (string.IsNullOrEmpty(startZoneConfig.DBConnection) || string.IsNullOrEmpty(dbName))
            {
                return;
            }

            MongoClient mongoClient = new(startZoneConfig.DBConnection);
            await mongoClient.DropDatabaseAsync(dbName);
            await ETTask.CompletedTask;
        }
        
        /// <summary>
        /// 获取指定区服的 <see cref="DBComponent"/> 实例（若尚未创建则以 <paramref name="zone"/> 为实体 ID 自动创建并初始化连接）。
        /// </summary>
        public static DBComponent GetZoneDB(this DBManagerComponent self, int zone)
        {
            DBComponent dbComponent = self.GetChild<DBComponent>(zone);
            if (dbComponent != null)
            {
                return dbComponent;
            }

            StartZoneConfig startZoneConfig = self.GetStartZoneConfig(zone);
            if (string.IsNullOrEmpty(startZoneConfig.DBConnection))
            {
                throw new Exception($"zone: {zone} not found mongo connect string");
            }

            string dbName = self.GetZoneDBName(zone);
            if (string.IsNullOrEmpty(dbName))
            {
                throw new Exception($"zone: {zone} not found mongo db name");
            }

            dbComponent = self.AddChildWithId<DBComponent, string, string>(zone, startZoneConfig.DBConnection, dbName);
            return dbComponent;
        }
    }
}
