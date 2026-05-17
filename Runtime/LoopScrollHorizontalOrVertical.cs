using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.ScrollRect;

public abstract class LoopScrollHorizontalOrVertical : LoopScroll
{
    public RectTransform.Axis direction;
    public override bool vertical => direction == RectTransform.Axis.Vertical;
    public override bool horizontal => direction == RectTransform.Axis.Horizontal;

    [SerializeField]
    protected Scrollbar m_Scrollbar;

    public Scrollbar scrollbar
    {
        get
        {
            return m_Scrollbar;
        }
        set
        {
            if (m_Scrollbar)
                m_Scrollbar.onValueChanged.RemoveListener(SetNormalizedPosition);
            m_Scrollbar = value;
            if (m_Scrollbar)
                m_Scrollbar.onValueChanged.AddListener(SetNormalizedPosition);
        }
    }

    public override Scrollbar horizontalScrollbar
    {
        get
        {
            if (vertical) return null;
            return m_Scrollbar;
        }
        set
        {
            if (vertical) return;
            scrollbar = value;
        }
    }

    public override Scrollbar verticalScrollbar
    {
        get
        {
            if (horizontal) return null;
            return m_Scrollbar;
        }
        set
        {
            if (horizontal) return;
            scrollbar = value;
        }
    }

    [SerializeField]
    private ScrollbarVisibility m_ScrollbarVisibility;
    public float spacing;
    public ScrollbarVisibility scrollbarVisibility
    {
        get => m_ScrollbarVisibility;
        set => m_ScrollbarVisibility = value;
    }

    public override ScrollbarVisibility horizontalScrollbarVisibility
    {
        get
        {
            if (vertical) return ScrollbarVisibility.Permanent;
            return scrollbarVisibility;
        }
        set
        {
            if (vertical) return;
            scrollbarVisibility = value;
        }
    }

    public override ScrollbarVisibility verticalScrollbarVisibility
    {
        get
        {
            if (horizontal) return ScrollbarVisibility.Permanent;
            return scrollbarVisibility;
        }
        set
        {
            if (horizontal) return;
            scrollbarVisibility = value;
        }
    }

    protected abstract float expectTotalSize { get; set; }

    protected abstract float normalizedValue
    {
        get;
    }

    public override Vector2 normalizedPosition
    {
        get
        {
            if (horizontal) return new Vector2(normalizedValue, 0);
            else return new Vector2(0, normalizedValue);
        }
        set
        {
            if (horizontal) SetNormalizedPosition(value.x);
            else SetNormalizedPosition(value.y);
        }
    }

    protected float startPosition, endPosition;
    protected float boundStart, boundEnd;
    protected bool everReachStart, everReachEnd;

    protected override void OnSetup(bool forwards)
    {
        everReachStart = everReachEnd = false;
        if (forwards)
        {
            startPosition = 0;
            endPosition = horizontal ? -spacing : spacing;
        }
        else if (horizontal)
        {
            endPosition = view.rect.width;
            startPosition = endPosition + spacing;
        }
        else
        {
            endPosition = -view.rect.height;
            startPosition = endPosition - spacing;
        }
    }

