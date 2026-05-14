using UnityEngine;

internal static class LoopScrollViewHelpers
{
    internal static float GetAnchoredTop(this RectTransform rt)
    {
        var pos = rt.anchoredPosition.y;
        return pos + rt.rect.yMax;
    }

    internal static float GetAnchoredBottom(this RectTransform rt)
    {
        var pos = rt.anchoredPosition.y;
        return pos + rt.rect.yMin;
    }

    internal static float GetAnchoredLeft(this RectTransform rt)
    {
        var pos = rt.anchoredPosition.x;
        return pos + rt.rect.xMin;
    }

    internal static float GetAnchoredRight(this RectTransform rt)
    {
        var pos = rt.anchoredPosition.x;
        return pos + rt.rect.xMax;
    }

    internal static Vector2 GetAnchoredMax(this RectTransform rt)
    {
        var pos = rt.anchoredPosition;
        return pos + rt.rect.max;
    }

    internal static Vector2 GetAnchoredMin(this RectTransform rt)
    {
        var pos = rt.anchoredPosition;
        return pos + rt.rect.min;
    }

    internal static float GetAnchoredTopOffset(float height, float pivot)
    {
        return (1 - pivot) * height;
    }

    internal static float GetAnchoredBottomOffset(float height, float pivot)
    {
        return -pivot * height;
    }

    internal static float GetAnchoredLeftOffset(float width, float pivot)
    {
        return -pivot * width;
    }

    internal static float GetAnchoredRightOffset(float width, float pivot)
    {
        return (1 - pivot) * width;
    }
}