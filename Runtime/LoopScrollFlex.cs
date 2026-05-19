using System;
using UnityEngine;
public class LoopScrollFlex : LoopScrollRowOrColumn
{
    private float[] sizes;
    private float expectAverageSize, average;
    private bool noContentUpdate;
    private float pNormalizedValue;

    protected override float expectTotalSize
    {
        get;
        set;
    }

    protected override float normalizedValue
    {
        get
        {
            if (totalCount <= 0)
                return 0.5f;

            var hl = horizontal;
            var viewSize = hl ? m_ViewBounds.size.x : m_ViewBounds.size.y;
            var position = startPosition +
                (hl ? -startIndex * (expectAverageSize + spacing) + content.anchoredPosition.x
                    : startIndex * (expectAverageSize + spacing) + content.anchoredPosition.y);

            if (expectTotalSize <= viewSize || Mathf.Approximately(expectTotalSize, viewSize))
            {
                if (hl)
                    return position < 0 ? 0 : 1;
                else
                    return position > 0 ? 0 : 1;
            }

            if (hl)
                return -position / (expectTotalSize - viewSize);
            else
                return position / (expectTotalSize - viewSize);
        }
    }

    protected override void OnSetup(bool forwards)
    {
        base.OnSetup(forwards);
        if (totalCount <= 0) return;

        var item = prefabSource.template.transform as RectTransform;
        float size, viewSize;
        if (horizontal)
        {
            size = item.rect.width;
            viewSize = view.rect.width;
        }
        else
        {
            size = item.rect.height;
            viewSize = view.rect.height;
        }

        size = Mathf.Max(0, size);
        var count = Mathf.CeilToInt(viewSize / (size + spacing) * 1.5f);
        count = Math.Min(Math.Max(count, 10), totalCount);
        sizes = new float[count];
        Array.Fill(sizes, size);
        expectAverageSize = average = size;
        expectTotalSize = (size + spacing) * totalCount - spacing;
    }

    protected override void Refill(bool fowards)
    {
        if (fowards)
            InstantiateForwards();
        else
            InstantiateBackwards();
        UpdateContentBounds();
        UpdateContentSize();
        UpdatePrevData();
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
            if (jump && content.childCount == 0)
                OnForwardsJump();
            InstantiateForwards();
        }
        else
        {
            ReleaseBackwards(position);
            RepositionContent(position);
            if (jump && content.childCount == 0)
                OnBackwardsJump();
            InstantiateBackwards();
        }
        UpdateContentBounds();
        UpdateContentSize();
        noContentUpdate = true;
        UpdateScrollbars(Vector2.zero);
        noContentUpdate = false;
    }

    protected override void SetNormalizedPosition(float value)
    {
        if (noContentUpdate) return;
        if (normalizedValue == value) return;
        StopMovement();

        Vector2 position = content.anchoredPosition;
        var newPosition = position;
        float offset;
        if (horizontal)
        {
            offset = (value - pNormalizedValue) * (expectTotalSize - m_ViewBounds.size.x);
            newPosition.x -= offset;
        }
        else
        {
            offset = (value - pNormalizedValue) * (expectTotalSize - m_ViewBounds.size.y);
            newPosition.y += offset;
        }

        if (Mathf.Abs(offset) > 0.01f)
        {
            SetContentAnchoredPosition(newPosition, true);
        }
    }

    protected void OnForwardsJump()
    {
        if (endIndex >= totalCount - 2) return;
        bool hl = horizontal;
        float offsetSize = expectAverageSize + spacing;
        var offset = hl ? offsetSize : -offsetSize;
        var contentPosition = hl ? content.anchoredPosition.x : content.anchoredPosition.y;
        var position = Math.Abs(endPosition + contentPosition);
        int count = Mathf.FloorToInt(position / offsetSize);
        endIndex += count;
        startIndex = endIndex + 1;
        endPosition += offset * count;
        startPosition = endPosition + (hl ? spacing : -spacing);
    }

    protected void OnBackwardsJump()
    {
        if (startIndex <= 0) return;
        bool hl = horizontal;
        float offsetSize = expectAverageSize + spacing;
        var offset = hl ? -offsetSize : offsetSize;
        var contentPosition = hl ? content.anchoredPosition.x : content.anchoredPosition.y;
        var position = startPosition + contentPosition;
        int count;
        if (horizontal)
        {
            count = Mathf.FloorToInt((position - m_ViewBounds.size.x) / offsetSize);
        }
        else
        {
            count = Mathf.FloorToInt((-position - m_ViewBounds.size.y) / offsetSize);
        }

        startIndex -= count;
        endIndex = startIndex - 1;
        startPosition += offset * count;
        endPosition = startPosition + (hl ? -spacing : spacing);
    }

    protected override float GetItemSize(RectTransform item, int index)
    {
        return horizontal ? item.rect.width : item.rect.height;
    }

    protected override void OnInstantiatedItem(RectTransform item, int index, float size)
    {
        if (totalCount <= 0) return;
        index %= sizes.Length;
        var pValue = sizes[index];
        if (pValue != size)
        {
            sizes[index] = size;
            average += (size - pValue) / sizes.Length;
        }
    }

    protected void UpdateContentSize()
    {
        if (totalCount <= 0) return;
        expectAverageSize = average;
        var visibleSize = horizontal ? endPosition - startPosition : startPosition - endPosition;
        expectTotalSize = (startIndex + totalCount - 1 - endIndex) * (average + spacing) + visibleSize;
    }

    protected override void UpdateScrollbars(Vector2 offset)
    {
        base.UpdateScrollbars(offset);
        pNormalizedValue = m_Scrollbar.value;
    }
}