# LoopScrollView

## Overview

This project implements an recyclable list by referencing ScrollRect source code and LoopScrollRect, with the goal of completely abandoning LayoutGroup components.

I have implemented for Horizontal/Vertical loop with one element each row/column.Grid is on the way.Probably will not implement for both direction since rare use cases.

If element size is fixed, use `LoopScrollFixed`, you should set its size field, since it will ignore item's real size. Otherwise use `LoopScrollFlex`.

## Use

- Fill elements via calling `RefillCells(int offset = 0)` or `RefillCellsBackwards(int index)`.

- Listen to `onRefreshItem` to draw your elements.

- Listen to `onReleaseItem` if you wish to get callback when element was released to cache pool.

- Set `totalCount` negative for infinite items.

- Set element size in `onRefreshItem` for dynamic case.

## Dynamic Case Notes

- Scrollbar may be a little jittering, since I need to update predicted total size for all elements and scrollbar value every time when element changes. 

- If you want element for auto size, functionally like `ContentSizeFitter` 、 `LayoutElment`, please implement your own script to immediately resize RectTransform size in `onRefreshItem` invokes. Maybe like `LayoutRebuilder.ForceRebuildLayoutImmediate`.

## Other Notes

- Now it's simple and may lack lots of functions, but should be useable.

- Padding is not added, as I think it's not realy neccessary in my experience. Only spacing is offered.

- `ScrollSensitivity` in horizontal is negative of `ScrollRect.ScrollSensitivity` which applys to my habbit.

- For auto expand view scrollbar, I just expand view size by its pivot. So set view anchor pivot properly if you use auto hide and expand viewport scrollbar.

- `enableDragInParent` lets you to pass drag events to parents.For example, if you have loop horizontals in loop vertical, you can drag vertically for the vertical loop without affecting horizontals, and drag horizontally for a horizontal loop without affecting the vertical loop. Whether it's a horizontal or vertical drag is judged in `OnBeginDrag` by checking whick direction of dragged offset is larger.