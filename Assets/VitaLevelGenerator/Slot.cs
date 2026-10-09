using System;
using System.Collections.Generic;
using System.Linq;

// 0 表示未分配，1~20 表示具体的牌面数字类型
public enum TileType
{
    None = 0,
    Type_1 = 1,
    Type_2 = 2,
    Type_3 = 3,
    Type_4 = 4,
    Type_5 = 5,
    Type_6 = 6,
    Type_7 = 7,
    Type_8 = 8,
    Type_9 = 9,
    Type_10 = 10,
    Type_11 = 11,
    Type_12 = 12,
    Type_13 = 13,
    Type_14 = 14,
    Type_15 = 15,
    Type_16 = 16,
    Type_17 = 17,
    Type_18 = 18,
    Type_19 = 19,
    Type_20 = 20
}

[Serializable] // 👈 必须加这个，Unity 才能在编辑器模式下序列化保存它
public class Slot
{
    public int Id;
    public int Layer; // 层级 (z 轴)
    public float X;   // 网格/世界坐标 X
    public float Y;   // 网格/世界坐标 Y

    // 卡牌的碰撞尺寸（通常麻将/卡牌的长宽比例，比如宽 1.0，高 1.2）
    public static float TileWidth = 1.0f;
    public static float TileHeight = 1.2f;

    public TileType AssignedType = TileType.None;

    // 拓扑遮挡关系
    public List<int> BlockingSlots = new List<int>(); // 压在自己上面的牌
    
    public List<int> Dependents = new List<int>();    // 被自己压在下面的牌

    // 左侧紧挨着的槽位列表
    public List<int> LeftBlockingSlots = new List<int>();

    // 右侧紧挨着的槽位列表
    public List<int> RightBlockingSlots = new List<int>();
    
    public Slot(int id, float x, float y, int layer)
    {
        Id = id;
        X = x;
        Y = y;
        Layer = layer;
    }
    
    public bool IsExposedInForwardGame
    {
        get
        {
            // 1. 上方不能有任何未消除的牌
            bool topClear = BlockingSlots.Count == 0;
            if (!topClear) return false;

            // 2. 左右两侧至少有一边是空的
            bool leftClear = LeftBlockingSlots.Count == 0;
            bool rightClear = RightBlockingSlots.Count == 0;

            return leftClear || rightClear;
        }
    }

    /// <summary>
    /// 检测当前槽位是否在 2D 投影上遮挡了下方的另一个槽位 (other)
    /// </summary>
    public bool OverlapsWith(Slot other)
    {
        // 只有上层的牌（Layer 更高）才能遮挡下层的牌
        if (this.Layer <= other.Layer) return false;

        // AABB 矩形重叠检测 (加上微小容差 margin 避免刚好贴边算作遮挡)
        float margin = 0.05f;
        bool overlapX = Math.Abs(this.X - other.X) < (TileWidth - margin);
        bool overlapY = Math.Abs(this.Y - other.Y) < (TileHeight - margin);

        return overlapX && overlapY;
    }
}
public class SlotPair
{
    public Slot SlotA;
    public Slot SlotB;
    public TileType AssignedType = TileType.None;

    public SlotPair(Slot a, Slot b)
    {
        SlotA = a;
        SlotB = b;
    }
}