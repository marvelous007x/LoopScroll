# LoopScrollView

## Overview

This project implements an recyclable list by referencing ScrollRect source code and LoopScrollRect, with the goal of completely abandoning LayoutGroup components.

I have implemented for Horizontal/Vertical loop with one element each row/column.Grid is on the way.Probably will not implement for both direction since rare use cases.

If element size is fixed, use `LoopScrollFixed`, you should set its size field, since it will ignore item's real size. Otherwise use `LoopScrollFlex`.

## Use

- Listen to `onRefreshItem` to draw your elements.

- Listen to `onReleaseItem` if you wish to get callback when element was released to cache pool.

- Set `totalCount` negative for infinite items.

- Set element size in `onRefreshItem` for dynamic case.

## Dynamic Case Notes

- Scrollbar may be a little jittering, since I need to update predicted total size for all elements and scrollbar value every time when element changes. 

- If you want element for auto size, functionally like `ContentSizeFitter` 、 `LayoutElment`, please implement your own script to immediately resize RectTransform size in `onRefreshItem` invokes. Maybe like `LayoutRebuilder.ForceRebuildLayoutImmediate`.

## Other Notes

- Now it's simple and may lack lots of functions, but should be useable.

- Padding is not added, as I think it's not reall neccessary in my experience. Only spacing is offered.

- `ScrollSensitivity` in horizontal is negative of `ScrollRect.ScrollSensitivity` which applys to my habbit.

- It can only fill elements from left or top now.If you want to fill from right or bottom,`RefillCells(totalCount - 1)` and reverse indexes will do the trick