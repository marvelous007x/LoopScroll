using System;
using UnityEngine;
using static UnityEngine.UI.ScrollRect;

public class LoopScrollViewFixed : LoopScrollViewOneDirection
{
    public float size;
    public float spacing;

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

    private float totalSize, itemOffset;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (scrollbar)
            scrollbar.onValueChanged.AddListener(SetNormalizedPosition);
    }

    protected override void Refill()
    {
        itemOffset = size + spacing;
        totalSize = itemOffset * totalCount - spacing;
        if (horizontal) m_VirtualContentOffset.x = -itemOffset * startIndex;
        else m_VirtualContentOffset.y = itemOffset * startIndex;
        InstantiateForwards();
        UpdateContentBounds();
        UpdatePrevData();
    }

    private void ReleaseForwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;
        bool hl = horizontal;
        float value;
        if (hl)
        {
            value = position.x + content.rect.xMin;
            value += GetRectRight(content.GetChild(0) as RectTransform);
        }
        else
        {
            value = position.y + content.rect.yMax;
            value += GetRectBottom(content.GetChild(0) as RectTransform);
        }
        for (int i = 0; i < count; i++)
        {
            var item = content.GetChild(0) as RectTransform;
            if (hl)
            {
                if (value <= 0)
                {
                    ReleaseItem(startIndex++, item);
                    value += itemOffset;
                    continue;
                }
            }
            else
            {
                if (value >= 0)
                {
                    ReleaseItem(startIndex++, item);
                    value -= itemOffset;
                    continue;
                }
            }
            break;
        }
    }

    private void InstantiateForwards(bool jump = false)
    {
        if (totalCount == 0) return;
        bool hl = horizontal;
        var count = content.childCount;
        float startValue = 0;
        float offset = itemOffset;
        var anchorValue = hl ? content.anchoredPosition.x : content.anchoredPosition.y;
        if (!hl) offset = -offset;
        if (count > 0)
        {
            var rt = content.GetChild(count - 1) as RectTransform;
            startValue = hl ? GetRectLeft(rt) : GetRectTop(rt);
            startValue += offset;
        }
        else
        {
            startValue = offset * endIndex;
            startValue += hl ? m_VirtualContentOffset.x : m_VirtualContentOffset.y;
            if (jump)
            {
                var startBorder = (horizontal ? -size : size) + anchorValue;
                var add = Mathf.FloorToInt(Math.Abs(startBorder - startValue) / itemOffset);
                startIndex += add;
                endIndex += add;
                startValue += add * offset;
            }
            startValue += offset;
        }

        float border = hl ? (view.rect.width - anchorValue) : (-view.rect.height - anchorValue);
        var templateRt = prefabSource.template.transform as RectTransform;
        var anchorMin = templateRt.anchorMin;
        var anchorMax = templateRt.anchorMax;
        var p = templateRt.anchoredPosition;
        ref float value = ref (hl ? ref p.x : ref p.y);
        if (hl)
        {
            anchorMin.x = 0;
            anchorMax.x = 0;
        }
        else
        {
            anchorMin.y = 1;
            anchorMax.y = 1;
        }

        while (true)
        {
            if (totalCount > 0 && endIndex >= totalCount - 1)
                break;
            if (hl && startValue >= border)
                break;
            if (!hl && startValue <= border)
                break;
            var item = InstantiateItem();
            item.anchorMin = anchorMin;
            item.anchorMax = anchorMax;
            value = startValue + (hl ? item.pivot.x : (item.pivot.y - 1)) * size;
            item.anchoredPosition = p;
            endIndex++;
            item.name = endIndex.ToString();//zzz
            onRefreshItem?.Invoke(endIndex, item);
            startValue += offset;
        }
    }

    private void ReleaseBackwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;

        bool hl = horizontal;
        float value, border;
        if (hl)
        {
            border = view.rect.width;
            value = position.x + content.rect.xMin;
            value += GetRectLeft(content.GetChild(count - 1) as RectTransform);
        }
        else
        {
            border = -view.rect.height;
            value = position.y + content.rect.yMax;
            value += GetRectTop(content.GetChild(count - 1) as RectTransform);
        }

        for (int i = count - 1; i >= 0; i--)
        {
            var item = content.GetChild(i) as RectTransform;
            if (hl)
            {
                if (value >= border)
                {
                    ReleaseItem(endIndex--, item);
                    value -= itemOffset;
                    continue;
                }
            }
            else
            {
                if (value <= border)
                {
                    ReleaseItem(endIndex--, item);
                    value += itemOffset;
                    continue;
                }
            }
            break;
        }
    }

    private void InstantiateBackwards(bool jump = false)
    {
        if (totalCount == 0) return;
        bool hl = horizontal;
        var count = content.childCount;
        var anchorValue = hl ? content.anchoredPosition.x : content.anchoredPosition.y;
        float startValue;
        float offset = itemOffset;
        if (!hl) offset = -offset;
        if (count > 0)
        {
            var rt = content.GetChild(0) as RectTransform;
            startValue = hl ? GetRectRight(rt) : GetRectBottom(rt);
            startValue -= offset;
        }
        else
        {
            startValue = offset * startIndex + (hl ? size : -size);
            startValue += hl ? m_VirtualContentOffset.x : m_VirtualContentOffset.y;

            if (jump)
            {
                var startBorder = (horizontal ? view.rect.width + size : -view.rect.height - size) - anchorValue;
                var add = Mathf.FloorToInt(Math.Abs(startBorder - startValue) / itemOffset);
                startIndex -= add;
                endIndex -= add;
                startValue -= add * offset;
            }
            startValue -= offset;
        }
        float border = -anchorValue;
        var templateRt = prefabSource.template.transform as RectTransform;
        var anchorMin = templateRt.anchorMin;
        var anchorMax = templateRt.anchorMax;
        var p = templateRt.anchoredPosition;

        ref float value = ref (hl ? ref p.x : ref p.y);
        if (hl)
        {
            anchorMin.x = 0;
            anchorMax.x = 0;
        }
        else
        {
            anchorMin.y = 1;
            anchorMax.y = 1;
        }

        while (true)
        {
            if (totalCount > 0 && startIndex <= 0) break;
            if (hl && startValue <= border) break;
            if (!hl && startValue >= border) break;
            var item = InstantiateItem();
            item.SetAsFirstSibling();
            item.anchorMin = anchorMin;
            item.anchorMax = anchorMax;
            value = startValue + (hl ? (item.pivot.x - 1) : item.pivot.y) * size;

            item.anchoredPosition = p;
            startIndex--;
            item.name = startIndex.ToString();//zzz
            onRefreshItem?.Invoke(startIndex, item);
            startValue -= offset;
        }
    }

    protected override void SetContentAnchoredPosition(Vector2 position, bool jump = false)
    {
        Vector2 currentPosition = content.anchoredPosition;
        bool forward;
        if (horizontal)
        {
            if (position.x == currentPosition.x) return;
            forward = position.x < currentPosition.x;
            position.y = currentPosition.y;
        }
        else
        {
            if (position.y == currentPosition.y) return;
            forward = position.y > currentPosition.y;
            position.x = currentPosition.x;
        }

        if (forward)
        {
            ReleaseForwards(position);
            RepositionContent(position);
            InstantiateForwards(jump);
        }
        else
        {
            ReleaseBackwards(position);
            RepositionContent(position);
            InstantiateBackwards(jump);
        }
        UpdateContentBounds();
    }

    protected override void UpdateContentBounds()
    {
        if (movementType == MovementType.Unrestricted || totalCount < 0) return;
        m_ContentBounds.center = m_ViewBounds.center;
        m_ContentBounds.extents = m_ViewBounds.extents * 2;
        if (startIndex <= 0 || endIndex >= totalCount - 1)
        {
            if (horizontal)
            {
                var max = m_ViewBounds.max;
                max.y += max.y;
                max.x = totalSize + m_VirtualContentOffset.x + content.anchoredPosition.x - max.x;
                m_ContentBounds.max = max;

                var min = m_ViewBounds.min;
                min.y += min.y;
                min.x += content.anchoredPosition.x + m_VirtualContentOffset.x;
                m_ContentBounds.min = min;
            }
            else
            {
                var min = m_ViewBounds.min;
                min.x += min.x;
                min.y = -totalSize + m_VirtualContentOffset.y + content.anchoredPosition.y - min.y;
                m_ContentBounds.min = min;

                var max = m_ViewBounds.max;
                max.x += max.x;
                max.y += content.anchoredPosition.y + m_VirtualContentOffset.y;
                m_ContentBounds.max = max;
            }
        }
        AdjustBounds();
    }

    protected override void SetNormalizedPosition(float value)
    {
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
}