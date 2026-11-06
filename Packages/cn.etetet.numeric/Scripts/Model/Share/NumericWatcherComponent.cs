using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 单个数值监听器的注册信息（包含生效场景类型与监听器实例）。
    /// </summary>
    public struct NumericWatcherInfo
    {
        /// <summary>
        /// 监听器生效的 <see cref="ET.SceneType"/>。
        /// </summary>
        public int SceneType { get; }

        /// <summary>
        /// 数值监听器处理实例。
        /// </summary>
        public INumericWatcher INumericWatcher { get; }

        /// <summary>
        /// 构造数值监听器注册信息。
        /// </summary>
        public NumericWatcherInfo(int sceneType, INumericWatcher numericWatcher)
        {
            this.SceneType = sceneType;
            this.INumericWatcher = numericWatcher;
        }
    }

    /// <summary>
    /// 监视数值变化组件,分发监听
    /// </summary>
    [CodeProcess]
    [AllowInstance]
    public class NumericWatcherComponent : Singleton<NumericWatcherComponent>, ISingletonAwake
    {
        /// <summary>
        /// 按 <see cref="NumericType"/> 分类存储的所有数值监听器列表。
        /// </summary>
        private readonly Dictionary<NumericType, List<NumericWatcherInfo>> allWatchers = new();
        
        /// <summary>
        /// 扫描所有标注了 <see cref="NumericWatcherAttribute"/> 的类型并注册到分发字典中。
        /// </summary>
        public void Awake()
        {
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof(NumericWatcherAttribute));
            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(NumericWatcherAttribute), false);

                foreach (object attr in attrs)
                {
                    NumericWatcherAttribute numericWatcherAttribute = (NumericWatcherAttribute)attr;
                    INumericWatcher obj = (INumericWatcher)Activator.CreateInstance(type);
                    NumericWatcherInfo numericWatcherInfo = new(numericWatcherAttribute.SceneType, obj);
                    if (!this.allWatchers.ContainsKey(numericWatcherAttribute.NumericType))
                    {
                        this.allWatchers.Add(numericWatcherAttribute.NumericType, new List<NumericWatcherInfo>());
                    }
                    this.allWatchers[numericWatcherAttribute.NumericType].Add(numericWatcherInfo);
                }
            }
        }
        
        /// <summary>
        /// 分发数值变化事件，依次调用匹配当前 <see cref="Unit"/> 所属场景类型的 <see cref="INumericWatcher"/>。
        /// </summary>
        public void Run(Unit unit, NumbericChange args)
        {
            List<NumericWatcherInfo> list;
            if (!this.allWatchers.TryGetValue(args.NumericType, out list))
            {
                return;
            }

            int unitDomainSceneType = unit.IScene.SceneType;
            foreach (NumericWatcherInfo numericWatcher in list)
            {
                if (!SceneTypeSingleton.IsSame(numericWatcher.SceneType, unitDomainSceneType))
                {
                    continue;
                }
                numericWatcher.INumericWatcher.Run(unit, args);
            }
        }
    }
}
