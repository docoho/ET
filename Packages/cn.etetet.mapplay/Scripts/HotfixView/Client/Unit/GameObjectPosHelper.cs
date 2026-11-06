using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端角色模型贴地高度校准辅助工具类。
    /// </summary>
    public static class GameObjectPosHelper
    {
        /// <summary>
        /// 从角色上方 100 米处向下发射物理射线检测 Map 图层，将 Transform 的 Y 坐标吸附到地表碰撞点。
        /// </summary>
        public static void OnTerrain(Transform transform)
        {
            // 贴地
            Ray ray = new(new Vector3(transform.position.x, transform.position.y + 100, transform.position.z), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200, LayerMask.GetMask("Map")))
            {
                transform.position = hit.point;
            }
        }
    }
}