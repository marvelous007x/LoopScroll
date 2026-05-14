using System;
using UnityEngine;
using static UnityEngine.UI.ScrollRect;

public class LoopScrollViewFixed : LoopScrollViewOneDirection
{
    public float size;

    protected override float normalizedValue
    {
        get
        {
            if (totalCount <= 0)
                return 0.5f;

            var postion = GetVirtualContentPosition();
            if (horizontal)
            {
                if (postion.x >= 0)
                    return 0;
                var endValue = totalSize - view.rect.width;
                if (endValue <= 0)
                    return 0;
                return Mathf.Clamp01(-postion.x / endValue);
            }
            else
            {
                if (postion.y <= 0)
                    return 0;
                var endValue = totalSize - view.rect.height;
                if (endValue <= 0)
                    return 0;
                return Mathf.Clamp01(postion.y / endValue);
            }
        }
    }

    private float totalSize;

    protected override void OnSetup()
    {
        base.OnSetup();
        var offset = size + spacing;
        totalSize = offset * totalCount - spacing;
        if (horizontal) m_VirtualContentOffset.x = -offset * startIndex;
        else m_VirtualContentOffset.y = offset * startIndex;
    }

    protected override void OnInstantiateForwardsJump()
    {
        bool hl = horizontal;
        float offsetSize = size + spacing;
        var offset = hl ? offsetSize : -offsetSize;

        float virtualPosition, virtualOffset;
        if (hl)
        {
            virtualOffset = m_VirtualContentOffset.x;
            virtualPosition = content.anchoredPosition.x + virtualOffset;
        }
        else
        {
            virtualOffset = m_VirtualContentOffset.y;
            virtualPosition = content.anchoredPosition.y + virtualOffset;
        }

        startIndex = Mathf.FloorToInt(Math.Abs(virtualPosition) / offsetSize) - 1;
        endIndex = startIndex - 1;
        startPosition = startIndex * offset + offset - virtualOffset;
        endPosition = startPosition + (hl ? -spacing : spacing);
    }

    protected override void OnInstantiateBackwardsJump()
    {
        bool hl = horizontal;
        float offsetSize = size + spacing;
        var offset = hl ? offsetSize : -offsetSize;

        float virtualPosition, virtualOffset;
        if (hl)
        {
            virtualOffset = m_VirtualContentOffset.x;
            virtualPosition = content.anchoredPosition.x + virtualOffset + view.rect.width;
        }
        else
        {
            virtualOffset = m_VirtualContentOffset.y;
            virtualPosition = content.anchoredPosition.y + virtualOffset - view.rect.height;
        }

        startIndex = Mathf.CeilToInt(Math.Abs(virtualPosition) / offsetSize);
        endIndex = startIndex - 1;
        startPosition = startIndex * offset - virtualOffset;
        endPosition = startPosition + (hl ? size : -size);
    }

    protected override void UpdateContentBounds()
    {
        if (movementType == MovementType.Unrestricted || totalCount < 0) return;
        m_ContentBounds.center = m_ViewBounds.center;
        var viewExtents = m_ViewBounds.extents;
        m_ContentBounds.extents = viewExtents * 2;
        if (startIndex <= 0 || endIndex >= totalCount - 1)
        {
            if (horizontal)
            {
                var virtualPosition = content.anchoredPosition.x + m_VirtualContentOffset.x;
                var max = m_ViewBounds.max;
                max.y += max.y;
                max.x = virtualPosition + totalSize - viewExtents.x;
                m_ContentBounds.max = max;

                var min = m_ViewBounds.min;
                min.y += min.y;
                min.x = virtualPosition - viewExtents.x;
                m_ContentBounds.min = min;
            }
            else
            {
                var virtualPosition = content.anchoredPosition.y + m_VirtualContentOffset.y;
                var min = m_ViewBounds.min;
                min.x += min.x;
                min.y = virtualPosition - totalSize + viewExtents.y;
                m_ContentBounds.min = min;

                var max = m_ViewBounds.max;
                max.x += max.x;
                max.y = virtualPosition + viewExtents.y;
                m_ContentBounds.max = max;
            }
        }
        AdjustBounds();
    }
    protected override void SetNormalizedPosition(float value)
    {
        Debug.Log(value);
        if (normalizedValue == value) return;
        var axis = horizontal ? 0 : 1;
        if (horizontal) value = -value;
        Vector2 position = content.anchoredPosition;
        var newPosition = position;
        newPosition[axis] = value * (totalSize - m_ViewBounds.size[axis]) - m_VirtualContentOffset[axis];
        if (Mathf.Abs(newPosition[axis] - position[axis]) > 0.01f)
        {
            SetContentAnchoredPosition(newPosition, true);
            m_Velocity[axis] = 0;
        }
    }

    protected override void UpdateScrollbars(Vector2 offset)
    {
        if (!m_Scrollbar || totalCount < 0)
            return;

        if (horizontal)
        {
            if (m_ContentBounds.min.x >= -m_ViewBounds.extents.x || m_ContentBounds.max.x <= m_ViewBounds.extents.x)
                m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
            else
            {
                m_Scrollbar.size = Mathf.Clamp01(m_ViewBounds.size.x / totalSize);
            }
        }
        else
        {
            if (m_ContentBounds.min.y >= -m_ViewBounds.extents.y || m_ContentBounds.max.y <= m_ViewBounds.extents.y)
                m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
            else
            {
                m_Scrollbar.size = Mathf.Clamp01(m_ViewBounds.size.y / totalSize);
            }
        }
        m_Scrollbar.value = normalizedValue;
    }

    protected override float GetItemSize(RectTransform item, int index)
    {
        return size;
    }
}