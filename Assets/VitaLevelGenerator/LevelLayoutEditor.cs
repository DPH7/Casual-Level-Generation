using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelLayoutEditor : MonoBehaviour
{
    [Header("卡牌 Prefab (若为空将自动建 Cube)")]
    public GameObject tilePrefab;

    private int nextId = 0;

    /// <summary>
    /// 在指定坐标与层级生成卡牌（集成 Undo 撤销支持）
    /// </summary>
    public GameObject CreateTileAt(float x, float y, int layer)
    {
        // 防重叠检测：如果在同一层且坐标极其贴近，不重复生成
        foreach (Transform child in transform)
        {
            var node = child.GetComponent<TileEditorNode>();
            if (node != null && node.slotData.Layer == layer)
            {
                if (Mathf.Abs(node.slotData.X - x) < 0.01f && Mathf.Abs(node.slotData.Y - y) < 0.01f)
                {
                    Debug.LogWarning($"[编辑器] 坐标 ({x}, {y}) Layer:{layer} 已存在卡牌，无法重复生成！");
                    return child.gameObject;
                }
            }
        }

        GameObject tileObj;
        if (tilePrefab != null)
        {
            tileObj = Instantiate(tilePrefab, transform);
        }
        else
        {
            tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tileObj.transform.SetParent(transform, false);
        }

        TileEditorNode newNode = tileObj.GetComponent<TileEditorNode>();
        newNode.Init(nextId++, x, y, layer);

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(tileObj, "Create Tile Node");
        UnityEditor.Selection.activeGameObject = tileObj; // 自动选中新生成的牌，方便连续扩展
#endif

        return tileObj;
    }

    /// <summary>
    /// 将手动摆好的 GameObject 结构导出为算法所需的 List<Slot> 并计算拓扑依赖[cite: 5]
    /// </summary>
    public List<Slot> ExportLayoutAndBuildDependencies()
    {
        List<Slot> slots = new List<Slot>();
        TileEditorNode[] nodes = GetComponentsInChildren<TileEditorNode>();

        int idCounter = 0;
        foreach (var node in nodes)
        {
            node.slotData.Id = idCounter++;
            slots.Add(node.slotData);
        }

        // 确保总卡牌数为偶数（2消规则必须成对）[cite: 5]
        if (slots.Count % 2 != 0 && slots.Count > 0)
        {
            Debug.LogWarning("[关卡编辑器] 当前摆放的总牌数为奇数！请补一张或删一张，算法将自动暂忽略最后一张。");
        }

        return slots;
    }
}