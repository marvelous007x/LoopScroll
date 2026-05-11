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

    protected void RepositionContent(in Vector2 position)
    {
        Vector2 offset = Vector2.zero;
        if (horizontal)
        {
            if (Math.Abs(position.x) <= view.rect.width)
            {
                content.anchoredPosition = position;
                return;
            }
            offset.x = position.x;
        }
        else
        {
            if (Math.Abs(position.y) <= view.rect.height)
            {
                content.anchoredPosition = position;
                return;
            }
            offset.y = position.y;
        }
        content.anchoredPosition = position - offset;
        foreach (var item in content)
        {
            (item as RectTransform).anchoredPosition += offset;
        }
        m_ContentStartPosition -= offset;
        m_VirtualContentOffset += offset;
    }

    protected abstract void ReleaseForwards(in Vector2 position);
    protected abstract void InstantiateForwards(bool jump = false);
    protected abstract void ReleaseBackwards(in Vector2 position);
    protected abstract void InstantiateBackwards(bool jump = false);
    protected abstract void SetNormalizedPosition(float value);

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