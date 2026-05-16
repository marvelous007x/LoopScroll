# LoopScrollView

## Overview

This project implements an recyclable list by referencing ScrollRect source code and LoopScrollRect, with the goal of completely abandoning LayoutGroup components.

I have implemented for Horizontal/Vertical loop with one element each row/column.Grid is on the way.Probably will not implement for both direction since rare use cases.

If element size is fixed, use `LoopScrollFixed`, you should set its size field, since it will ignore item's real size. Otherwise use `LoopScrollFlex`.

Set `totalCount` negative for infinite items.

## Some Notes

- Now it's simple and may lack lots of functions, but should be useable.

- Padding is not added, as I think it's not reall neccessary in my experience. Only spacing is offered.

- `ScrollSensitivity` in horizontal is negative of `ScrollRect.ScrollSensitivity` which applys to my habbit.

- It can only fill elements from left or top now.