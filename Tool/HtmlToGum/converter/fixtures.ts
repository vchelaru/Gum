/** Fixture paths are relative to samples/ (see samples-path.ts). */
export type Fixture = {
  tag: string;
  html: string;
  sel: string;
  screen: string;
  w: number;
  h: number;
  maxPct: number;
  noResponsive?: boolean;
};

// maxPct: soft ceiling. layoutOk means glyph AA only is fine; layout must match.
// For fixtures where residual is known-large (Tabler font/CDN, Cerberus placeholders),
// the ceiling is the documented residual band, not zero.
export const FIXTURES: Fixture[] = [
  { tag: 'inventory', html: 'features/inventory.html', sel: '#panel', screen: 'InventoryScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'statusbar', html: 'features/statusbar.html', sel: '#bar', screen: 'StatusbarScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'grid', html: 'features/grid-uniform.html', sel: '#grid', screen: 'GridScreen', w: 800, h: 600, maxPct: 0.5 },
  { tag: 'asymmetric', html: 'features/asymmetric-border.html', sel: '#box', screen: 'AsymBorderScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'raster', html: 'features/raster-effects.html', sel: 'body', screen: 'RasterScreen', w: 800, h: 400, maxPct: 5 },
  { tag: 'padding', html: 'features/padding-flex.html', sel: '#panel', screen: 'PaddingScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'align', html: 'features/align-items-center.html', sel: '#bar', screen: 'AlignScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'zindex', html: 'features/z-index-order.html', sel: '#stage', screen: 'ZIndexScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'justify', html: 'features/justify-between.html', sel: '#bar', screen: 'JustifyScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'nineslice', html: 'features/nineslice-panel.html', sel: '#panel', screen: 'NineSliceScreen', w: 800, h: 600, maxPct: 5 },
  { tag: 'cssom', html: 'features/cssom-percent.html', sel: 'body', screen: 'CssomScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'borderimage', html: 'features/border-image.html', sel: '#panel', screen: 'BorderImageScreen', w: 800, h: 600, maxPct: 5 },
  // bg url + border-image -> chrome rasterized with kids hidden (RPGUI panel pattern).
  { tag: 'borderbg', html: 'features/border-image-with-bg.html', sel: '#panel', screen: 'BorderBgScreen', w: 800, h: 400, maxPct: 5, noResponsive: true },
  { tag: 'brtext', html: 'features/br-text.html', sel: '#panel', screen: 'BrTextScreen', w: 800, h: 400, maxPct: 5.5, noResponsive: true },
  { tag: 'fixed', html: 'composites/fixed-hud.html', sel: 'body', screen: 'FixedHudScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'gridspan', html: 'features/grid-span.html', sel: '#grid', screen: 'GridSpanScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'gamehud', html: 'composites/game-hud.html', sel: 'body', screen: 'GameHudScreen', w: 800, h: 600, maxPct: 5 },
  // Real RPGUI composite - border-image chrome is rasterized; residual is mostly font AA.
  { tag: 'rpgui', html: 'composites/rpgui-hud.html', sel: '#hud', screen: 'RpguiHudScreen', w: 800, h: 600, maxPct: 5, noResponsive: true },
  { tag: 'textxform', html: 'features/text-transform.html', sel: '#panel', screen: 'TextTransformScreen', w: 800, h: 400, maxPct: 12, noResponsive: true },
  { tag: 'textoutline', html: 'features/text-outline.html', sel: '#panel', screen: 'TextOutlineScreen', w: 800, h: 400, maxPct: 8, noResponsive: true },
  { tag: 'tabler', html: 'third-party/tabler-card.html', sel: '.card', screen: 'TablerScreen', w: 800, h: 600, maxPct: 26 },
  { tag: 'cerberus', html: 'third-party/cerberus-fluid.html', sel: '.email-container', screen: 'CerberusScreen', w: 700, h: 1000, maxPct: 45 },
  { tag: 'imagecard', html: 'third-party/tabler-image-card.html', sel: '.row', screen: 'TablerImageScreen', w: 800, h: 400, maxPct: 58 },
];

export const EXTRA_CHECKS = {
  underlay: { html: 'features/underlay-opacity.html', sel: '#panel', screen: 'UnderlayScreen', w: 800, h: 400, tag: 'underlay' },
  responsive: { html: 'features/responsive-sidebar.html', sel: '#layout', screen: 'ResponsiveScreen', w: 1200, h: 400, tag: 'responsive-sidebar' },
  responsiveDefault: { html: 'features/responsive-sidebar.html', sel: '#layout', screen: 'ResponsiveDefault', w: 800, h: 400, tag: 'responsive-default' },
  breakpoint: { html: 'features/responsive-breakpoint.html', sel: '#layout', screen: 'BreakpointScreen', w: 1200, h: 400, tag: 'responsive-breakpoint' },
};

