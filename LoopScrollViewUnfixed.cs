using UnityEngine;
using static UnityEngine.UI.ScrollRect;
public class LoopScrollViewUnfixed : LoopScrollViewOneDirection
{

    protected override float normalizedValue
    {
        get;
        // get
        // {
        //     if (totalCount <= 0)
        //         return 0.5f;

        //     var position = GetVirtualContentPosition();
        //     if (horizontal)
        //     {
        //         if (position.x >= 0)
        //             return 0;
        //         var endValue = GetTotalContentSize() - view.rect.width;
        //         if (endValue <= 0)
        //             return 0;
        //         return Mathf.Clamp01(-position.x / endValue);
        //     }
        //     else
        //     {
        //         if (position.y <= 0)
        //             return 0;
        //         var endValue = GetTotalContentSize() - view.rect.height;
        //         if (endValue <= 0)
        //             return 0;
        //         return Mathf.Clamp01(position.y / endValue);
        //     }
        // }
    }

    protected override void OnSetup()
    {
        base.OnSetup();
        m_Scrollbar = null;     //拖动条的位置相关处理没想好，暂时放一边
    }

    protected override void OnInstantiateForwardsJump()
    {
    }

    protected override void OnInstantiateBackwardsJump()
    {

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

    protected override void SetNormalizedPosition(float value)
    {
        // if (normalizedValue == value) return;
        // var axis = horizontal ? 0 : 1;
        // if (horizontal) value = -value;
        // Vector2 position = content.anchoredPosition;
        // var newPosition = position;

        // float totalSize = GetTotalContentSize();
        // float viewSize = horizontal ? m_ViewBounds.size.x : m_ViewBounds.size.y;
        // newPosition[axis] = value * (totalSize - viewSize) - m_VirtualContentOffset[axis];

        // if (Mathf.Abs(newPosition[axis] - position[axis]) > 0.01f)
        // {
        //     SetContentAnchoredPosition(newPosition, true);
        //     m_Velocity[axis] = 0;
        // }
    }

    protected override void UpdateScrollbars(Vector2 offset)
    {
        if (!m_Scrollbar || totalCount < 0)
            return;

        // float totalSize = GetTotalContentSize();
        // float viewSize = horizontal ? m_ViewBounds.size.x : m_ViewBounds.size.y;

        // if (horizontal)
        // {
        //     if (m_ContentBounds.min.x >= -m_ViewBounds.extents.x || m_ContentBounds.max.x <= m_ViewBounds.extents.x)
        //         m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size.x - Mathf.Abs(offset.x)) / m_ContentBounds.size.x);
        //     else
        //     {
        //         m_Scrollbar.size = Mathf.Clamp01(m_ViewBounds.size.x / totalSize);
        //     }
        // }
        // else
        // {
        //     if (m_ContentBounds.min.y >= -m_ViewBounds.extents.y || m_ContentBounds.max.y <= m_ViewBounds.extents.y)
        //         m_Scrollbar.size = Mathf.Clamp01((m_ViewBounds.size.y - Mathf.Abs(offset.y)) / m_ContentBounds.size.y);
        //     else
        //     {
        //         m_Scrollbar.size = Mathf.Clamp01(m_ViewBounds.size.y / totalSize);
        //     }
        // }
        // m_Scrollbar.value = normalizedValue;
    }

    protected override float GetItemSize(RectTransform item, int index)
    {
        return horizontal ? item.rect.width : item.rect.height;
    }
}