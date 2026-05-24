using System;
using UnityEngine;

public class LoopScrollGrid : LoopScrollHorizontalOrVertical
{
    public enum Alignment
    {
        FromStart,
        Center,
        ToEnd
    }

    public Vector2 cellSize, spacing;
    public int anotherCount = 2;
    public bool anotherCountFlexible;
    public Alignment startSide;

    private float anotherPositionOffset;

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

        if (cellSize.x <= 0 || cellSize.y <= 0)
            throw new Exception("Item cell size must be greater than zero");

        if (cellSize.x + spacing.x <= 0 || cellSize.y + spacing.y <= 0)
            throw new Exception("Item cell size + spacing must be greater than zero");

        base.OnSetup(forwards);
        RefreshAnotherValues();
        if (forwards)
        {
            startPosition = 0;
            startIndex = startIndex / anotherCount * anotherCount;
            endPosition = horizontal ? -spacing.x : spacing.y;
        }
        else
        {
            startIndex = endIndex / anotherCount * anotherCount + anotherCount;
            if (totalCount > 0) startIndex = Math.Min(startIndex, totalCount);
            if (horizontal)
            {
                endPosition = alongViewSize;
                startPosition = endPosition + spacing.x;
            }
            else
            {
                endPosition = -alongViewSize;
                startPosition = endPosition - spacing.y;
            }
        }
        endIndex = startIndex - 1;

