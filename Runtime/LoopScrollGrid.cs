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

    [SerializeField]
    private Vector2 m_CellSize, m_Spacing;
    [SerializeField]
    private int m_AnotherCount = 2;
    [SerializeField]
    private bool m_AnotherCountFlexible;
    public Alignment startSide;

    public Vector2 spacing
    {
        get => m_Spacing;
        set
        {
            if (m_Spacing == value) return;
            m_Spacing = value;
            RefreshAnotherValues();
        }
    }

    public Vector2 cellSize
    {
        get => m_CellSize;
        set
        {
            if (m_CellSize == value) return;
            m_CellSize = value;
            RefreshAnotherValues();
        }
    }

    public bool anotherCountFlexible
    {
        get => m_AnotherCountFlexible;
        set
        {
            if (m_AnotherCountFlexible == value) return;
            m_AnotherCountFlexible = value;
            RefreshAnotherValues();
        }
    }

    public int anotherCount
    {
        get => m_AnotherCount;
        set
        {
            if (m_AnotherCount == value) return;
            m_AnotherCount = value;
            m_AnotherCountFlexible = false;
        }
    }

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
        RefreshAnotherValues();
        if (forwards)
        {
            startPosition = 0;
            startIndex = startIndex / m_AnotherCount * m_AnotherCount;
            endPosition = horizontal ? -m_Spacing.x : m_Spacing.y;
        }
        else
        {
            startIndex = endIndex / m_AnotherCount * m_AnotherCount + m_AnotherCount;
            if (totalCount > 0) startIndex = Math.Min(startIndex, totalCount);
            if (horizontal)
            {
                endPosition = view.rect.width;
                startPosition = endPosition + m_Spacing.x;
            }
            else
            {
                endPosition = -view.rect.height;
                startPosition = endPosition - m_Spacing.y;
            }
        }
        endIndex = startIndex - 1;

        var count = (totalCount - 1) / m_AnotherCount + 1;
        if (horizontal)
        {
            var offset = m_CellSize.x + m_Spacing.x;
            expectTotalSize = offset * count - m_Spacing.x;
            m_VirtualContentOffset.x = -offset * (startIndex / m_AnotherCount) + startPosition;
        }
        else
        {
            var offset = m_CellSize.y + m_Spacing.y;
            expectTotalSize = offset * count - m_Spacing.y;
            m_VirtualContentOffset.y = offset * (startIndex / m_AnotherCount) + startPosition;
        }
    }

    private void RefreshAnotherValues()
    {
        RefreshFlexAnotherSize();
        RefreshAnotherPostionOffset();
    }

    private void RefreshFlexAnotherSize()
    {
        if (!m_AnotherCountFlexible) return;
        float size, anotherSize, anotherSpacing;
        if (horizontal)
        {
            size = view.rect.height;
            anotherSize = m_CellSize.y;
            anotherSpacing = spacing.y;
        }
        else
        {
            size = view.rect.width;
            anotherSize = m_CellSize.x;
            anotherSpacing = spacing.x;
        }
        m_AnotherCount = Mathf.FloorToInt((size + anotherSpacing) / (anotherSize + anotherSpacing));
        if (m_AnotherCount <= 0) m_AnotherCount = 1;
    }

    private void RefreshAnotherPostionOffset()
    {
        var hl = horizontal;
        float contentSize;

        if (hl)
        {
            contentSize = view.rect.height;
            anotherPositionOffset = ((itemAnchorMin.y + itemAnchorMax.y) / 2 - 1) * contentSize;
        }
        else
        {
            contentSize = view.rect.width;
            anotherPositionOffset = -(itemAnchorMin.x + itemAnchorMax.x) / 2 * contentSize;
        }

        if (startSide == Alignment.FromStart)
            return;

        float size, anotherSize, anotherSpacing;
        if (hl)
        {
            anotherSize = m_CellSize.y;
            anotherSpacing = m_Spacing.y;
        }
        else
        {
            anotherSize = m_CellSize.x;
            anotherSpacing = m_Spacing.x;
        }
        size = (anotherSize + anotherSpacing) * m_AnotherCount - anotherSpacing;
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
            var offset = m_CellSize.x + spacing.x;
            var virtualPosition = content.anchoredPosition.x + m_VirtualContentOffset.x;
            startIndex = Mathf.FloorToInt(Math.Abs(virtualPosition) / offset);
            startPosition = startIndex * offset + m_VirtualContentOffset.x;
            endPosition = startPosition - spacing.x;
        }
        else
        {
            var offset = m_CellSize.y + spacing.y;
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
            var offset = m_CellSize.x + spacing.x;
            var virtualPosition = content.anchoredPosition.x + m_VirtualContentOffset.x + m_ViewBounds.size.x;
            startIndex = Mathf.CeilToInt(Math.Abs(virtualPosition) / offset);
            startPosition = startIndex * offset + m_VirtualContentOffset.x;
            endPosition = startPosition - spacing.x;
        }
        else
        {
            var offset = m_CellSize.y + spacing.y;
            var virtualPosition = content.anchoredPosition.y + m_VirtualContentOffset.y - m_ViewBounds.size.y;
            startIndex = Mathf.CeilToInt(Math.Abs(virtualPosition) / offset);
            startPosition = startIndex * -offset + m_VirtualContentOffset.y;
            endPosition = startPosition + spacing.y;
        }
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
            float itemEndPosition = hl ? (startPosition + m_CellSize.x) : (startPosition - m_CellSize.y);
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
            int lineEndIndex = startIndex + m_AnotherCount - 1;
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

        float border = hl ? (view.rect.width - contentPosition) : (-view.rect.height - contentPosition);
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
            int lineEndIndex = endIndex + m_AnotherCount;
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
                    value = itemStartPosition - LoopScrollHelper.GetAnchoredLeftOffset(m_CellSize.x, pivot.x);
                    anotherValue = -lineStartValue - LoopScrollHelper.GetAnchoredTopOffset(m_CellSize.y, pivot.y);
                    lineStartValue += m_CellSize.y + m_Spacing.y;
                }
                else
                {
                    value = itemStartPosition - LoopScrollHelper.GetAnchoredTopOffset(m_CellSize.y, pivot.y);
                    anotherValue = lineStartValue - LoopScrollHelper.GetAnchoredLeftOffset(m_CellSize.x, pivot.x);
                    lineStartValue += m_CellSize.x + m_Spacing.x;
                }
                item.anchoredPosition = p;
            }

            if (hl)
            {
                endPosition = itemStartPosition + m_CellSize.x;
            }
            else
            {
                endPosition = itemStartPosition - m_CellSize.y;
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
        var border = hl ? view.rect.width - position.x : -view.rect.height - position.y;

        for (int i = count - 1; i >= 0;)
        {
            float itemStartPosition = hl ? (endPosition - m_CellSize.x) : (endPosition + m_CellSize.y);
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
            int lineStartIndex = (totalCount < 0) ? endIndex - m_AnotherCount + 1 : endIndex / m_AnotherCount * m_AnotherCount;
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
                offsetSize = m_CellSize.y + m_Spacing.y;
            }
            else
            {
                offsetSize = m_CellSize.x + m_Spacing.x;
            }
            float lineStartValue;

            int lineStartIndex;
            int lineEndIndex = startIndex - 1;
            if (totalCount > 0 && startIndex >= totalCount)
            {
                lineStartIndex = startIndex / m_AnotherCount * m_AnotherCount;
                lineStartValue = anotherPositionOffset + offsetSize * (lineEndIndex - lineStartIndex);
            }
            else
            {
                lineStartIndex = startIndex - m_AnotherCount;
                lineStartValue = anotherPositionOffset + offsetSize * (m_AnotherCount - 1);
            }

            for (int j = lineEndIndex; j >= lineStartIndex; j--)
            {
                var item = InstantiateItem(--startIndex);
                item.SetAsFirstSibling();
                onRefreshItem?.Invoke(item, startIndex);
                var pivot = item.pivot;
                if (hl)
                {
                    value = itemEndPosition - LoopScrollHelper.GetAnchoredRightOffset(m_CellSize.x, pivot.x);
                    anotherValue = -lineStartValue - LoopScrollHelper.GetAnchoredTopOffset(m_CellSize.y, pivot.y);
                    lineStartValue -= m_CellSize.y + spacing.y;
                }
                else
                {
                    value = itemEndPosition - LoopScrollHelper.GetAnchoredBottomOffset(m_CellSize.y, pivot.y);
                    anotherValue = lineStartValue - LoopScrollHelper.GetAnchoredLeftOffset(m_CellSize.x, pivot.y);
                    lineStartValue -= m_CellSize.x + spacing.x;
                }

                item.anchoredPosition = p;
            }

            if (hl)
            {
                startPosition = itemEndPosition - m_CellSize.x;
            }
            else
            {
                startPosition = itemEndPosition + m_CellSize.y;
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