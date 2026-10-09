using UnityEngine;

public class TileController : MonoBehaviour
{
    public Slot slotData;
    private LevelVisualizer visualizer;

    public void Init(Slot slot, LevelVisualizer mgr)
    {
        slotData = slot;
        visualizer = mgr;
    }

    private void OnMouseDown()
    {
        if (visualizer == null || slotData == null) return;

        // 核心规则 1：上层有遮挡时无法点击
        if (!slotData.IsExposedInForwardGame)
        {
            Debug.Log($"[无法点击] 卡牌 #{slotData.Id} 被上层遮挡！");
            return;
        }

        // 核心规则 2：暂存区已满时无法点击
        if (visualizer.IsHolderFull())
        {
            Debug.LogWarning("[无法点击] 4格暂存区已满！无法继续选择卡牌");
            return;
        }

        // 满足条件，通知管理器选中该卡牌
        visualizer.OnTileClicked(this);
    }
}