using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelVisualizer : MonoBehaviour
{
    [Header("关卡数据")]
    public List<Slot> currentLayout = new List<Slot>();

    [Header("卡牌渲染配置")]
    public Vector3 tileSize = new Vector3(1.0f, 1.2f, 0.15f); // 牌的长宽高
    public Vector3 layerOffset = new Vector3(0, 0, -0.2f);     // Z 轴叠加深度间距
    public GameObject tilePrefab;                             // 可选：自定义卡牌 Prefab

    [Header("挂载根节点")]
    public Transform boardRoot;  // 存放棋盘上的卡牌
    public Transform holderRoot; // 存放 4 格暂存区

    [Header("暂存区配置 (4 格上限)")]
    public const int MaxHolderSize = 4;
    public List<TileController> holderTiles = new List<TileController>(); // 当前在暂存区中的牌
    public Vector3 holderStartPos = new Vector3(-1.8f, -4.0f, 0);          // 暂存区起点坐标
    public float holderSpacing = 1.2f;                                     // 暂存区槽位间距

    /// <summary>
    /// 根据 currentLayout 实例化真实的 GameObject 方块
    /// </summary>
    public void RenderBoard()
    {
        holderTiles.Clear();

        if (currentLayout == null || currentLayout.Count == 0) return;

        // 2. 渲染底部的 4 个暂存区槽位背景框
        RenderHolderSlots();

        // 3. 渲染棋盘卡牌
        foreach (var slot in currentLayout)
        {
            var name = $"Tile_{slot.Id}_L{slot.Layer}";
            var tileObj = transform.Find(name);
            if (tileObj)
            {
                // 确保挂载 Collider 以支持点击检测
                if (tileObj.GetComponent<Collider>() == null)
                {
                    tileObj.gameObject.AddComponent<BoxCollider>();
                }

                // 数字标签
                CreateTextLabel(tileObj.transform, (int)slot.AssignedType);
            }
        }
    }

    /// <summary>
    /// 渲染底部的 4 个暂存区半透明底框
    /// </summary>
    private void RenderHolderSlots()
    {
        for (int i = 0; i < MaxHolderSize; i++)
        {
            GameObject slotFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slotFrame.name = $"HolderSlot_{i}";
            slotFrame.transform.SetParent(holderRoot);
            slotFrame.transform.localPosition = holderStartPos + new Vector3(i * holderSpacing, 0, 0.1f);
            slotFrame.transform.localScale = new Vector3(tileSize.x * 1.05f, tileSize.y * 1.05f, 0.05f);

            // 渲染为灰白色半透明底框
            Renderer r = slotFrame.GetComponent<Renderer>();
            if (r != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor("_Color", new Color(0.8f, 0.8f, 0.8f, 0.5f));
                r.SetPropertyBlock(block);
            }
        }
    }

    /// <summary>
    /// 卡牌点击响应处理
    /// </summary>
    public void OnTileClicked(TileController tile)
    {
        // 从棋盘遮挡关系中移除（正向消除：其上层遮挡已被满足）
        tile.slotData.AssignedType = TileType.None;

        // 加入暂存区列表
        holderTiles.Add(tile);
        tile.transform.SetParent(holderRoot);

        // 重新排布暂存区中的卡牌位置
        UpdateHolderPositions();

        // 尝试执行 2 消匹配
        CheckAndMatchPairs();
    }

    /// <summary>
    /// 判断暂存区是否已满
    /// </summary>
    public bool IsHolderFull()
    {
        return holderTiles.Count >= MaxHolderSize;
    }

    /// <summary>
    /// 检查并消除相同的两个牌
    /// </summary>
    private void CheckAndMatchPairs()
    {
        // 统计相同牌型的卡牌
        var matchedGroup = holderTiles
            .GroupBy(t => t.slotData.AssignedType)
            .FirstOrDefault(g => g.Count() >= 2);

        if (matchedGroup != null)
        {
            // 找到一对相同的牌
            List<TileController> toRemove = matchedGroup.Take(2).ToList();

            foreach (var tile in toRemove)
            {
                holderTiles.Remove(tile);
                Destroy(tile.gameObject);
            }

            Debug.Log($"<color=green>[消除成功]</color> 匹配消除牌型: {(int)matchedGroup.Key}");

            // 消除后重新对齐暂存区
            UpdateHolderPositions();
        }

        // 检查游戏结束/失败条件
        CheckGameState();
    }

    /// <summary>
    /// 刷新暂存区卡牌位置
    /// </summary>
    private void UpdateHolderPositions()
    {
        for (int i = 0; i < holderTiles.Count; i++)
        {
            Vector3 targetPos = holderStartPos + new Vector3(i * holderSpacing, 0, 0);
            holderTiles[i].transform.localPosition = targetPos;
        }
    }

    /// <summary>
    /// 检查输赢状态
    /// </summary>
    private void CheckGameState()
    {
        // 1. 所有棋盘牌已消除完毕 -> 胜利
        bool allCleared = currentLayout.All(s => s.AssignedType == TileType.None) && holderTiles.Count == 0;
        if (allCleared && currentLayout.Count > 0)
        {
            Debug.Log("<color=yellow>🎉 恭喜通关！全部卡牌已成功消除！</color>");
            return;
        }

        // 2. 暂存区已满 4 格且无法进行任何消除 -> 失败
        if (IsHolderFull())
        {
            Debug.LogError("💥 暂存区已满 (4/4)，且没有可消除的牌对！游戏失败！");
        }
    }

    public void CreateTextLabel(Transform parentTile, int type)
    {
        // 确保挂载 Collider 以支持点击检测
        if (parentTile.GetComponent<Collider>() == null)
        {
            parentTile.gameObject.AddComponent<BoxCollider>();
        }
        
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(parentTile, false);
        textObj.transform.localPosition = new Vector3(0, 0, -0.51f);
        textObj.transform.localRotation = Quaternion.Euler(0, 0, 0);

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.2f;
        textMesh.fontSize = 32;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = Color.black;

        int typeValue = type;
        textMesh.text = typeValue >= 0 ? typeValue.ToString() : "";
    }

    public void ClearBoard()
    {
        if (boardRoot != null)
        {
            for (int i = boardRoot.childCount - 1; i >= 0; i--)
            {
                DestroyObject(boardRoot.GetChild(i).gameObject);
            }
        }
        if (holderRoot != null)
        {
            for (int i = holderRoot.childCount - 1; i >= 0; i--)
            {
                DestroyObject(holderRoot.GetChild(i).gameObject);
            }
        }
    }

    private void DestroyObject(GameObject obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}