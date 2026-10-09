using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class TileEditorNode : MonoBehaviour
{
    [SerializeField]
    public Slot slotData;
    
    // 卡牌尺寸标准（可根据实际材质调节）
    public static Vector2 tileSize = new Vector2(2.2f, 3.2f);
    public static float layerZOffset = -1f; // Z 轴深度层间距

    public void Init(int id, float x, float y, int layer)
    {
        slotData = new Slot(id, x, y, layer);
        UpdateTransform();
        
#if UNITY_EDITOR
        // 2. 标记对象为 Dirty，强行刷新 Inspector 并持久化保存数据
        EditorUtility.SetDirty(this);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    public void UpdateTransform()
    {
        transform.localPosition = new Vector3(slotData.X * tileSize.x, slotData.Y * tileSize.y, slotData.Layer * layerZOffset);
        gameObject.name = $"Tile_{slotData.Id}_L{slotData.Layer}";
    }

    // ================= 1. 同层四向扩展 =================
    public Vector2 GetNewPosSameLayer(Vector2Int direction)
    {
        return new Vector2(
            slotData.X + direction.x,
            slotData.Y + direction.y
        );
    }

    // ================= 2. 上层半牌错位扩展 =================
    // offsetX / offsetY 传入 -0.5, 0, 0.5 等（例如 x遮挡一半为 0.5，x不变为 0）
    public Vector2 GetNewPosUpperLayer(float offsetXRatio, float offsetYRatio)
    {
        return new Vector2(
            slotData.X + offsetXRatio,
            slotData.Y + offsetYRatio
        );
    }
}