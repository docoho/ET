using System;
using System.Collections.Generic;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace ET
{
    /// <summary>
    /// MongoDB BSON 序列化类型映射与约定注册器。
    /// </summary>
    public static class MongoRegister
    {
        /// <summary>
        /// 为指定结构体类型 <typeparamref name="T"/> 注册通用反射 BSON 序列化器 <see cref="StructBsonSerialize{TValue}"/>。
        /// </summary>
        public static void RegisterStruct<T>() where T : struct
        {
            BsonSerializer.RegisterSerializer(typeof (T), new StructBsonSerialize<T>());
        }
        
        /// <summary>
        /// 初始化 MongoDB BSON 序列化注册表：重置旧注册表、注册核心集合序列化器、设置忽略多余字段约定，并自动映射所有继承自 <see cref="Object"/> 的类。
        /// </summary>
        public static void Init()
        {
            // 清理老的数据
            MethodInfo createSerializerRegistry = typeof (BsonSerializer).GetMethod("CreateSerializerRegistry", BindingFlags.Static | BindingFlags.NonPublic);
            createSerializerRegistry.Invoke(null, Array.Empty<object>());
            MethodInfo registerIdGenerators = typeof (BsonSerializer).GetMethod("RegisterIdGenerators", BindingFlags.Static | BindingFlags.NonPublic);
            registerIdGenerators.Invoke(null, Array.Empty<object>());
            
            ObjectSerializer objectSerializer = new(_ => true);
            BsonSerializer.RegisterSerializer(objectSerializer);
            
            BsonSerializer.RegisterSerializer(typeof(ComponentsCollection), new BsonComponentsCollectionSerializer());
            BsonSerializer.RegisterSerializer(typeof(ChildrenCollection), new BsonChildrenCollectionSerializer());
            
            // 自动注册IgnoreExtraElements
            ConventionPack conventionPack = new() { new IgnoreExtraElementsConvention(true) };

            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            //RegisterStruct<float2>();
            //RegisterStruct<float3>();
            //RegisterStruct<float4>();
            //RegisterStruct<quaternion>();
            //RegisterStruct<FP>();
            //RegisterStruct<TSVector>();
            //RegisterStruct<TSVector2>();
            //RegisterStruct<TSVector4>();
            //RegisterStruct<TSQuaternion>();
            //RegisterStruct<LSInput>();
#if UNITY_EDITOR
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    RegisterClass(type);
                }
            }
#else
            foreach (Type type in CodeTypes.Instance.GetTypes().Values)
            {
                RegisterClass(type);
            }
#endif
        }

        /// <summary>
        /// 将继承自 <see cref="Object"/> 的非泛型类注册到 <see cref="BsonClassMap"/>，并以 <see cref="Type.FullName"/> 作为多态鉴别器（Discriminator）。
        /// </summary>
        private static void RegisterClass(Type type)
        {
            if (!type.IsSubclassOf(typeof(Object)))
            {
                return;
            }

            if (type.IsGenericType)
            {
                return;
            }

            if (BsonClassMap.IsClassMapRegistered(type))
            {
                return;
            }

            BsonClassMap cm = new(type);
            cm.AutoMap();
            cm.SetDiscriminator(type.FullName);
            BsonClassMap.RegisterClassMap(cm);
        }
    }
}