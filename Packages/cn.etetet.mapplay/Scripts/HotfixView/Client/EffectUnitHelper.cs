using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端单位特效实例化辅助工具类。
    /// </summary>
    public static class EffectUnitHelper
    {
        /// <summary>
        /// 在目标 Unit 的指定骨骼绑点（BindPoint）上实例化特效预制体，并支持按 duration（毫秒）定时自动销毁。
        /// </summary>
        public static GameObject Create(Unit unit, BindPoint bindPoint, GameObject gameObject, bool worldPositionStays, int duration)
        {
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            BindPointComponent bindPointComponent = gameObjectComponent.GameObject.GetComponent<BindPointComponent>();

            Transform bindPointTransform = bindPointComponent.GetBindPoints()[bindPoint];

            GameObject effect;
            if (worldPositionStays)
            {
                effect = UnityEngine.Object.Instantiate(gameObject, bindPointTransform.position, gameObjectComponent.GameObject.transform.rotation);
            }
            else
            {
                effect = UnityEngine.Object.Instantiate(gameObject, bindPointTransform, false);
            }
            
            if (duration > 0)
            {
                UnityEngine.Object.Destroy(effect, duration / 1000f);
            }
            
            return effect;
        }
        
        /// <summary>
        /// 在世界空间的指定坐标与旋转处实例化特效预制体，并按 duration（毫秒）定时销毁。
        /// </summary>
        public static GameObject Create(GameObject gameObject, Vector3 pos, Quaternion quaternion, int duration)
        {
            GameObject effect = UnityEngine.Object.Instantiate(gameObject, pos, quaternion);
            effect.transform.SetParent(GameObject.Find("/Global/Unit").transform);
            
            if (duration > 0)
            {
                UnityEngine.Object.Destroy(effect, duration / 1000f);
            }
            return effect;
        }
    }
}