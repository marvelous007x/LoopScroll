using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridTest : MonoBehaviour
{
    LoopScroll loop;
    void Start()
    {
        loop = GetComponent<LoopScroll>();
        loop.onRefreshItem = (item, index) =>
        {
            item.Find("Text").GetComponent<Text>().text = index.ToString();
        };
    }

    public void ScrollTo(int index)
    {
        loop.ScrollToCell(index, 1000, () => Debug.Log("done"));
    }
}
