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
            if (expectTotalSize <= alongViewSize || Mathf.Approximately(expectTotalSize, alongViewSize))
            {
                if (hl)
                    return position.x < 0 ? 0 : 1;
                else
                    return position.y > 0 ? 0 : 1;
            }

            if (hl)
                return -position.x / (expectTotalSize - alongViewSize);
            else
                return position.y / (expectTotalSize - alongViewSize);
        }
    }

    protected override void OnSetup(bool forwards)
    {
        base.OnSetup(forwards);
        var offset = size + spacing;
        expectTotalSize = offset * totalCount - spacing + padding.x + padding.y;
        var virtualOffsetSize = offset * startIndex;
        if (!forwards)
        {
            virtualOffsetSize += padding.x + padding.y - alongViewSize - spacing;
        }

        if (horizontal)
        {
            m_VirtualContentOffset.x = -virtualOffsetSize;
        }
        else
        {
            m_VirtualContentOffset.y = virtualOffsetSize;
        }
    }

    protected override void SetNormalizedPosition(float value)
    {
        if (normalizedValue == value) return;
        StopMovement();
        var axis = horizontal ? 0 : 1;
        if (horizontal) value = -value;
        Vector2 position = content.anchoredPosition;
        var newPosition = position;
        newPosition[axis] = value * (expectTotalSize - alongViewSize) - m_VirtualContentOffset[axis];
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
        if (hl)
        {
            var virtualOffset = m_VirtualContentOffset.x;
            var virtualPosition = content.anchoredPosition.x + virtualOffset;
            startIndex = Mathf.FloorToInt((-(virtualPosition + padding.x)) / offsetSize);
            startPosition = startIndex * offsetSize + padding.x + virtualOffset;
            endPosition = startPosition - spacing;
        }
        else
        {
            var virtualOffset = m_VirtualContentOffset.y;
            var virtualPosition = content.anchoredPosition.y + virtualOffset;
            startIndex = Mathf.FloorToInt((virtualPosition - padding.x) / offsetSize);
            startPosition = -startIndex * offsetSize - padding.x + virtualOffset;
            endPosition = startPosition + spacing;
        }
        endIndex = startIndex - 1;
    }

    private void OnBackwardsJump()
    {
        bool hl = horizontal;
        float offsetSize = size + spacing;

        if (hl)
        {
            var virtualOffset = m_VirtualContentOffset.x;
            var virtualPosition = content.anchoredPosition.x + virtualOffset;
            startIndex = Mathf.CeilToInt(-(virtualPosition - alongViewSize + padding.x) / offsetSize);
            startPosition = startIndex * offsetSize + padding.x + virtualOffset;
            endPosition = startPosition - spacing;

        }
        else
        {
            var virtualOffset = m_VirtualContentOffset.y;
            var virtualPosition = content.anchoredPosition.y + virtualOffset;
            startIndex = Mathf.CeilToInt((virtualPosition + alongViewSize - padding.x) / offsetSize);
            startPosition = -startIndex * offsetSize - padding.x + virtualOffset;
            endPosition = startPosition + spacing;
        }

        endIndex = startIndex - 1;
    }

    protected override float GetItemSize(RectTransform item, int index, bool isInstantiate)
    {
        return size;
    }
}