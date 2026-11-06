using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ET.Server
{
	/// <summary>
	/// MongoDB 数据库操作组件（<see cref="DBComponent"/>）的业务系统类。
	/// <para>封装对指定区服 MongoDB 数据库的集合创建、文档计数（Count）、存在性判断（Exist）、按 ID/Lambda/JSON 查询（Query）、原子替换（FindOneAndReplace）、批量插入（InsertBatch）、实体保存（Save）、删除（Remove）及索引创建（CreateIndex）操作。</para>
	/// <para>所有读写操作均通过 <c>CoroutineLockType.MongoDB</c> 配合 <c>id % DBComponent.TaskCount</c> 协程锁分片排队，保证同一实体 ID 的数据库读写严格串行化且不阻塞其他实体的并发请求。</para>
	/// </summary>
	[EntitySystemOf(typeof(DBComponent))]
    public static partial class DBComponentSystem
    {
	    /// <summary>
	    /// 初始化 MongoDB 客户端连接并获取指定数据库与已有集合列表。
	    /// </summary>
	    [EntitySystem]
	    private static void Awake(this DBComponent self, string dbConnection, string dbName)
		{
			self.mongoClient = new MongoClient(dbConnection);
			self.database = self.mongoClient.GetDatabase(dbName);
			self.GetCollectionNames();
		}

		/// <summary>
		/// 获取强类型集合对象（若未指定集合名则默认使用类型名称 <c>typeof(T).Name</c>）。
		/// </summary>
		private static IMongoCollection<T> GetCollection<T>(this DBComponent self, string collection = null)
		{
		    return self.database.GetCollection<T>(collection ?? GetDefaultCollectionName(typeof(T)));
		}

	    /// <summary>
	    /// 按集合名称获取基类为 <see cref="Entity"/> 的集合对象。
	    /// </summary>
	    private static IMongoCollection<Entity> GetCollection(this DBComponent self, string name)
	    {
		    return self.database.GetCollection<Entity>(name);
	    }

	    /// <summary>
	    /// 获取实体类型对应的默认 MongoDB 集合名称（即类名 <c>type.Name</c>）。
	    /// </summary>
	    private static string GetDefaultCollectionName(Type type)
	    {
		    if (type == null)
		    {
			    throw new ArgumentNullException(nameof(type));
		    }

		    return type.Name;
	    }
	    
	    /// <summary>
	    /// 拉取当前数据库中已存在的所有集合名称并缓存到 <see cref="DBComponent.CollectionDic"/>。
	    /// </summary>
	    public static void GetCollectionNames(this DBComponent self)
	    {
		    self.CollectionDic.Clear();
		    var list = self.database.ListCollectionNames().ToList();
		    foreach (string collection in list)
		    {
			    self.CollectionDic.Add(collection);
		    }
	    }
        
	    /// <summary>
	    /// 按实体类型 <typeparamref name="T"/> 的名称异步创建 MongoDB 集合。
	    /// </summary>
	    public static async ETTask CreateCollection<T>(this DBComponent self)
	    {
		    string name = GetDefaultCollectionName(typeof(T));
		    await self.CreateCollection(name);
	    }
        
	    /// <summary>
	    /// 按指定名称异步创建 MongoDB 集合（若集合已存在则自动忽略异常）。
	    /// </summary>
	    public static async ETTask CreateCollection(this DBComponent self, string name)
	    {
		    try
		    {
			    EntityRef<DBComponent> selfRef = self;
			    await self.database.CreateCollectionAsync(name);
			    self = selfRef;
		    }
		    catch (MongoCommandException e) when (e.CodeName == "NamespaceExists")
		    {
		    }

		    if (self != null)
		    {
			    self.CollectionDic.Add(name);
		    }
	    }
	    
	    #region Count

	    public static async ETTask<long> Count<T>(this DBComponent self, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    return await self.GetCollection<T>(collection).CountDocumentsAsync(d => true);
		    }
	    }

	    public static async ETTask<long> Count<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    { 
			    return await self.GetCollection<T>(collection).CountDocumentsAsync(filter);
		    }
	    }

	    #endregion
	    
	    #region Exist

	    public static async ETTask<bool> Exist<T>(this DBComponent self, string collection = null) where T : Entity
	    {
		    long count = await self.Count<T>(collection);

		    return count > 0;
	    }

	    public static async ETTask<bool> Exist<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null) where T : Entity
	    {
		    long count = await self.Count(filter, collection);

		    return count > 0;
	    }
        
	    // //是否存在表
	    public static async ETTask<bool> CollectionExistsAsync(this DBComponent self, string collectionName)
	    {
		    EntityRef<DBComponent> selfRef = self;
		    var filter = new BsonDocument("name", collectionName);
		    //filter by collection name
		    var collections = await self.database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
		    self = selfRef;
		    if (self == null)
		    {
			    return false;
		    }
		    //check for existence
		    bool exists = await collections.AnyAsync();
		    self = selfRef;
		    if (self == null)
		    {
			    return false;
		    }
		    if (exists)
		    {
			    self.CollectionDic.Add(collectionName);
		    }
		    else
		    {
			    self.CollectionDic.Remove(collectionName);
		    }
		    return exists;
	    }

	    #endregion
	    
	    #region Query

	    public static async ETTask<T> Query<T>(this DBComponent self, long id, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, id % DBComponent.TaskCount))
		    {
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(d => d.Id == id);
			    
			    return await cursor.FirstOrDefaultAsync();
		    }
	    }
	    
	    public static async ETTask<List<T>> Query<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null)
			    where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filter);

			    return await cursor.ToListAsync();
		    }
	    }

	    public static async ETTask<List<T>> Query<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter, string collection = null)
			    where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filter);

			    return await cursor.ToListAsync();
		    }
	    }
	    
	    public static async ETTask Query(this DBComponent self, long id, List<string> collectionNames, List<Entity> result)
	    {
		    if (collectionNames == null || collectionNames.Count == 0)
		    {
			    return;
		    }

		    EntityRef<DBComponent> selfRef = self;
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, id % DBComponent.TaskCount))
		    {
			    foreach (string collectionName in collectionNames)
			    {
				    self = selfRef;
				    IAsyncCursor<Entity> cursor = await self.GetCollection(collectionName).FindAsync(d => d.Id == id);

				    Entity e = await cursor.FirstOrDefaultAsync();

				    if (e == null)
				    {
					    continue;
				    }

				    result.Add(e);
			    }
		    }
	    }

	    public static async ETTask<List<T>> QueryJson<T>(this DBComponent self, string json, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filterDefinition);
			    return await cursor.ToListAsync();
		    }
	    }

	    public static async ETTask<List<T>> QueryJson<T>(this DBComponent self, long taskId, string json, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filterDefinition);
			    return await cursor.ToListAsync();
		    }
	    }

	    #endregion

	    #region CompareAndSwap

	    public static async ETTask<T> FindOneAndReplace<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter,
			    T replacement, bool isUpsert = false, string collection = null) where T : Entity
	    {
		    if (replacement == null)
		    {
			    throw new Exception($"find and replace entity is null: {typeof(T).Name}");
		    }

		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    FindOneAndReplaceOptions<T> options = new()
			    {
				    IsUpsert = isUpsert,
				    ReturnDocument = ReturnDocument.After,
			    };

			    return await self.GetCollection<T>(collection).FindOneAndReplaceAsync(filter, replacement, options);
		    }
	    }

	    public static async ETTask<T> FindOneAndReplace<T>(this DBComponent self, Expression<Func<T, bool>> filter,
			    T replacement, bool isUpsert = false, string collection = null) where T : Entity
	    {
		    return await self.FindOneAndReplace(RandomGenerator.RandInt64(), filter, replacement, isUpsert, collection);
	    }

	    #endregion

	    #region Insert

	    public static async ETTask InsertBatch<T>(this DBComponent self, IEnumerable<T> list, string collection = null) where T: Entity
	    {
		    if (collection == null)
		    {
			    collection = typeof (T).Name;
		    }
		    
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    await self.GetCollection(collection).InsertManyAsync(list);
		    }
	    }

	    #endregion

	    #region Save

	    public static async ETTask Save<T>(this DBComponent self, T entity, string collection = null) where T : Entity
	    {
		    if (entity == null)
		    {
			    Log.Error($"save entity is null: {typeof (T).Name}");

			    return;
		    }
		    
		    if (collection == null)
		    {
			    collection = entity.GetType().Name;
		    }

		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, entity.Id % DBComponent.TaskCount))
		    {
			    await self.GetCollection(collection).ReplaceOneAsync(d => d.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = true });
		    }
	    }

	    public static async ETTask Save<T>(this DBComponent self, long taskId, T entity, string collection = null) where T : Entity
	    {
		    if (entity == null)
		    {
			    Log.Error($"save entity is null: {typeof (T).Name}");

			    return;
		    }

		    if (collection == null)
		    {
			    collection = entity.GetType().Name;
		    }

		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    await self.GetCollection(collection).ReplaceOneAsync(d => d.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = true });
		    }
	    }

	    public static async ETTask Save(this DBComponent self, long id, List<Entity> entities)
	    {
		    if (entities == null)
		    {
			    Log.Error($"save entity is null");
			    return;
		    }

		    EntityRef<DBComponent> selfRef = self;
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, id % DBComponent.TaskCount))
		    {
			    foreach (Entity entity in entities)
			    {
				    if (entity == null)
				    {
					    continue;
				    }

				    self = selfRef;
				    long entityId = entity.Id;
				    await self.GetCollection(entity.GetType().Name).ReplaceOneAsync(d => d.Id == entityId, entity, new ReplaceOptions { IsUpsert = true });
			    }
		    }
	    }

	    public static async ETTask SaveNotWait<T>(this DBComponent self, T entity, long taskId = 0, string collection = null) where T : Entity
	    {
		    if (taskId == 0)
		    {
			    await self.Save(entity, collection);

			    return;
		    }

		    await self.Save(taskId, entity, collection);
	    }

	    #endregion

	    #region Remove
	    
	    public static async ETTask<long> Remove<T>(this DBComponent self, long id, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, id % DBComponent.TaskCount))
		    {
			    DeleteResult result = await self.GetCollection<T>(collection).DeleteOneAsync(d => d.Id == id);

			    return result.DeletedCount;
		    }
	    }

	    public static async ETTask<long> Remove<T>(this DBComponent self, long taskId, long id, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    DeleteResult result = await self.GetCollection<T>(collection).DeleteOneAsync(d => d.Id == id);

			    return result.DeletedCount;
		    }
	    }

	    public static async ETTask<long> Remove<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    DeleteResult result = await self.GetCollection<T>(collection).DeleteManyAsync(filter);

			    return result.DeletedCount;
		    }
	    }

	    public static async ETTask<long> Remove<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter, string collection = null)
			    where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    DeleteResult result = await self.GetCollection<T>(collection).DeleteManyAsync(filter);

			    return result.DeletedCount;
		    }
	    }

	    #endregion

	    #region Index

	    public static async ETTask CreateIndex<T>(this DBComponent self, params IndexKeysDefinition<T>[] keys) where T : Entity
	    {
		    if (keys == null)
		    {
			    return;
		    }

		    var indexModels = new List<CreateIndexModel<T>>();

		    foreach (var indexKeysDefinition in keys)
		    {
			    indexModels.Add(new CreateIndexModel<T>(indexKeysDefinition));
		    }

		    await self.GetCollection<T>().Indexes.CreateManyAsync(indexModels);
	    }

	    public static async ETTask CreateIndex<T>(this DBComponent self, string collection, params IndexKeysDefinition<T>[] keys) where T : Entity
	    {
		    if (keys == null)
		    {
			    return;
		    }

		    var indexModels = new List<CreateIndexModel<T>>();

		    foreach (var indexKeysDefinition in keys)
		    {
			    indexModels.Add(new CreateIndexModel<T>(indexKeysDefinition));
		    }

		    await self.GetCollection<T>(collection).Indexes.CreateManyAsync(indexModels);
	    }

	    public static async ETTask CreateIndex<T>(this DBComponent self, CreateIndexOptions indexOptions, params IndexKeysDefinition<T>[] keys) where T : Entity
	    {
		    if (keys == null)
		    {
			    return;
		    }

		    var indexModels = new List<CreateIndexModel<T>>();

		    foreach (var indexKeysDefinition in keys)
		    {
			    indexModels.Add(new CreateIndexModel<T>(indexKeysDefinition, indexOptions));
		    }

		    await self.GetCollection<T>().Indexes.CreateManyAsync(indexModels);
	    }

	    #endregion
    }
}
