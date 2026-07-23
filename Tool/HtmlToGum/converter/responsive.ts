// @ts-nocheck
// A single Chromium snapshot can't tell "width:300px" from "width:25%" — both resolve
// to the same pixel number at whatever viewport was rendered. This compares the SAME
// DOM (same fixture, same rootSelector) rendered at two different viewport widths and
// infers, per node per axis, whether the resolved value stayed constant (Absolute) or
// scaled proportionally with its parent (PercentageOfParent). Requires the two trees to
// have identical structure (child count/order) at both widths — a real, named
// limitation: a page that shows/hides elements via display:none media queries between
// the two chosen widths breaks the positional path-key matching this relies on.

import type { BoxNode, ResponsiveMap, ViewportSize } from './types.js';

const ABS_TOLERANCE_PX = 2;     // constant-across-viewports tolerance
const FRACTION_TOLERANCE = 0.02; // 2% — proportional-to-parent tolerance

function inferAxis(vNarrow, vWide, parentNarrow, parentWide, viewportNarrow, viewportWide) {
  if (Math.abs(vNarrow - vWide) <= ABS_TOLERANCE_PX) {
    return { units: 'Absolute', value: Math.round(vWide) };
  }
  if (parentNarrow > 0 && parentWide > 0) {
    const fracNarrow = vNarrow / parentNarrow;
    const fracWide = vWide / parentWide;
    if (Math.abs(fracNarrow - fracWide) <= FRACTION_TOLERANCE) {
      return { units: 'PercentageOfParent', value: Math.round(fracWide * 10000) / 100 };
    }
  }
  // Root has no DOM parent — but if it tracked the training viewport (CSS width:100% /
  // 100vw), Gum's canvas is the viewport analogue. RelativeToParent 0 fills the canvas;
  // a stable fraction of the viewport becomes PercentageOfParent of the canvas.
  if (!(parentNarrow > 0) && viewportNarrow > 0 && viewportWide > 0) {
    if (Math.abs(vNarrow - viewportNarrow) <= ABS_TOLERANCE_PX
        && Math.abs(vWide - viewportWide) <= ABS_TOLERANCE_PX) {
      return { units: 'RelativeToParent', value: 0 };
    }
    const fracNarrow = vNarrow / viewportNarrow;
    const fracWide = vWide / viewportWide;
    if (Math.abs(fracNarrow - fracWide) <= FRACTION_TOLERANCE) {
      return { units: 'PercentageOfParent', value: Math.round(fracWide * 10000) / 100 };
    }
  }
  // Neither constant nor cleanly proportional — most likely a discrete CSS breakpoint
  // (a Bootstrap-style column class jumping from 50% to 25% at a media query) rather
  // than a continuous function of viewport width. Fall back to Absolute at the wider
  // viewport's measured value and flag it so the caller can warn instead of silently
  // guessing; a real limitation of comparing only 2 sample points, not a bug.
  return { units: 'Absolute', value: Math.round(vWide), ambiguous: true };
}

/**
 * @param {{width:number,height:number}|null} viewportNarrow Training viewport used for treeNarrow
 * @param {{width:number,height:number}|null} viewportWide Training viewport used for treeWide
 * @returns {Map<string, {width: object, height: object}>} path-key ("0.1.0" = root's
 *   2nd child's 1st child) -> inferred {units, value, ambiguous?} per axis.
 */
export function computeResponsiveMap(
  treeNarrow: BoxNode,
  treeWide: BoxNode,
  viewportNarrow: ViewportSize | null = null,
  viewportWide: ViewportSize | null = null,
): { map: ResponsiveMap; mismatches: string[] } {
  const map = new Map();
  const mismatches = [];

  function walk(nodeNarrow, nodeWide, parentNarrow, parentWide, path) {
    const key = path.join('.');
    map.set(key, {
      width: inferAxis(nodeNarrow.rect.width, nodeWide.rect.width, parentNarrow?.rect.width, parentWide?.rect.width, viewportNarrow?.width, viewportWide?.width),
      height: inferAxis(nodeNarrow.rect.height, nodeWide.rect.height, parentNarrow?.rect.height, parentWide?.rect.height, viewportNarrow?.height, viewportWide?.height),
    });

    const cn = nodeNarrow.children, cw = nodeWide.children;
    if (cn.length !== cw.length) {
      mismatches.push(`${key || '(root)'}: ${cn.length} children narrow vs ${cw.length} wide — structure differs between viewports, inference for this subtree is unreliable.`);
      return;
    }
    for (let i = 0; i < cn.length; i++) walk(cn[i], cw[i], nodeNarrow, nodeWide, [...path, i]);
  }

  walk(treeNarrow, treeWide, null, null, []);
  return { map, mismatches };
}
