using System;
using UnityEngine;

public class LoopScrollFixed : LoopScrollRowOrColumn
{
    public float size;
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

            var position = GetVirtualContentPosition();
            var hl = horizontal;
            var viewSize = hl ? m_ViewBounds.size.x : m_ViewBounds.size.y;
            if (expectTotalSize <= viewSize || Mathf.Approximately(expectTotalSize, viewSize))
            {
                if (hl)
                    return position.x < 0 ? 0 : 1;
                else
                    return position.y > 0 ? 0 : 1;
            }

            if (hl)
                return -position.x / (expectTotalSize - viewSize);
            else
                return position.y / (expectTotalSize - viewSize);
        }
    }

    protected override void OnSetup(bool forwards)
    {
        base.OnSetup(forwards);
        var offset = size + spacing;
        expectTotalSize = offset * totalCount - spacing;
        if (horizontal) m_VirtualContentOffset.x = -offset * startIndex;
        else m_VirtualContentOffset.y = offset * startIndex;
    }

    protected override void Refill(bool forwards)
    {
        if (forwards)
            InstantiateForwards();
        else
            InstantiateBackwards();
        UpdateContentBounds();
    }


    protected override void SetNormalizedPosition(float value)
    {
        if (normalizedValue == value) return;
        StopMovement();
        var axis = horizontal ? 0 : 1;
        if (horizontal) value = -value;
        Vector2 position = content.anchoredPosition;
        var newPosition = position;
        newPosition[axis] = value * (expectTotalSize - m_ViewBounds.size[axis]) - m_VirtualContentOffset[axis];
        if (Mathf.Abs(newPosition[axis] - position[axis]) > 0.01f)
        {
            SetContentAnchoredPosition(newPosition, true);
            m_Velocity[axis] = 0;
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
    }

    private void OnForwardsJump()
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

        startIndex = Mathf.FloorToInt(Math.Abs(virtualPosition) / offsetSize);
        endIndex = startIndex - 1;
        startPosition = startIndex * offset + virtualOffset;
        endPosition = startPosition + (hl ? -spacing : spacing);
    }

    private void OnBackwardsJump()
    {
        bool hl = horizontal;
        float offsetSize = size + spacing;
        var offset = hl ? offsetSize : -offsetSize;

        float virtualPosition, virtualOffset;
        if (hl)
        {
            virtualOffset = m_VirtualContentOffset.x;
            virtualPosition = content.anchoredPosition.x + virtualOffset + m_ViewBounds.size.x;
        }
        else
        {
            virtualOffset = m_VirtualContentOffset.y;
            virtualPosition = content.anchoredPosition.y + virtualOffset - m_ViewBounds.size.y;
        }

        startIndex = Mathf.CeilToInt(Math.Abs(virtualPosition) / offsetSize);
        endIndex = startIndex - 1;
        startPosition = startIndex * offset + virtualOffset;
        endPosition = startPosition + (hl ? -spacing : spacing);
    }

    protected override float GetItemSize(RectTransform item, int index)
    {
        return size;
    }
}