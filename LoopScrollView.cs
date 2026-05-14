using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;
using static UnityEngine.UI.ScrollRect;
using System.Collections;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public abstract class LoopScrollView : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
{
    [Tooltip("Prefab Source")]
    public LoopSource prefabSource;
    [Tooltip("Total count, negative means INFINITE mode")]
    public int totalCount;
    public RectTransform view;
    public RectTransform content;

    public bool enableDragInParent;
    private bool routeToParent = false;
    public MovementType movementType = MovementType.Elastic;
    public float elasticity = 0.1f;
    public bool inertia = true;
    public float decelerationRate = 0.135f;
    public float scrollSensitivity = 25.0f;

    public Action<Vector2> onValueChanged;

    private Vector2 m_PointerStartLocalCursor = Vector2.zero;
    protected Vector2 m_ContentStartPosition = Vector2.zero;

    protected Bounds m_ContentBounds;
    protected Bounds m_ViewBounds;
    protected Vector2 m_Velocity;
    public Vector2 velocity { get { return m_Velocity; } set { m_Velocity = value; } }

    protected abstract bool vertical { get; }
    protected abstract bool horizontal { get; }
    protected bool m_Dragging;
    private bool m_Scrolling;
    public abstract Scrollbar horizontalScrollbar { get; set; }
    public abstract Scrollbar verticalScrollbar { get; set; }
    public abstract Vector2 normalizedPosition { get; set; }
    public abstract ScrollbarVisibility horizontalScrollbarVisibility { get; set; }
    public abstract ScrollbarVisibility verticalScrollbarVisibility { get; set; }

    private bool hScrollingNeeded
    {
        get
        {
            if (Application.isPlaying)
                return totalCount > 0 && m_ContentBounds.size.x > m_ViewBounds.size.x + 0.01f;
            return true;
        }
    }
    private bool vScrollingNeeded
    {
        get
        {
            if (Application.isPlaying)
                return totalCount > 0 && m_ContentBounds.size.y > m_ViewBounds.size.y + 0.01f;

            return true;
        }
    }

    private Vector2 m_PrevPosition = Vector2.zero;
    public Action<Vector2> onBeginDrag;
    public Action onDrag;
    public Action<Vector2> onEndDrag;

    public Action<int, RectTransform> onRefreshItem;
    public Action<int, RectTransform> onReleaseItem;
    protected Vector2 m_VirtualContentOffset;
    protected int startIndex = 0, endIndex = -1;
    private Vector2 itemAnchorMin, itemAnchorMax;
    protected bool filled;

    public void RefillCells(int offset = 0)
    {
        if (totalCount >= 0 && offset >= totalCount) throw new Exception();
        Clear();
        startIndex = offset;
        endIndex = offset - 1;
        var pos = content.anchoredPosition;
        var anchorMin = content.anchorMin;
        var anchorMax = content.anchorMax;
        var pivot = content.pivot;
        var item = prefabSource.template.transform as RectTransform;
        itemAnchorMin = item.anchorMin;
        itemAnchorMax = item.anchorMax;
        if (horizontal)
        {
            anchorMin.x = 0;
            anchorMax.x = 0;
            pivot.x = 0;
            itemAnchorMin.x = 0;
            itemAnchorMax.x = 0;
            pos.x = 0;
        }
        if (vertical)
        {
            anchorMin.y = 1;
            anchorMax.y = 1;
            pivot.y = 1;
            itemAnchorMin.y = 1;
            itemAnchorMax.y = 1;
            pos.y = 0;
        }
        content.anchorMin = anchorMin;
        content.anchorMax = anchorMax;
        content.pivot = pivot;

        content.anchoredPosition = pos;
        m_VirtualContentOffset.x = 0;
        m_VirtualContentOffset.y = 0;
        UpdateViewBounds();
        Refill();
        UpdateContentBounds();
        // 处理有offset时，已经到最后一个元素就尝试向前塞元素
        if (totalCount > 0 && endIndex >= totalCount - 1 && offset > 0 && movementType != MovementType.Unrestricted)
        {
            var positionOffset = Vector2.zero;
            if (horizontal)
                positionOffset.x = m_ViewBounds.max.x - m_ContentBounds.max.x;
            if (vertical)
                positionOffset.y = m_ViewBounds.min.y - m_ContentBounds.min.y;

            if (positionOffset.x != 0 || positionOffset.y != 0)
                SetContentAnchoredPosition(content.anchoredPosition + positionOffset);
        }
        filled = true;
        UpdatePrevData();
        UpdateScrollbars(Vector2.zero);
    }

    public void RefreshCells()
    {
        if (!isActiveAndEnabled) return;
        int index = 0;
        for (int i = startIndex; i <= endIndex; i++)
        {
            onRefreshItem?.Invoke(startIndex + index, content.GetChild(index++) as RectTransform);
        }
    }

    protected abstract void Refill();

    public void ScrollToCell(int index, float speed, Action callBack = null)
    {
        if (!filled) return;
        if (totalCount >= 0 && (index < 0 || index >= totalCount))
        {
            Debug.LogWarningFormat("invalid index {0}", index);
            return;
        }
        if (speed <= 0)
        {
            Debug.LogWarningFormat("invalid speed {0}", speed);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(ScrollToStartCellCoroutine(index, speed, callBack));
    }

    // 移动速度如果太快一两帧会跳过1个元素的情况暂不处理
    IEnumerator ScrollToStartCellCoroutine(int index, float speed, Action callBack = null)
    {
        Vector2 move = Vector2.zero;
        RectTransform item = null;
        Rect itemRect = Rect.zero;
        if (index < startIndex)
        {
            if (horizontal) move.x = speed;
            if (vertical) move.y = -speed;
        }
        else if (index > endIndex)
        {
            if (horizontal) move.x = -speed;
            if (vertical) move.y = speed;
        }
        else
        {
            item = GetItem(index);
            itemRect = item.rect;
            var pos = item.anchoredPosition + content.anchoredPosition;
            if (horizontal)
            {
                var x = pos.x + itemRect.xMin;
                if (x < 0)
                    move.x = speed;
                else if (x > 0)
                    move.x = -speed;
            }
            if (vertical)
            {
                var y = pos.y + itemRect.yMax;
                if (y > 0)
                    move.y = -speed;
                else if (y < 0)
                    move.y = speed;
            }
        }
        bool needMoving = move.x != 0 || move.y != 0;

        while (needMoving)
        {
            if (m_Dragging) break;

            yield return null;
            var offset = move * Time.deltaTime;
            if (item == null)
            {
                item = GetItem(index);
                if (item != null) itemRect = item.rect;
            }
            var position = content.anchoredPosition + offset;
            if (item == null)
            {
                SetContentAnchoredPosition(position);
            }
            else
            {
                var itemPostion = item.anchoredPosition + position;
                if (move.x != 0)
                {
                    var x = itemPostion.x + itemRect.xMin;
                    if ((move.x > 0 && x >= 0) || (move.x < 0 && x <= 0))
                    {
                        position.x -= x;
                        move.x = 0;
                    }
                }
                if (move.y != 0)
                {
                    var y = itemPostion.y + itemRect.yMax;
                    if ((move.y > 0 && y >= 0) || (move.y < 0 && y <= 0))
                    {
                        position.y -= y;
                        move.y = 0;
                    }
                }
                SetContentAnchoredPosition(position);
                if (totalCount > 0)
                {
                    offset.x = offset.y = 0;
                    if (move.x > 0)
                    {
                        if (m_ContentBounds.min.x > m_ViewBounds.min.x)
                        {
                            offset.x = m_ViewBounds.min.x - m_ContentBounds.min.x;
                            move.x = 0;
                        }
                    }
                    else if (move.x < 0)
                    {
                        if (m_ContentBounds.max.x < m_ViewBounds.max.x)
                        {
                            offset.x = m_ViewBounds.max.x - m_ContentBounds.max.x;
                            move.x = 0;
                        }
                    }
                    if (move.y > 0)
                    {
                        if (m_ContentBounds.min.y > m_ViewBounds.min.y)
                        {
                            offset.y = m_ViewBounds.min.y - m_ContentBounds.min.y;
                            move.y = 0;
                        }
                    }
                    else if (move.y < 0)
                    {
                        if (m_ContentBounds.max.y < m_ViewBounds.max.y)
                        {
                            offset.y = m_ViewBounds.max.y - m_ContentBounds.max.y;
                            move.y = 0;
                        }
                    }
                    if (offset.x != 0 || offset.y != 0)
                    {
                        SetContentAnchoredPosition(content.anchoredPosition + offset);
                    }
                }
                if (move.x == 0 && move.y == 0) break;
            }
        }
        StopMovement();
        UpdatePrevData();
        callBack?.Invoke();
    }

    private void StopMovement()
    {
        m_Velocity = Vector2.zero;
    }

    protected RectTransform InstantiateItem(int index)
    {
        RectTransform item = prefabSource.Get().transform as RectTransform;
        item.transform.SetParent(content, false);
        item.anchorMin = itemAnchorMin;
        item.anchorMax = itemAnchorMax;
        item.name = index.ToString();
        return item;
    }

    protected void ReleaseItem(int itemIndex, RectTransform item)
    {
        prefabSource.Release(item.gameObject);
        onReleaseItem?.Invoke(itemIndex, item);
    }

    public RectTransform GetItem(int index)
    {
        if (index < startIndex || index > endIndex) return null;
        return content.GetChild(index - startIndex) as RectTransform;
    }

    public virtual void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        StopMovement();
        if (enableDragInParent && (!horizontal || !vertical))
        {
            ExecuteEvents.ExecuteHierarchy<IInitializePotentialDragHandler>(transform.parent.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
        }
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!IsActive() || !filled)
            return;

        if (enableDragInParent)
        {
            routeToParent = false;
            if (!horizontal)
            {
                if (Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
                    routeToParent = true;
            }
            else if (!vertical)
            {
                if (Math.Abs(eventData.delta.y) > Math.Abs(eventData.delta.x))
                    routeToParent = true;
            }
            if (routeToParent)
            {
                ExecuteEvents.ExecuteHierarchy<IBeginDragHandler>(transform.parent.gameObject, eventData, ExecuteEvents.beginDragHandler);
                return;
            }
        }

        onBeginDrag?.Invoke(eventData.position);
        m_PointerStartLocalCursor = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(view, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
        m_ContentStartPosition = content.anchoredPosition;
        m_Dragging = true;
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (routeToParent)
        {
            ExecuteEvents.ExecuteHierarchy<IDragHandler>(transform.parent.gameObject, eventData, ExecuteEvents.dragHandler);
            return;
        }

        if (!m_Dragging || !IsActive() || !filled)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(view, eventData.position, eventData.pressEventCamera, out Vector2 localCursor))
            return;

        onDrag?.Invoke();
        var pointerDelta = localCursor - m_PointerStartLocalCursor;
        Vector2 position = m_ContentStartPosition + pointerDelta;
        // Offset to get content into place in the view.
        Vector2 offset = CalculateOffset(position - content.anchoredPosition);
        position += offset;

        if (movementType == MovementType.Elastic)
        {
            if (offset.x != 0)
                position.x = position.x - RubberDelta(offset.x, m_ViewBounds.size.x);
            if (offset.y != 0)
                position.y = position.y - RubberDelta(offset.y, m_ViewBounds.size.y);
        }
        SetContentAnchoredPosition(position);
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!IsActive() || !filled)
            return;

        if (routeToParent)
        {
            ExecuteEvents.ExecuteHierarchy<IEndDragHandler>(transform.parent.gameObject, eventData, ExecuteEvents.endDragHandler);
            routeToParent = false;
            return;
        }

        onEndDrag?.Invoke(eventData.position);
        m_Dragging = false;
    }

    public void OnScroll(PointerEventData data)
    {
        if (!IsActive() || !filled)
            return;

        Vector2 delta = data.scrollDelta;
        // Down is positive for scroll events, while in UI system up is positive.
        delta.y *= -1;
        if (vertical && !horizontal)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                delta.y = delta.x;
            delta.x = 0;
        }
        if (horizontal && !vertical)
        {
            if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                delta.x = delta.y;
            delta.y = 0;
        }
        delta.x = -delta.x;

        if (data.IsScrolling())
            m_Scrolling = true;

        Vector2 position = content.anchoredPosition;
        position += delta * scrollSensitivity;
        if (movementType == MovementType.Clamped)
            position += CalculateOffset(position - content.anchoredPosition);

        SetContentAnchoredPosition(position);
    }

    protected abstract void UpdateScrollbars(Vector2 offset);

    protected void UpdatePrevData()
    {
        m_PrevPosition = GetVirtualContentPosition();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateScrollbarVisibility();
    }

    protected virtual void LateUpdate()
    {
        if (!content)
            return;

        float deltaTime = Time.unscaledDeltaTime;
        Vector2 offset = CalculateOffset(Vector2.zero);
        var virtualPosition = GetVirtualContentPosition();
        // Skip processing if deltaTime is invalid (0 or less) as it will cause inaccurate velocity calculations and a divide by zero error.
        if (deltaTime > 0.0f)
        {
            if (!m_Dragging && (offset != Vector2.zero || m_Velocity != Vector2.zero))
            {
                Vector2 position = content.anchoredPosition;
                for (int axis = 0; axis < 2; axis++)
                {
                    // Apply spring physics if movement is elastic and content has an offset from the view.
                    if (movementType == MovementType.Elastic && offset[axis] != 0)
                    {
                        float speed = m_Velocity[axis];
                        float smoothTime = elasticity;
                        if (m_Scrolling)
                            smoothTime *= 3.0f;
                        position[axis] = Mathf.SmoothDamp(content.anchoredPosition[axis], content.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                        if (Mathf.Abs(speed) < 1)
                            speed = 0;
                        m_Velocity[axis] = speed;
                    }
                    // Else move content according to velocity with deceleration applied.
                    else if (inertia)
                    {
                        m_Velocity[axis] *= Mathf.Pow(decelerationRate, deltaTime);
                        if (Mathf.Abs(m_Velocity[axis]) < 1)
                            m_Velocity[axis] = 0;
                        position[axis] += m_Velocity[axis] * deltaTime;
                    }
                    // If we have neither elaticity or friction, there shouldn't be any velocity.
                    else
                    {
                        m_Velocity[axis] = 0;
                    }
                }

                if (movementType == MovementType.Clamped)
                {
                    offset = CalculateOffset(position - content.anchoredPosition);
                    position += offset;
                }

                SetContentAnchoredPosition(position);
                virtualPosition = GetVirtualContentPosition();
            }

            if (m_Dragging && inertia)
            {
                Vector3 newVelocity = (virtualPosition - m_PrevPosition) / deltaTime;
                m_Velocity = Vector3.Lerp(m_Velocity, newVelocity, deltaTime * 10);
            }
        }

        if (virtualPosition != m_PrevPosition)
        {
            UpdateScrollbars(offset);
            UISystemProfilerApi.AddMarker("ScrollRect.value", this);
            onValueChanged?.Invoke(normalizedPosition);
            m_PrevPosition = virtualPosition;
        }
        if (totalCount > 0)
            UpdateScrollbarVisibility();
        m_Scrolling = false;
    }

    public void Clear()
    {
        filled = false;
        for (int i = startIndex; i <= endIndex; i++)
        {
            ReleaseItem(i, content.GetChild(0) as RectTransform);
        }
    }

    protected abstract void SetContentAnchoredPosition(Vector2 position, bool jump = false);

    protected Vector2 GetVirtualContentPosition()
    {
        var position = content.anchoredPosition;
        return position + m_VirtualContentOffset;
    }

    private void UpdateBounds()
    {
        UpdateViewBounds();
        UpdateContentBounds();
    }

    private void UpdateViewBounds()
    {
        m_ViewBounds = new Bounds(Vector2.zero, view.rect.size);
    }

    protected abstract void UpdateContentBounds();

    protected void AdjustBounds()
    {
        // Make sure content bounds are at least as large as view by adding padding if not.
        // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
        // E.g. if pivot is at top, bounds are expanded downwards.
        var contentSize = m_ContentBounds.size;
        var contentPos = m_ContentBounds.center;
        var contentPivot = content.pivot;
        Vector3 excess = m_ViewBounds.size - contentSize;
        bool dirty = false;
        if (excess.x > 0)
        {
            contentPos.x -= excess.x * (contentPivot.x - 0.5f);
            contentSize.x = m_ViewBounds.size.x;
            dirty = true;
        }
        if (excess.y > 0)
        {
            contentPos.y -= excess.y * (contentPivot.y - 0.5f);
            contentSize.y = m_ViewBounds.size.y;
            dirty = true;
        }
        if (dirty)
        {
            m_ContentBounds.center = contentPos;
            m_ContentBounds.size = contentSize;
        }
    }

    private static float RubberDelta(float overStretching, float viewSize)
    {
        return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
    }

    void UpdateScrollbarVisibility()
    {
        if (verticalScrollbar && verticalScrollbarVisibility != ScrollbarVisibility.Permanent)
            UpdateScrollbarVisibility(vScrollingNeeded, verticalScrollbarVisibility, verticalScrollbar, 0);
        if (horizontalScrollbar && horizontalScrollbarVisibility != ScrollbarVisibility.Permanent)
            UpdateScrollbarVisibility(hScrollingNeeded, horizontalScrollbarVisibility, horizontalScrollbar, 1);
    }

    private void UpdateScrollbarVisibility(bool xScrollingNeeded, ScrollbarVisibility scrollbarVisibility, Scrollbar scrollbar, int expandAxis)
    {
        bool change = scrollbar.gameObject.activeSelf != xScrollingNeeded;
        if (!change) return;
        scrollbar.gameObject.SetActive(xScrollingNeeded);
        //有expand需求自己处理view的锚点和支点，这里只对尺寸做处理
        if (scrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport)
        {
            var size = Vector2.zero;
            size[expandAxis] = (scrollbar.transform as RectTransform).sizeDelta[expandAxis];
            if (xScrollingNeeded)
                view.sizeDelta += size;
            else
                view.sizeDelta -= size;
            UpdateBounds();
        }
    }

    private Vector2 CalculateOffset(Vector2 delta)
    {
        Vector2 offset = Vector2.zero;
        if (movementType == MovementType.Unrestricted || totalCount < 0)
            return offset;

        Vector2 min = m_ContentBounds.min;
        Vector2 max = m_ContentBounds.max;

        if (horizontal)
        {
            min.x += delta.x;
            max.x += delta.x;

            float maxOffset = m_ViewBounds.max.x - max.x;
            float minOffset = m_ViewBounds.min.x - min.x;

            if (minOffset < -0.001f)
                offset.x = minOffset;
            else if (maxOffset > 0.001f)
                offset.x = maxOffset;
        }

        if (vertical)
        {
            min.y += delta.y;
            max.y += delta.y;

            float maxOffset = m_ViewBounds.max.y - max.y;
            float minOffset = m_ViewBounds.min.y - min.y;

            if (maxOffset > 0.001f)
                offset.y = maxOffset;
            else if (minOffset < -0.001f)
                offset.y = minOffset;
        }

        return offset;
    }

    protected bool IsItemVisible(int index)
    {
        return index >= startIndex && index <= endIndex;
    }

    protected bool IsItemAllVisible(int index)
    {
        if (!IsItemVisible(index)) return false;
        var item = GetItem(index);
        var pos = item.anchoredPosition + content.anchoredPosition;
        var rect = item.rect;
        if (horizontal)
        {
            if (pos.x + rect.xMin < 0 || pos.x + rect.xMax > view.rect.width)
                return false;
        }
        if (vertical)
        {
            if (pos.y + rect.yMax > 0 || pos.y + rect.yMin < -view.rect.height)
                return false;
        }
        return true;
    }

    protected override void OnDisable()
    {
        m_Dragging = false;
        m_Scrolling = false;
        m_Velocity = Vector2.zero;
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        onRefreshItem = null;
        onReleaseItem = null;
        onBeginDrag = null;
        onDrag = null;
        onEndDrag = null;
        prefabSource.Clear();
    }
}