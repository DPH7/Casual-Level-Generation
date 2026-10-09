using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GridCellUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("组件引用")]
    [SerializeField] private Image bgImage;
    [SerializeField] private GameObject catIconObj;
    [SerializeField] private Image catIconImage;
    [SerializeField] private GameObject crossIconObj;
    [SerializeField] private Image crossIconImage;

    private bool isCorrectCat;
    private bool isCatRevealed = false;
    private bool isMarkedNoCat = false;

    private Coroutine clickCoroutine;
    private const float doubleClickThreshold = 0.22f;

    // 全局/静态变量：记录本次滑动拖拽中已经标记过的格子，防止在一个格子里重复触发
    private static HashSet<GridCellUI> draggedCells = new HashSet<GridCellUI>();
    private static bool isDragging = false;

    public void Init(Color color, bool hasCat, Sprite catSprite, Sprite crossSprite = null)
    {
        bgImage.color = color;
        isCorrectCat = hasCat;

        if (catIconImage != null && catSprite != null) catIconImage.sprite = catSprite;
        if (crossIconImage != null && crossSprite != null) crossIconImage.sprite = crossSprite;

        catIconObj.SetActive(false);
        crossIconObj.SetActive(false);
        isMarkedNoCat = false;
        isCatRevealed = false;
    }

    #region 点击逻辑 (单击/双击)

    public void OnPointerClick(PointerEventData eventData)
    {
        // 如果刚刚进行了滑动拖拽，不触发点击逻辑
        if (isDragging || isCatRevealed) return;

        if (eventData.clickCount == 1)
        {
            clickCoroutine = StartCoroutine(SingleClickRoutine());
        }
        else if (eventData.clickCount == 2)
        {
            if (clickCoroutine != null)
            {
                StopCoroutine(clickCoroutine);
                clickCoroutine = null;
            }
            OnDoubleClick();
        }
    }

    private IEnumerator SingleClickRoutine()
    {
        yield return new WaitForSeconds(doubleClickThreshold);
        ToggleNoCatMark();
        clickCoroutine = null;
    }

    private void OnDoubleClick()
    {
        if (isCorrectCat)
        {
            isCatRevealed = true;
            catIconObj.SetActive(true);
            SetMarkNoCat(false); // 找到猫咪时清除叉号
        }
    }

    #endregion

    #region 滑动/拖拽标记逻辑 (Drag to Mark)

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        draggedCells.Clear();

        // 中断可能正在等待的单击判定
        if (clickCoroutine != null)
        {
            StopCoroutine(clickCoroutine);
            clickCoroutine = null;
        }

        // 标记起始格子
        ProcessCellUnderPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 实时检测指针/手指当前停留在哪个 UI 格子上
        ProcessCellUnderPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        draggedCells.Clear();
        // 延迟一小段时间重置拖拽标记，避免抬起时误触发 PointerClick
        StartCoroutine(ResetDragState());
    }

    private IEnumerator ResetDragState()
    {
        yield return new WaitForEndOfFrame();
        isDragging = false;
    }

    /// <summary>
    /// 射线检测当前指针对准的 GridCellUI 并划上“无猫”标记
    /// </summary>
    private void ProcessCellUnderPointer(PointerEventData eventData)
    {
        GameObject currentTarget = eventData.pointerCurrentRaycast.gameObject;
        if (currentTarget == null) return;

        GridCellUI targetCell = currentTarget.GetComponentInParent<GridCellUI>();
        if (targetCell != null && !draggedCells.Contains(targetCell) && targetCell.isCatRevealed == false)
        {
            draggedCells.Add(targetCell);
            targetCell.MarkAsNoCatFromDrag();
        }
    }

    /// <summary>
    /// 滑动划过时强制标记为“无猫”
    /// </summary>
    public void MarkAsNoCatFromDrag()
    {
        if (isCatRevealed) return;
        SetMarkNoCat(true);
    }

    private void ToggleNoCatMark()
    {
        SetMarkNoCat(!isMarkedNoCat);
    }

    private void SetMarkNoCat(bool active)
    {
        isMarkedNoCat = active;
        crossIconObj.SetActive(isMarkedNoCat);
    }

    #endregion
}