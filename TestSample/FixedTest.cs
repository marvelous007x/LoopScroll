using UnityEngine;

public class FixedTest : MonoBehaviour
{
    LoopScrollView loop;
    void Start()
    {
        loop = GetComponent<LoopScrollView>();
    }

    public void ScrollTo(int index)
    {
        loop.ScrollToCell(index, 1000, () => Debug.Log("done"));
    }
}