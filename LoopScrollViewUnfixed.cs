using UnityEngine;
using static UnityEngine.UI.ScrollRect;

public class LoopScrollViewUnfixed : LoopScrollViewOneDirection
{
    private float startPosition, endPosition;
    private float boundStart, boundEnd;
    private bool everReachStart, everReachEnd;

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

    protected override void Refill()
    {
        m_Scrollbar = null;     //拖动条的位置相关处理没想好，暂时放一边
        startPosition = 0;
        everReachStart = everReachEnd = false;
        endPosition = horizontal ? -spacing : spacing;
        InstantiateForwards();
        UpdateContentBounds();
        UpdatePrevData();
    }

    protected override void ReleaseForwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;
        bool hl = horizontal;
        var border = hl ? -content.anchoredPosition.x : -content.anchoredPosition.y;
        float itemEndPosition;
        for (int i = 0; i < count; i++)
        {
            var item = content.GetChild(0) as RectTransform;
            var size = hl ? item.rect.width : item.rect.height;
            if (hl)
            {
                itemEndPosition = item.anchoredPosition.x + LoopScrollViewHelper.GetAnchoredRightOffset(size, item.pivot.x);
                if (itemEndPosition <= border)
                {
                    ReleaseItem(startIndex++, item);
                    startPosition = itemEndPosition + spacing;
                    continue;
                }
            }
            else
            {
                itemEndPosition = item.anchoredPosition.y + LoopScrollViewHelper.GetAnchoredBottomOffset(size, item.pivot.y);
                if (itemEndPosition >= border)
                {
                    ReleaseItem(startIndex++, item);
                    startPosition = itemEndPosition - spacing;
                    continue;
                }
            }
            break;
        }
    }

    protected override void InstantiateForwards(bool jump = false)
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
            var item = InstantiateItem();
            endIndex++;
            item.name = endIndex.ToString();
            onRefreshItem?.Invoke(endIndex, item);
            var pivot = item.pivot;
            var size = hl ? item.rect.width : item.rect.height;
            if (hl)
            {
                value = itemStartPosition - LoopScrollViewHelper.GetAnchoredLeftOffset(size, pivot.x);
                endPosition = itemStartPosition + size;
            }
            else
            {
                value = itemStartPosition - LoopScrollViewHelper.GetAnchoredTopOffset(size, pivot.y);
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
        }
    }

    protected override void ReleaseBackwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;

        bool hl = horizontal;
        var border = hl ? view.rect.width - content.anchoredPosition.x : -view.rect.height - content.anchoredPosition.y;

        float itemStartPosition;
        for (int i = count - 1; i >= 0; i--)
        {
            var item = content.GetChild(i) as RectTransform;
            var size = hl ? item.rect.width : item.rect.height;
            if (hl)
            {
                itemStartPosition = item.anchoredPosition.x + LoopScrollViewHelper.GetAnchoredLeftOffset(size, item.pivot.x);
                if (itemStartPosition >= border)
                {
                    ReleaseItem(endIndex--, item);
                    endPosition = itemStartPosition - spacing;
                    continue;
                }
            }
            else
            {
                itemStartPosition = item.anchoredPosition.y + LoopScrollViewHelper.GetAnchoredTopOffset(size, item.pivot.y);
                if (itemStartPosition <= border)
                {
                    ReleaseItem(endIndex--, item);
                    endPosition = itemStartPosition + spacing;
                    continue;
                }
            }
            break;
        }
    }

    protected override void InstantiateBackwards(bool jump = false)
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
            var item = InstantiateItem();
            item.SetAsFirstSibling();
            startIndex--;
            item.name = startIndex.ToString();
            onRefreshItem?.Invoke(startIndex, item);
            var size = hl ? item.rect.width : item.rect.height;
            if (hl)
            {
                value = itemEndPosition - LoopScrollViewHelper.GetAnchoredRightOffset(size, item.pivot.x);
                startPosition = itemEndPosition - size;
            }
            else
            {
                value = itemEndPosition - LoopScrollViewHelper.GetAnchoredBottomOffset(size, item.pivot.y);
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
        }
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

    protected override void OnContentRepsoitioned(Vector2 offset)
    {
        var add = horizontal ? offset.x : offset.y;
        startPosition += add;
        endPosition += add;
        boundStart += add;
        boundEnd += add;
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

}