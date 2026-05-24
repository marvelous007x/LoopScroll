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

    [Tooltip("X means padding from start, Y means padding from end")]
    public Vector2 padding;

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
    protected float alongViewSize;

    protected override void OnSetup(bool forwards)
    {
        alongViewSize = horizontal ? m_ViewBounds.size.x : m_ViewBounds.size.y;
    }

    protected override void RefillCells(bool forwards)
    {
        if (forwards)
            InstantiateForwards();
        else
            InstantiateBackwards();

        if (forwards)
        {
            if (padding.x > 0 && (totalCount < 0 || startIndex > 0))
                InstantiateBackwards();
        }
        else
        {
            if (padding.y > 0 && (totalCount < 0 || endIndex < totalCount - 1))
                InstantiateForwards();
        }

        OnRefilled();

        if (forwards)
        {
            if (AdjustToEnd())
            {
                AdjustToStart();
                return;
            }
        }
        else
        {
            if (AdjustToStart())
                return;
        }
        UpdateContentBounds();
        UpdateScrollbars(Vector2.zero);
    }

    protected virtual void OnRefilled() { }

    private bool AdjustToEnd()
    {
        if (totalCount > 0 && startIndex > 0 && endIndex >= totalCount - 1 && movementType != MovementType.Unrestricted)
        {
            // if offset cause has space to end, reset position to reach end
            var positionOffset = Vector2.zero;
            float position;
            var anchoredPosition = content.anchoredPosition;
            if (horizontal)
            {
                position = anchoredPosition.x + endPosition + padding.y;
                if (position < alongViewSize)
                {
                    positionOffset.x = alongViewSize - position;
                }
            }
            else
            {
                position = anchoredPosition.y + endPosition - padding.y;
                if (position > -alongViewSize)
                {
                    positionOffset.y = -alongViewSize - position;
                }
            }

            if (positionOffset.x != 0 || positionOffset.y != 0)
            {
                SetContentAnchoredPosition(anchoredPosition + positionOffset);
                return true;
            }
        }
        return false;
    }

    private bool AdjustToStart()
    {
        if (totalCount > 0 && startIndex == 0 && movementType != MovementType.Unrestricted)
        {
            var positionOffset = Vector2.zero;
            var anchoredPosition = content.anchoredPosition;
            float position;
            if (horizontal)
            {
                position = anchoredPosition.x + startPosition - padding.x;
                if (position > 0)
                {
                    positionOffset.x = -position;
                }
            }
            else
            {
                position = anchoredPosition.y + startPosition + padding.x;
                if (position < 0)
                {
                    positionOffset.y = -position;
                }
            }

            if (positionOffset.x != 0 || positionOffset.y != 0)
            {
                SetContentAnchoredPosition(content.anchoredPosition + positionOffset);
                return true;
            }
        }
        return false;
    }

    protected void RepositionContent(in Vector2 position)
    {
        content.anchoredPosition = position;
        Vector2 offset = Vector2.zero;
        float add;
        if (horizontal)
        {
            if (Math.Abs(position.x) <= alongViewSize)
            {
                content.anchoredPosition = position;
                return;
            }
            offset.x = position.x;
            add = offset.x;
        }
        else
        {
            if (Math.Abs(position.y) <= alongViewSize)
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
        if (movementType == MovementType.Unrestricted || totalCount <= 0) return;
        m_ContentBounds.center = m_ViewBounds.center;
        m_ContentBounds.extents = m_ViewBounds.extents * 2;

        if (startIndex > 0 && endIndex < totalCount - 1)
            return;

        var hl = horizontal;
        var contentPosition = hl ? content.anchoredPosition.x : content.anchoredPosition.y;
        var viewExtents = m_ViewBounds.extents;

        bool hasStart = startIndex == 0;
        bool hasEnd = endIndex == totalCount - 1;

        var min = m_ContentBounds.min;
        var max = m_ContentBounds.max;

        if (hasStart)
        {
            if (hl)
            {
                min.x = boundStart - padding.x + contentPosition - viewExtents.x;
                m_ContentBounds.min = min;
            }
            else
            {
                max.y = boundStart + padding.x + contentPosition + viewExtents.y;
                m_ContentBounds.max = max;
            }
        }

        if (hasEnd)
        {
            if (hl)
            {
                max.x = boundEnd + padding.y + contentPosition - viewExtents.x;
                m_ContentBounds.max = max;
            }
            else
            {
                min.y = boundEnd - padding.y + contentPosition + viewExtents.y;
                m_ContentBounds.min = min;
            }
        }

        if (!hasStart)
        {
            if (hl)
            {
                min.x = max.x - expectTotalSize;
                m_ContentBounds.min = min;
            }
            else
            {
                max.y = min.y + expectTotalSize;
                m_ContentBounds.max = max;
            }
        }

        if (!hasEnd)
        {
            if (hl)
            {
                max.x = min.x + expectTotalSize;
                m_ContentBounds.max = max;
            }
            else
            {
                min.y = max.y - expectTotalSize;
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
                m_Scrollbar.size = Mathf.Clamp01((alongViewSize - Mathf.Abs(offset.x)) / expectTotalSize);
            else
                m_Scrollbar.size = Mathf.Clamp01(alongViewSize / expectTotalSize);
        }
        else
        {
            if (m_ContentBounds.min.y >= -m_ViewBounds.extents.y || m_ContentBounds.max.y <= m_ViewBounds.extents.y)
                m_Scrollbar.size = Mathf.Clamp01((alongViewSize - Mathf.Abs(offset.y)) / expectTotalSize);
            else
                m_Scrollbar.size = Mathf.Clamp01(alongViewSize / expectTotalSize);

        }
        m_Scrollbar.value = normalizedValue;
    }

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