    protected void ReleaseForwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;
        bool hl = horizontal;
        var border = hl ? -position.x : -position.y;
        float itemEndPosition;
        for (int i = 0; i < count; i++)
        {
            var item = content.GetChild(0) as RectTransform;
            var size = GetItemSize(item, startIndex);
            if (hl)
            {
                itemEndPosition = item.anchoredPosition.x + LoopScrollHelper.GetAnchoredRightOffset(size, item.pivot.x);
                if (itemEndPosition <= border)
                {
                    ReleaseItem(item, startIndex++);
                    startPosition = itemEndPosition + spacing;
                    continue;
                }
            }
            else
            {
                itemEndPosition = item.anchoredPosition.y + LoopScrollHelper.GetAnchoredBottomOffset(size, item.pivot.y);
                if (itemEndPosition >= border)
                {
                    ReleaseItem(item, startIndex++);
                    startPosition = itemEndPosition - spacing;
                    continue;
                }
            }
            break;
        }
    }

    protected void InstantiateForwards()
    {
        if (totalCount == 0) return;
        bool hl = horizontal;
        var count = content.childCount;
        var contentPosition = hl ? content.anchoredPosition.x : content.anchoredPosition.y;

        float border = hl ? (view.rect.width - contentPosition) : (-view.rect.height - contentPosition);
        var p = (prefabSource.template.transform as RectTransform).anchoredPosition;
        ref float value = ref (hl ? ref p.x : ref p.y);
        var spacingOffset = hl ? spacing : -spacing;
        while (true)
        {
            if (totalCount > 0 && endIndex >= totalCount - 1)
                break;
            var itemStartPosition = endPosition + spacingOffset;
            if (hl && itemStartPosition >= border)
                break;
            if (vertical && itemStartPosition <= border)
                break;
            var item = InstantiateItem(++endIndex); ;
            onRefreshItem?.Invoke(item, endIndex);
            var pivot = item.pivot;
            var size = GetItemSize(item, endIndex);
            if (hl)
            {
                value = itemStartPosition - LoopScrollHelper.GetAnchoredLeftOffset(size, pivot.x);
                endPosition = itemStartPosition + size;
            }
            else
            {
                value = itemStartPosition - LoopScrollHelper.GetAnchoredTopOffset(size, pivot.y);
                endPosition = itemStartPosition - size;
            }
            item.anchoredPosition = p;
            if (totalCount > 0)
            {
                if (endIndex == 0)
                {
                    everReachStart = true;
                    boundStart = itemStartPosition;
                }

                if (endIndex == totalCount - 1)
                {
                    everReachEnd = true;
                    boundEnd = endPosition;
                }
            }
            OnInstantiatedItem(item, endIndex, size);
        }
    }

    protected void ReleaseBackwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;

        bool hl = horizontal;
        var border = hl ? view.rect.width - position.x : -view.rect.height - position.y;

        float itemStartPosition;
        for (int i = count - 1; i >= 0; i--)
        {
            var item = content.GetChild(i) as RectTransform;
            var size = GetItemSize(item, endIndex);
            if (hl)
            {
                itemStartPosition = item.anchoredPosition.x + LoopScrollHelper.GetAnchoredLeftOffset(size, item.pivot.x);
                if (itemStartPosition >= border)
                {
                    ReleaseItem(item, endIndex--);
                    endPosition = itemStartPosition - spacing;
                    continue;
                }
            }
            else
            {
                itemStartPosition = item.anchoredPosition.y + LoopScrollHelper.GetAnchoredTopOffset(size, item.pivot.y);
                if (itemStartPosition <= border)
                {
                    ReleaseItem(item, endIndex--);
                    endPosition = itemStartPosition + spacing;
                    continue;
                }
            }
            break;
        }
    }

    protected void InstantiateBackwards()
    {
        if (totalCount == 0) return;
        bool hl = horizontal;
        var count = content.childCount;
        var contentPosition = hl ? content.anchoredPosition.x : content.anchoredPosition.y;
        float border = -contentPosition;
        var p = (prefabSource.template.transform as RectTransform).anchoredPosition;
        ref float value = ref (hl ? ref p.x : ref p.y);
        float spacingOffset = hl ? -spacing : spacing;
        while (true)
        {
            if (totalCount > 0 && startIndex <= 0) break;
            var itemEndPosition = startPosition + spacingOffset;
            if (hl && itemEndPosition <= border) break;
            if (vertical && itemEndPosition >= border) break;
            var item = InstantiateItem(--startIndex);
            item.SetAsFirstSibling();
            onRefreshItem?.Invoke(item, startIndex);
            var size = GetItemSize(item, startIndex);
            if (hl)
            {
                value = itemEndPosition - LoopScrollHelper.GetAnchoredRightOffset(size, item.pivot.x);
                startPosition = itemEndPosition - size;
            }
            else
            {
                value = itemEndPosition - LoopScrollHelper.GetAnchoredBottomOffset(size, item.pivot.y);
                startPosition = itemEndPosition + size;
            }
            item.anchoredPosition = p;

            if (totalCount > 0)
            {
                if (startIndex == 0)
                {
                    everReachStart = true;
                    boundStart = startPosition;
                }

                if (startIndex == totalCount - 1)
                {
                    everReachEnd = true;
                    boundEnd = itemEndPosition;
                }
            }
            OnInstantiatedItem(item, startIndex, size);
        }
    }

    protected void RepositionContent(in Vector2 position)
    {
        content.anchoredPosition = position;
        Vector2 offset = Vector2.zero;
        float add;
        if (horizontal)
        {
            if (Math.Abs(position.x) <= view.rect.width)
            {
                content.anchoredPosition = position;
                return;
            }
            offset.x = position.x;
            add = offset.x;
        }
        else
        {
            if (Math.Abs(position.y) <= view.rect.height)
            {
                content.anchoredPosition = position;
                return;
            }
            offset.y = position.y;
            add = offset.y;
        }
        content.anchoredPosition = position - offset;
        foreach (var item in content)
        {
            (item as RectTransform).anchoredPosition += offset;
        }
        m_ContentStartPosition -= offset;
        m_VirtualContentOffset += offset;

        startPosition += add;
        endPosition += add;
        boundStart += add;
        boundEnd += add;
    }

    protected override void UpdateContentBounds()
    {
        if (movementType == MovementType.Unrestricted || totalCount < 0) return;
        m_ContentBounds.center = m_ViewBounds.center;
        m_ContentBounds.extents = m_ViewBounds.extents * 2;

        if (startIndex > 0 && endIndex < totalCount - 1)
            return;

        var hl = horizontal;
        var contentPosition = hl ? content.anchoredPosition.x : content.anchoredPosition.y;
        var viewExtents = m_ViewBounds.extents;

        if (everReachStart)
        {
            if (hl)
            {
                var min = m_ContentBounds.min;
                min.x = boundStart + contentPosition - viewExtents.x;
                m_ContentBounds.min = min;
            }
            else
            {
                var max = m_ContentBounds.max;
                max.y = boundStart + contentPosition + viewExtents.y;
                m_ContentBounds.max = max;
            }
        }

        if (everReachEnd)
        {
            if (hl)
            {
                var max = m_ContentBounds.max;
                max.x = boundEnd + contentPosition - viewExtents.x;
                m_ContentBounds.max = max;
            }
            else
            {
                var min = m_ContentBounds.min;
                min.y = boundEnd + contentPosition + viewExtents.y;
                m_ContentBounds.min = min;
            }
        }
        AdjustBounds();
    }

    protected abstract void SetNormalizedPosition(float value);

    protected override void UpdateScrollbars(Vector2 offset)
    {
        if (!m_Scrollbar || totalCount < 0)
            return;

        if (!working)
        {
            m_Scrollbar.size = 1;
        }
        else if (horizontal)
        {
            if (m_ContentBounds.min.x >= -m_ViewBounds.extents.x || m_ContentBounds.max.x <= m_ViewBounds.extents.x)
                m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / expectTotalSize);
            else
                m_Scrollbar.size = Mathf.Clamp01(m_ViewBounds.size.x / expectTotalSize);
        }
        else
        {
            if (m_ContentBounds.min.y >= -m_ViewBounds.extents.y || m_ContentBounds.max.y <= m_ViewBounds.extents.y)
                m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / expectTotalSize);
            else
                m_Scrollbar.size = Mathf.Clamp01(m_ViewBounds.size.y / expectTotalSize);

        }
        m_Scrollbar.value = normalizedValue;
    }

    protected abstract float GetItemSize(RectTransform item, int index);

    protected virtual void OnInstantiatedItem(RectTransform item, int index, float size) { }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (scrollbar)
            scrollbar.onValueChanged.AddListener(SetNormalizedPosition);
    }

    protected override void OnDisable()
    {
        if (scrollbar)
            scrollbar.onValueChanged.RemoveListener(SetNormalizedPosition);
        base.OnDisable();
    }
}