        var count = (totalCount - 1) / anotherCount + 1;
        if (horizontal)
        {
            var offset = cellSize.x + spacing.x;
            expectTotalSize = offset * count - spacing.x;
            m_VirtualContentOffset.x = -offset * (startIndex / anotherCount) + startPosition;
        }
        else
        {
            var offset = cellSize.y + spacing.y;
            expectTotalSize = offset * count - spacing.y;
            m_VirtualContentOffset.y = offset * (startIndex / anotherCount) + startPosition;
        }
    }

    private void RefreshAnotherValues()
    {
        RefreshFlexAnotherSize();
        RefreshAnotherPostionOffset();
    }

    private void RefreshFlexAnotherSize()
    {
        if (!anotherCountFlexible) return;
        float size, anotherSize, anotherSpacing;
        if (horizontal)
        {
            size = content.rect.height;
            anotherSize = cellSize.y;
            anotherSpacing = spacing.y;
        }
        else
        {
            size = content.rect.width;
            anotherSize = cellSize.x;
            anotherSpacing = spacing.x;
        }

        anotherCount = Mathf.FloorToInt((size + anotherSpacing) / (anotherSize + anotherSpacing));
        if (anotherCount <= 0) anotherCount = 1;
    }

    private void RefreshAnotherPostionOffset()
    {
        var hl = horizontal;
        float contentSize;

        if (hl)
        {
            contentSize = content.rect.height;
            anotherPositionOffset = ((itemAnchorMin.y + itemAnchorMax.y) / 2 - 1) * contentSize;
        }
        else
        {
            contentSize = content.rect.width;
            anotherPositionOffset = -(itemAnchorMin.x + itemAnchorMax.x) / 2 * contentSize;
        }

        if (startSide == Alignment.FromStart)
            return;

        float size, anotherSize, anotherSpacing;
        if (hl)
        {
            anotherSize = cellSize.y;
            anotherSpacing = spacing.y;
        }
        else
        {
            anotherSize = cellSize.x;
            anotherSpacing = spacing.x;
        }
        size = (anotherSize + anotherSpacing) * anotherCount - anotherSpacing;
        if (startSide == Alignment.ToEnd)
            anotherPositionOffset += contentSize - size;
        else
            anotherPositionOffset += (contentSize - size) / 2;
    }

    protected override void Refill(bool forwards)
    {
        if (forwards)
            InstantiateForwards();
        else
            InstantiateBackwards();
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
        if (horizontal)
        {
            var offset = cellSize.x + spacing.x;
            var virtualPosition = content.anchoredPosition.x + m_VirtualContentOffset.x;
            startIndex = Mathf.FloorToInt(Math.Abs(virtualPosition) / offset);
            startPosition = startIndex * offset + m_VirtualContentOffset.x;
            endPosition = startPosition - spacing.x;
        }
        else
        {
            var offset = cellSize.y + spacing.y;
            var virtualPosition = content.anchoredPosition.y + m_VirtualContentOffset.y;
            startIndex = Mathf.FloorToInt(Math.Abs(virtualPosition) / offset);
            startPosition = startIndex * -offset + m_VirtualContentOffset.y;
            endPosition = startPosition + spacing.y;
        }
        endIndex = startIndex - 1;
    }

    private void OnBackwardsJump()
    {
        if (horizontal)
        {
            var offset = cellSize.x + spacing.x;
            var virtualPosition = content.anchoredPosition.x + m_VirtualContentOffset.x - alongViewSize;
            startIndex = Mathf.CeilToInt(Math.Abs(virtualPosition) / offset);
            startPosition = startIndex * offset + m_VirtualContentOffset.x;
            endPosition = startPosition - spacing.x;
        }
        else
        {
            var offset = cellSize.y + spacing.y;
            var virtualPosition = content.anchoredPosition.y + m_VirtualContentOffset.y + alongViewSize;
            startIndex = Mathf.CeilToInt(Math.Abs(virtualPosition) / offset);
            startPosition = startIndex * -offset + m_VirtualContentOffset.y;
            endPosition = startPosition + spacing.y;
        }
        startIndex *= anotherCount;
        endIndex = startIndex - 1;
    }

    protected void ReleaseForwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;
        bool hl = horizontal;
        var border = hl ? -position.x : -position.y;
        for (int i = 0; i < count;)
        {
            float itemEndPosition = hl ? (startPosition + cellSize.x) : (startPosition - cellSize.y);
            if (hl)
            {
                if (itemEndPosition > border)
                    break;
            }
            else
            {
                if (itemEndPosition < border)
                    break;
            }

            int lineStartIndex = startIndex;
            int lineEndIndex = startIndex + anotherCount - 1;
            if (totalCount > 0)
                lineEndIndex = Mathf.Min(lineEndIndex, endIndex);

            for (int j = lineStartIndex; j <= lineEndIndex; j++)
            {
                var item = content.GetChild(0) as RectTransform;
                ReleaseItem(item, startIndex++);
                i++;
            }
            startPosition = hl ? (itemEndPosition + spacing.x) : (itemEndPosition - spacing.y);
        }
    }

    protected void InstantiateForwards()
    {
        if (totalCount == 0) return;
        bool hl = horizontal;
        var count = content.childCount;
        var contentPosition = hl ? content.anchoredPosition.x : content.anchoredPosition.y;

        float border = hl ? (alongViewSize - contentPosition) : (-alongViewSize - contentPosition);
        var p = (prefabSource.template.transform as RectTransform).anchoredPosition;
        ref float value = ref (hl ? ref p.x : ref p.y);
        ref float anotherValue = ref (hl ? ref p.y : ref p.x);
        var spacingOffset = hl ? spacing.x : -spacing.y;
        while (true)
        {
            if (totalCount > 0 && endIndex >= totalCount - 1)
                break;
            var itemStartPosition = endPosition + spacingOffset;
            if (hl && itemStartPosition > border)
                break;
            if (!hl && itemStartPosition < border)
                break;

            int lineStartIndex = endIndex + 1;
            int lineEndIndex = endIndex + anotherCount;
            if (totalCount > 0)
                lineEndIndex = Mathf.Min(lineEndIndex, totalCount - 1);

            float lineStartValue = anotherPositionOffset;
            for (int i = lineStartIndex; i <= lineEndIndex; i++)
            {
                var item = InstantiateItem(++endIndex); ;
                onRefreshItem?.Invoke(item, endIndex);
                var pivot = item.pivot;
                if (hl)
                {
                    value = itemStartPosition - LoopScrollHelper.GetAnchoredLeftOffset(cellSize.x, pivot.x);
                    anotherValue = -lineStartValue - LoopScrollHelper.GetAnchoredTopOffset(cellSize.y, pivot.y);
                    lineStartValue += cellSize.y + spacing.y;
                }
                else
                {
                    value = itemStartPosition - LoopScrollHelper.GetAnchoredTopOffset(cellSize.y, pivot.y);
                    anotherValue = lineStartValue - LoopScrollHelper.GetAnchoredLeftOffset(cellSize.x, pivot.x);
                    lineStartValue += cellSize.x + spacing.x;
                }
                item.anchoredPosition = p;
            }

            if (hl)
            {
                endPosition = itemStartPosition + cellSize.x;
            }
            else
            {
                endPosition = itemStartPosition - cellSize.y;
            }

            if (totalCount > 0)
            {
                if (lineStartIndex == 0)
                {
                    boundStart = itemStartPosition;
                }

                if (endIndex == totalCount - 1)
                {
                    boundEnd = endPosition;
                }
            }
        }
    }

    protected void ReleaseBackwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;

        bool hl = horizontal;
        var border = hl ? alongViewSize - position.x : -alongViewSize - position.y;

        for (int i = count - 1; i >= 0;)
        {
            float itemStartPosition = hl ? (endPosition - cellSize.x) : (endPosition + cellSize.y);
            if (hl)
            {
                if (itemStartPosition < border)
                    break;
            }
            else
            {
                if (itemStartPosition > border)
                    break;
            }

            int lineEndIndex = endIndex;
            int lineStartIndex = (totalCount < 0) ? endIndex - anotherCount + 1 : endIndex / anotherCount * anotherCount;
            for (int j = lineEndIndex; j >= lineStartIndex; j--)
            {
                var item = content.GetChild(i) as RectTransform;
                ReleaseItem(item, endIndex--);
                i--;
            }

            if (hl) endPosition = itemStartPosition - spacing.x;
            else endPosition = itemStartPosition + spacing.y;
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
        ref float anotherValue = ref (hl ? ref p.y : ref p.x);
        float spacingOffset = hl ? -spacing.x : spacing.y;

        while (true)
        {
            if (totalCount > 0 && startIndex <= 0) break;
            var itemEndPosition = startPosition + spacingOffset;
            if (hl)
            {
                if (itemEndPosition < border)
                    break;
            }
            else
            {
                if (itemEndPosition > border)
                    break;
            }


            float offsetSize;
            if (hl)
            {
                offsetSize = cellSize.y + spacing.y;
            }
            else
            {
                offsetSize = cellSize.x + spacing.x;
            }
            float lineStartValue;

            int lineStartIndex;
            int lineEndIndex = startIndex - 1;
            if (totalCount > 0 && startIndex >= totalCount)
            {
                lineStartIndex = (startIndex - 1) / anotherCount * anotherCount;
                lineStartValue = anotherPositionOffset + offsetSize * (lineEndIndex - lineStartIndex);
            }
            else
            {
                lineStartIndex = startIndex - anotherCount;
                lineStartValue = anotherPositionOffset + offsetSize * (anotherCount - 1);
            }

            for (int j = lineEndIndex; j >= lineStartIndex; j--)
            {
                var item = InstantiateItem(--startIndex);
                item.SetAsFirstSibling();
                onRefreshItem?.Invoke(item, startIndex);
                var pivot = item.pivot;
                if (hl)
                {
                    value = itemEndPosition - LoopScrollHelper.GetAnchoredRightOffset(cellSize.x, pivot.x);
                    anotherValue = -lineStartValue - LoopScrollHelper.GetAnchoredTopOffset(cellSize.y, pivot.y);
                    lineStartValue -= cellSize.y + spacing.y;
                }
                else
                {
                    value = itemEndPosition - LoopScrollHelper.GetAnchoredBottomOffset(cellSize.y, pivot.y);
                    anotherValue = lineStartValue - LoopScrollHelper.GetAnchoredLeftOffset(cellSize.x, pivot.y);
                    lineStartValue -= cellSize.x + spacing.x;
                }

                item.anchoredPosition = p;
            }

            if (hl)
            {
                startPosition = itemEndPosition - cellSize.x;
            }
            else
            {
                startPosition = itemEndPosition + cellSize.y;
            }

            if (totalCount > 0)
            {
                if (lineStartIndex == 0)
                {
                    boundStart = startPosition;
                }

                if (lineEndIndex == totalCount - 1)
                {
                    boundEnd = itemEndPosition;
                }
            }
        }
    }
}