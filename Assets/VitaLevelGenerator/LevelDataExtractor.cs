using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelDataExtractor
{
    // 浮点数比较容差
    private const float EPSILON = 0.05f;

    /// <summary>
    /// 从场景根节点读取所有 Tile 并生成运行时 Slot 拓扑结构
    /// </summary>
    public static List<Slot> ExtractRuntimeSlots(Transform sceneRoot)
    {
        TileEditorNode[] nodes = sceneRoot.GetComponentsInChildren<TileEditorNode>();
        List<Slot> allSlots = new List<Slot>();

        // 1. 搜集并重构 Slot 基础坐标与 ID
        int idCounter = 0;
        foreach (var node in nodes)
        {
            Slot slot = node.slotData;
            slot.Id = idCounter++;
            node.name = $"Tile_{slot.Id}_L{slot.Layer}";
                
            // 清空旧的运行时计算数据
            slot.BlockingSlots.Clear();
            slot.LeftBlockingSlots.Clear();
            slot.RightBlockingSlots.Clear();
            slot.Dependents.Clear();
            
            allSlots.Add(slot);
        }

        // 2. 双重循环匹配，建立“压制”与“左右阻挡”关系
        for (int i = 0; i < allSlots.Count; i++)
        {
            Slot a = allSlots[i];

            for (int j = 0; j < allSlots.Count; j++)
            {
                if (i == j) continue;
                Slot b = allSlots[j];

                // ------------------ A. 上方遮挡判断 (Top Blocking) ------------------
                // 必须在上方层级 (b.Layer > a.Layer) 且在 XY 投影平面上有重叠 (包含半覆盖、1/4覆盖)
                if (b.Layer > a.Layer)
                {
                    bool xOverlap = Mathf.Abs(a.X - b.X) < (0.5f + EPSILON);
                    bool yOverlap = Mathf.Abs(a.Y - b.Y) < (0.5f + EPSILON);

                    if (xOverlap && yOverlap)
                    {
                        // b 压在了 a 的上方，因此 a 的被遮挡列表中加入 b
                        a.BlockingSlots.Add(b.Id);
                        b.Dependents.Add(a.Id);
                    }
                }

                // ------------------ B. 同层左右阻挡判断 (Left/Right Blocking) ------------------
                // 必须在同一层级 (b.Layer == a.Layer) 且 Y 轴方向有足够交叠（紧挨着）
                if (b.Layer == a.Layer)
                {
                    bool yOverlap = (int)a.Y- (int)b.Y == 0;

                    if (yOverlap)
                    {
                        // 判断 b 是否在 a 的左侧（b.X < a.X，且紧邻距离在 1.0 个单位内）
                        if (b.X < a.X && (a.X - b.X) < (1 + EPSILON))
                        {
                            a.LeftBlockingSlots.Add(b.Id);
                        }
                        // 判断 b 是否在 a 的右侧（b.X > a.X，且紧邻距离在 1.0 个单位内）
                        else if (b.X > a.X && (b.X - a.X) < (1 + EPSILON))
                        {
                            a.RightBlockingSlots.Add(b.Id);
                        }
                    }
                }
            }
        }

        Debug.Log($"<color=green>[关卡数据构建成功]</color> 成功读取 {allSlots.Count} 张卡牌的数据与依赖关系！");
        return allSlots;
    }
}