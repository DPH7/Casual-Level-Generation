#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileEditorNode))]
public class TileEditorNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TileEditorNode currentNode = (TileEditorNode)target;
        LevelLayoutEditor layoutMgr = currentNode.GetComponentInParent<LevelLayoutEditor>();

        if (layoutMgr == null)
        {
            EditorGUILayout.HelpBox("请确保此卡牌物体位于 LevelLayoutEditor 根节点下！", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("⚡ 同层快速扩展 (Same Layer)", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬅️ 左扩展")) AddTileSameLayer(currentNode, layoutMgr, Vector2Int.left);
        if (GUILayout.Button("➡️ 右扩展")) AddTileSameLayer(currentNode, layoutMgr, Vector2Int.right);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⬆️ 上扩展")) AddTileSameLayer(currentNode, layoutMgr, Vector2Int.up);
        if (GUILayout.Button("⬇️ 下扩展")) AddTileSameLayer(currentNode, layoutMgr, Vector2Int.down);
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("🏗️ 上层错位覆盖扩展 (Upper Layer +1)", EditorStyles.boldLabel);

        // 1. 单轴一半遮盖
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("↔️ X遮一半, Y不变")) 
            AddTileUpperLayer(currentNode, layoutMgr, 0.5f, 0f);
        if (GUILayout.Button("↕️ X不变, Y遮一半")) 
            AddTileUpperLayer(currentNode, layoutMgr, 0f, 0.5f);
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("📐 XY 各遮一半 (1/4 覆盖 - 4个角)", EditorStyles.miniBoldLabel);

        // 2. 双轴 1/4 四角遮盖
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("↖️ 左上 1/4")) AddTileUpperLayer(currentNode, layoutMgr, -0.5f, 0.5f);
        if (GUILayout.Button("↗️ 右上 1/4")) AddTileUpperLayer(currentNode, layoutMgr, 0.5f, 0.5f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("↙️ 左下 1/4")) AddTileUpperLayer(currentNode, layoutMgr, -0.5f, -0.5f);
        if (GUILayout.Button("↘️ 右下 1/4")) AddTileUpperLayer(currentNode, layoutMgr, 0.5f, -0.5f);
        GUILayout.EndHorizontal();

        EditorGUILayout.Space(15);
        GUI.color = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("❌ 删除当前选中的卡牌", GUILayout.Height(25)))
        {
            Undo.DestroyObjectImmediate(currentNode.gameObject);
        }
        GUI.color = Color.white;
    }

    private void AddTileSameLayer(TileEditorNode source, LevelLayoutEditor mgr, Vector2Int dir)
    {
        Vector2 newPos = source.GetNewPosSameLayer(dir);
        mgr.CreateTileAt(newPos.x, newPos.y, source.slotData.Layer);
    }

    private void AddTileUpperLayer(TileEditorNode source, LevelLayoutEditor mgr, float offsetXRatio, float offsetYRatio)
    {
        Vector2 newPos = source.GetNewPosUpperLayer(offsetXRatio, offsetYRatio);
        mgr.CreateTileAt(newPos.x, newPos.y, source.slotData.Layer + 1);
    }
}
#endif