using UnityEngine;

public class UnfixedTest : MonoBehaviour
{
    LoopScroll loop;
    void Start()
    {
        loop = GetComponent<LoopScroll>();
        loop.onRefreshItem = RefreshItem;
    }

    private void RefreshItem(RectTransform trans, int index)
    {
        var size = Random.Range(20, 400);
        // var size = FixedRandom(index);
        var axis = loop.horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical;
        trans.SetSizeWithCurrentAnchors(axis, size);
    }

    public void ScrollTo(int index)
    {
        loop.ScrollToCell(index, 1000, () => Debug.Log("done"));
    }

    private static int FixedRandom(int input)
    {
        const uint a = 1664525;
        const uint c = 1013904223;

        uint x = (uint)input;
        x = a * x + c;

        return 50 + (int)(x % 351); // 351 = 400 - 50 + 1
    }
}
