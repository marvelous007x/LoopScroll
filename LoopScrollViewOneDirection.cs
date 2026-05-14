using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.ScrollRect;

public abstract class LoopScrollViewOneDirection : LoopScrollView
{
    public RectTransform.Axis direction;

    protected override bool vertical => direction == RectTransform.Axis.Vertical;
    protected override bool horizontal => direction == RectTransform.Axis.Horizontal;

    [SerializeField]
    protected Scrollbar m_Scrollbar;

    public Scrollbar scrollbar
    {
        get
        {
            return m_Scrollbar;
        }
        set
        {
            if (m_Scrollbar)
                m_Scrollbar.onValueChanged.RemoveListener(SetNormalizedPosition);
            m_Scrollbar = value;
            if (m_Scrollbar)
                m_Scrollbar.onValueChanged.AddListener(SetNormalizedPosition);
        }
    }

    public override Scrollbar horizontalScrollbar
    {
        get
        {
            if (vertical) return null;
            return m_Scrollbar;
        }
        set
        {
            if (vertical) return;
            scrollbar = value;
        }
    }

    public override Scrollbar verticalScrollbar
    {
        get
        {
            if (horizontal) return null;
            return m_Scrollbar;
        }
        set
        {
            if (horizontal) return;
            scrollbar = value;
        }
    }

    [SerializeField]
    private ScrollbarVisibility m_ScrollbarVisibility;
    public float spacing;
    public ScrollbarVisibility scrollbarVisibility
    {
        get => m_ScrollbarVisibility;
        set => m_ScrollbarVisibility = value;
    }

    public override ScrollbarVisibility horizontalScrollbarVisibility
    {
        get
        {
            if (vertical) return ScrollbarVisibility.Permanent;
            return scrollbarVisibility;
        }
        set
        {
            if (vertical) return;
            scrollbarVisibility = value;
        }
    }

    public override ScrollbarVisibility verticalScrollbarVisibility
    {
        get
        {
            if (horizontal) return ScrollbarVisibility.Permanent;
            return scrollbarVisibility;
        }
        set
        {
            if (horizontal) return;
            scrollbarVisibility = value;
        }
    }

    protected abstract float normalizedValue
    {
        get;
    }

    public override Vector2 normalizedPosition
    {
        get
        {
            if (horizontal) return new Vector2(normalizedValue, 0);
            else return new Vector2(0, normalizedValue);
        }
        set
        {
            if (horizontal) SetNormalizedPosition(value.x);
            else SetNormalizedPosition(value.y);
        }
    }

    protected float startPosition, endPosition;
    protected float boundStart, boundEnd;
    protected bool everReachStart, everReachEnd;

    protected override void OnSetup()
    {
        startPosition = 0;
        everReachStart = everReachEnd = false;
        endPosition = horizontal ? -spacing : spacing;
    }

    protected override void Refill()
    {
        InstantiateForwards();
        UpdateContentBounds();
        UpdatePrevData();
    }

    protected void ReleaseForwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;
        bool hl = horizontal;
        var border = hl ? -content.anchoredPosition.x : -content.anchoredPosition.y;
        float itemEndPosition;
        for (int i = 0; i < count; i++)
        {
            var item = content.GetChild(0) as RectTransform;
            var size = GetItemSize(item, startIndex);
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
            onRefreshItem?.Invoke(endIndex, item);
            var pivot = item.pivot;
            var size = GetItemSize(item, endIndex);
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

    protected void ReleaseBackwards(in Vector2 position)
    {
        var count = content.childCount;
        if (count == 0) return;

        bool hl = horizontal;
        var border = hl ? view.rect.width - content.anchoredPosition.x : -view.rect.height - content.anchoredPosition.y;

        float itemStartPosition;
        for (int i = count - 1; i >= 0; i--)
        {
            var item = content.GetChild(i) as RectTransform;
            var size = GetItemSize(item, endIndex);
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
            onRefreshItem?.Invoke(startIndex, item);
            var size = GetItemSize(item, startIndex);
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
                OnInstantiateForwardsJump();
            InstantiateForwards();
        }
        else
        {
            ReleaseBackwards(position);
            RepositionContent(position);
            if (jump && content.childCount == 0)
                OnInstantiateBackwardsJump();
            InstantiateBackwards();
        }
        UpdateContentBounds();
    }

    protected void RepositionContent(in Vector2 position)
    {
        Vector2 offset = Vector2.zero;
        float add;
        if (horizontal)
        {
            if (Math.Abs(position.x) <= view.rect.width)
            {
                content.anchoredPosition = position;
                return;
            }
            offset.x = position.x;
            add = offset.x;
        }
        else
        {
            if (Math.Abs(position.y) <= view.rect.height)
            {
                content.anchoredPosition = position;
                return;
            }
            offset.y = position.y;
            add = offset.y;
        }
        content.anchoredPosition = position - offset;
        foreach (var item in content)
        {
            (item as RectTransform).anchoredPosition += offset;
        }
        m_ContentStartPosition -= offset;
        m_VirtualContentOffset += offset;

        startPosition += add;
        endPosition += add;
        boundStart += add;
        boundEnd += add;
    }

    protected abstract void OnInstantiateForwardsJump();
    protected abstract void OnInstantiateBackwardsJump();
    protected abstract void SetNormalizedPosition(float value);
    protected abstract float GetItemSize(RectTransform item, int index);
    protected override void OnEnable()
    {
        base.OnEnable();
        if (scrollbar)
            scrollbar.onValueChanged.AddListener(SetNormalizedPosition);
    }

    protected override void OnDisable()
    {
        if (scrollbar)
            scrollbar.onValueChanged.RemoveListener(SetNormalizedPosition);
        base.OnDisable();
    }
}