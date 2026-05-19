using UnityEngine;

public abstract class LoopScrollRowOrColumn : LoopScrollHorizontalOrVertical
{
    public float spacing;

    protected override void OnSetup(bool forwards)
    {
        base.OnSetup(forwards);
        if (forwards)
        {
            startPosition = 0;
            endPosition = horizontal ? -spacing : spacing;
        }
        else if (horizontal)
        {
            endPosition = view.rect.width;
            startPosition = endPosition + spacing;
        }
        else
        {
            endPosition = -view.rect.height;
            startPosition = endPosition - spacing;
        }
    }

    protected void ReleaseForwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;
        bool hl = horizontal;
        var border = hl ? -position.x : -position.y;
        float itemEndPosition;
        for (int i = 0; i < count; i++)
        {
            var item = content.GetChild(0) as RectTransform;
            var size = GetItemSize(item, startIndex);
            if (hl)
            {
                itemEndPosition = item.anchoredPosition.x + LoopScrollHelper.GetAnchoredRightOffset(size, item.pivot.x);
                if (itemEndPosition <= border)
                {
                    ReleaseItem(item, startIndex++);
                    startPosition = itemEndPosition + spacing;
                    continue;
                }
            }
            else
            {
                itemEndPosition = item.anchoredPosition.y + LoopScrollHelper.GetAnchoredBottomOffset(size, item.pivot.y);
                if (itemEndPosition >= border)
                {
                    ReleaseItem(item, startIndex++);
                    startPosition = itemEndPosition - spacing;
                    continue;
                }
            }
            break;
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
            var item = InstantiateItem(++endIndex); ;
            onRefreshItem?.Invoke(item, endIndex);
            var pivot = item.pivot;
            var size = GetItemSize(item, endIndex);
            if (hl)
            {
                value = itemStartPosition - LoopScrollHelper.GetAnchoredLeftOffset(size, pivot.x);
                endPosition = itemStartPosition + size;
            }
            else
            {
                value = itemStartPosition - LoopScrollHelper.GetAnchoredTopOffset(size, pivot.y);
                endPosition = itemStartPosition - size;
            }
            item.anchoredPosition = p;
            OnInstantiatedItem(item, endIndex, size);
            if (totalCount > 0)
            {
                if (endIndex == 0)
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

        float itemStartPosition;
        for (int i = count - 1; i >= 0; i--)
        {
            var item = content.GetChild(i) as RectTransform;
            var size = GetItemSize(item, endIndex);
            if (hl)
            {
                itemStartPosition = item.anchoredPosition.x + LoopScrollHelper.GetAnchoredLeftOffset(size, item.pivot.x);
                if (itemStartPosition >= border)
                {
                    ReleaseItem(item, endIndex--);
                    endPosition = itemStartPosition - spacing;
                    continue;
                }
            }
            else
            {
                itemStartPosition = item.anchoredPosition.y + LoopScrollHelper.GetAnchoredTopOffset(size, item.pivot.y);
                if (itemStartPosition <= border)
                {
                    ReleaseItem(item, endIndex--);
                    endPosition = itemStartPosition + spacing;
                    continue;
                }
            }
            break;
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
        float spacingOffset = hl ? -spacing : spacing;
        while (true)
        {
            if (totalCount > 0 && startIndex <= 0) break;
            var itemEndPosition = startPosition + spacingOffset;
            if (hl && itemEndPosition <= border) break;
            if (vertical && itemEndPosition >= border) break;
            var item = InstantiateItem(--startIndex);
            item.SetAsFirstSibling();
            onRefreshItem?.Invoke(item, startIndex);
            var size = GetItemSize(item, startIndex);
            if (hl)
            {
                value = itemEndPosition - LoopScrollHelper.GetAnchoredRightOffset(size, item.pivot.x);
                startPosition = itemEndPosition - size;
            }
            else
            {
                value = itemEndPosition - LoopScrollHelper.GetAnchoredBottomOffset(size, item.pivot.y);
                startPosition = itemEndPosition + size;
            }
            item.anchoredPosition = p;

            if (totalCount > 0)
            {
                if (startIndex == 0)
                {
                    boundStart = startPosition;
                }

                if (startIndex == totalCount - 1)
                {
                    boundEnd = itemEndPosition;
                }
            }
            OnInstantiatedItem(item, startIndex, size);
        }
    }

    protected abstract float GetItemSize(RectTransform item, int index);
    protected virtual void OnInstantiatedItem(RectTransform item, int index, float size) { }

}