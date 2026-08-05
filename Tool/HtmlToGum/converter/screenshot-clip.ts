// @ts-nocheck
// Intersect a Playwright page.screenshot clip with the scrollable page bounds.
// Off-screen / zero-area clips throw "Clipped area is either empty or outside the
// resulting image" (kali.org tools gradients, opencv hero SVGs with transformed boxes).

/**
 * @param {{ x: number, y: number, width: number, height: number }} clip
 * @param {number} pageW scrollWidth
 * @param {number} pageH scrollHeight
 * @returns {{ x: number, y: number, width: number, height: number } | null}
 */
export function intersectScreenshotClip(clip, pageW, pageH) {
  if (!(pageW > 0) || !(pageH > 0)) return null;
  const x0 = Math.max(0, Math.floor(Number(clip.x) || 0));
  const y0 = Math.max(0, Math.floor(Number(clip.y) || 0));
  const rawW = Number(clip.width);
  const rawH = Number(clip.height);
  if (!Number.isFinite(rawW) || !Number.isFinite(rawH) || rawW <= 0 || rawH <= 0) return null;
  const x1 = Math.min(pageW, Math.ceil((Number(clip.x) || 0) + rawW));
  const y1 = Math.min(pageH, Math.ceil((Number(clip.y) || 0) + rawH));
  const width = x1 - x0;
  const height = y1 - y0;
  if (width < 1 || height < 1) return null;
  return { x: x0, y: y0, width, height };
}
