using UnityEngine;

public class UnfixedTest : MonoBehaviour
{
    LoopScrollView loop;
    void Start()
    {
        loop = GetComponent<LoopScrollView>();
        loop.onRefreshItem = RefreshItem;
    }

    private void RefreshItem(RectTransform trans, int index)
    {
        var size = Random.Range(20, 400);
        var axis = loop.horizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical;
        trans.SetSizeWithCurrentAnchors(axis, size);
    }

    public void ScrollTo(int index)
    {
        loop.ScrollToCell(index, 1000, () => Debug.Log("done"));
    }
}
