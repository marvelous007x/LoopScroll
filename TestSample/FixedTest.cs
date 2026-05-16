using UnityEngine;

public class FixedTest : MonoBehaviour
{
    LoopScroll loop;
    void Start()
    {
        loop = GetComponent<LoopScroll>();
    }

    public void ScrollTo(int index)
    {
        loop.ScrollToCell(index, 1000, () => Debug.Log("done"));
    }
}