// @ts-nocheck
// Regression gate: convert → MonoGame render → pixel-diff vs Chromium, per fixture.
// Thresholds are max % differing pixels (Chromium AA drifts across browser builds;
// exact historical counts like 905px are not re-litigated here).
//
// Usage: npx tsx regress.ts
// Exit 0 = all gates pass; 1 = failure.
import { spawnSync } from 'node:child_process';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'node:fs';
import { nodeTsxArgs } from './tsx-run.js';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const hostDir = join(repoRoot, 'host');
const convertTs = join(__dirname, 'convert.ts');
const regressDir = join(repoRoot, '.regress');
mkdirSync(regressDir, { recursive: true });
const regressDir = join(repoRoot, '.regress');
mkdirSync(regressDir, { recursive: true });

// maxPct: soft ceiling. layoutOk means "glyph AA only is fine; layout must match."
// For fixtures where residual is known-large (Tabler font/CDN, Cerberus placeholders),
// the ceiling is the documented residual band, not zero.
const FIXTURES = [
  { tag: 'inventory', html: 'input/inventory.html', sel: '#panel', screen: 'InventoryScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'statusbar', html: 'input/statusbar.html', sel: '#bar', screen: 'StatusbarScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'grid', html: 'input/grid-uniform.html', sel: '#grid', screen: 'GridScreen', w: 800, h: 600, maxPct: 0.5 },
  { tag: 'asymmetric', html: 'input/asymmetric-border.html', sel: '#box', screen: 'AsymBorderScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'raster', html: 'input/raster-effects.html', sel: 'body', screen: 'RasterScreen', w: 800, h: 400, maxPct: 5 },
  { tag: 'padding', html: 'input/padding-flex.html', sel: '#panel', screen: 'PaddingScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'align', html: 'input/align-items-center.html', sel: '#bar', screen: 'AlignScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'zindex', html: 'input/z-index-order.html', sel: '#stage', screen: 'ZIndexScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'justify', html: 'input/justify-between.html', sel: '#bar', screen: 'JustifyScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'nineslice', html: 'input/nineslice-panel.html', sel: '#panel', screen: 'NineSliceScreen', w: 800, h: 600, maxPct: 5 },
  { tag: 'cssom', html: 'input/cssom-percent.html', sel: 'body', screen: 'CssomScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'borderimage', html: 'input/border-image.html', sel: '#panel', screen: 'BorderImageScreen', w: 800, h: 600, maxPct: 5 },
  // bg url + border-image → chrome rasterized with kids hidden (RPGUI panel pattern).
  { tag: 'borderbg', html: 'input/border-image-with-bg.html', sel: '#panel', screen: 'BorderBgScreen', w: 800, h: 400, maxPct: 5, noResponsive: true },
  { tag: 'brtext', html: 'input/br-text.html', sel: '#panel', screen: 'BrTextScreen', w: 800, h: 400, maxPct: 5.5, noResponsive: true },
  { tag: 'fixed', html: 'input/fixed-hud.html', sel: 'body', screen: 'FixedHudScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'gridspan', html: 'input/grid-span.html', sel: '#grid', screen: 'GridSpanScreen', w: 800, h: 600, maxPct: 1.5 },
  { tag: 'gamehud', html: 'input/game-hud.html', sel: 'body', screen: 'GameHudScreen', w: 800, h: 600, maxPct: 5 },
  // Real RPGUI composite — border-image chrome is rasterized; residual is mostly font AA.
  { tag: 'rpgui', html: 'input/rpgui-hud.html', sel: '#hud', screen: 'RpguiHudScreen', w: 800, h: 600, maxPct: 5, noResponsive: true },
  { tag: 'textxform', html: 'input/text-transform.html', sel: '#panel', screen: 'TextTransformScreen', w: 800, h: 400, maxPct: 12, noResponsive: true },
  { tag: 'textoutline', html: 'input/text-outline.html', sel: '#panel', screen: 'TextOutlineScreen', w: 800, h: 400, maxPct: 8, noResponsive: true },
  { tag: 'tabler', html: 'input/tabler-card.html', sel: '.card', screen: 'TablerScreen', w: 800, h: 600, maxPct: 26 },
  { tag: 'cerberus', html: 'input/cerberus-fluid.html', sel: '.email-container', screen: 'CerberusScreen', w: 700, h: 1000, maxPct: 45 },
  { tag: 'imagecard', html: 'input/tabler-image-card.html', sel: '.row', screen: 'TablerImageScreen', w: 800, h: 400, maxPct: 58 },
];

function run(cmd, args, cwd) {
  console.log(`\n> ${cmd} ${args.join(' ')}`);
  const r = spawnSync(cmd, args, { cwd, encoding: 'utf8', shell: false });
  if (r.stdout) process.stdout.write(r.stdout);
  if (r.stderr) process.stderr.write(r.stderr);
  if (r.status !== 0) throw new Error(`${cmd} exited ${r.status}`);
  return r;
}

function pixelDiff(gumPath, chromPath, crop) {
  const script = `
from PIL import Image, ImageChops
import json, sys
gum = Image.open(r'''${gumPath.replace(/\\/g, '/')}''').convert('RGB')
chrom = Image.open(r'''${chromPath.replace(/\\/g, '/')}''').convert('RGB')
x, y = ${crop.x}, ${crop.y}
w, h = chrom.size
gum_c = gum.crop((x, y, x+w, y+h))
if gum_c.size != chrom.size:
    print(json.dumps({"error": f"size {gum_c.size} vs {chrom.size}"}))
    sys.exit(0)
d = ImageChops.difference(gum_c, chrom)
diff = sum(1 for p in d.getdata() if p != (0,0,0))
total = w*h
print(json.dumps({"diff": diff, "total": total, "pct": 100.0*diff/total}))
`;
  const r = spawnSync('python', ['-c', script], { encoding: 'utf8' });
  if (r.status !== 0) throw new Error(`pixelDiff failed: ${r.stderr}`);
  return JSON.parse(r.stdout.trim());
}

const results = [];
let failed = 0;

console.log('=== single-viewport fixtures ===');
for (const f of FIXTURES) {
  console.log(`\n========== ${f.tag} ==========`);
  const convertArgs = [
    convertTs, f.html, f.sel, f.screen, String(f.w), String(f.h), `--tag=${f.tag}`,
  ];
  if (f.noResponsive) convertArgs.push('--no-responsive');
  run(process.execPath, nodeTsxArgs(...convertArgs), __dirname);

  const chromSaved = join(regressDir, `chromium-${f.tag}.png`);
  if (!existsSync(chromSaved)) throw new Error(`missing ${chromSaved}`);

  const tree = JSON.parse(readFileSync(join(hostDir, 'Content', 'generated', 'boxtree.json'), 'utf8'));
  const crop = {
    x: Math.max(0, Math.floor(tree.rect.x)),
    y: Math.max(0, Math.floor(tree.rect.y)),
  };

  const gumOut = join(regressDir, `gum-${f.tag}.png`);
  run('dotnet', [
    'run', '-c', 'Release', '--project', join(hostDir, 'HtmlToGumHost.csproj'), '--',
    gumOut, 'generated/Generated.gumx', f.screen, String(f.w), String(f.h),
  ], hostDir);

  const diff = pixelDiff(gumOut, chromSaved, crop);
  if (diff.error) {
    console.log(`FAIL ${f.tag}: ${diff.error}`);
    failed++;
    results.push({ ...f, ...diff, status: 'fail' });
    continue;
  }
  const ok = diff.pct <= f.maxPct;
  console.log(`${f.tag}: ${diff.diff} px / ${diff.total} (${diff.pct.toFixed(2)}%)  max ${f.maxPct}%  [${ok ? 'PASS' : 'FAIL'}]`);
  if (!ok) failed++;
  results.push({ ...f, ...diff, status: ok ? 'pass' : 'fail' });
}

console.log('\n=== underlay + opacity ===');
{
  run(process.execPath, nodeTsxArgs(
    convertTs,
    'input/underlay-opacity.html', '#panel', 'UnderlayScreen', '800', '400', '--tag=underlay',
  ), __dirname);
  const gusx = readFileSync(join(hostDir, 'Content', 'generated', 'Screens', 'UnderlayScreen.gusx'), 'utf8');
  const hasUnderlay = /PhBg[\s\S]*FillRed/.test(gusx) && /Instance Name="PhBg" BaseType="Rectangle"/.test(gusx);
  const hasFadeAlpha = /Fade\.FillAlpha[\s\S]*?<Value[^>]*>127<\/Value>/.test(gusx)
    || /Fade\.FillAlpha[\s\S]*?<Value[^>]*>128<\/Value>/.test(gusx);
  const ok = hasUnderlay && hasFadeAlpha;
  console.log(`img background-color underlay Rectangle: ${hasUnderlay ? 'yes' : 'NO'}`);
  console.log(`opacity 0.5 → FillAlpha ~128: ${hasFadeAlpha ? 'yes' : 'NO'}`);
  console.log(`underlay-opacity: [${ok ? 'PASS' : 'FAIL'}]`);
  if (!ok) failed++;
  results.push({ tag: 'underlay-opacity', status: ok ? 'pass' : 'fail', hasUnderlay, hasFadeAlpha });
}

console.log('\n=== responsive sidebar (width inference) ===');
{
  run(process.execPath, nodeTsxArgs(
    convertTs,
    'input/responsive-sidebar.html', '#layout', 'ResponsiveScreen', '1200', '400',
    '--responsive=400,1200', '--tag=responsive-sidebar',
  ), __dirname);

  const gusx = readFileSync(join(hostDir, 'Content', 'generated', 'Screens', 'ResponsiveScreen.gusx'), 'utf8');
  const hasPct = /Sidebar\.WidthUnits[\s\S]*?<Value xsi:type="xsd:int">1<\/Value>/.test(gusx);
  const hasPctVal = /Sidebar\.Width[\s\S]*?<Value xsi:type="xsd:float">25<\/Value>/.test(gusx);
  const hasFill = /Layout\.WidthUnits[\s\S]*?<Value xsi:type="xsd:int">2<\/Value>/.test(gusx);
  const ok = hasPct && hasPctVal && hasFill;
  console.log(`Sidebar PercentageOfParent 25: ${hasPct && hasPctVal ? 'yes' : 'NO'}`);
  console.log(`Layout RelativeToParent fill: ${hasFill ? 'yes' : 'NO'}`);
  console.log(`responsive-sidebar units: [${ok ? 'PASS' : 'FAIL'}]`);
  if (!ok) failed++;
  results.push({ tag: 'responsive-sidebar', status: ok ? 'pass' : 'fail', hasPct, hasPctVal, hasFill });

  // Render at an untrained mid-width and check sidebar ≈ 25%.
  const midW = 800;
  const gumMid = join(regressDir, `gum-responsive-${midW}.png`);
  run('dotnet', [
    'run', '-c', 'Release', '--project', join(hostDir, 'HtmlToGumHost.csproj'), '--',
    gumMid, 'generated/Generated.gumx', 'ResponsiveScreen', String(midW), '400',
  ], hostDir);

  const measure = `
from PIL import Image
im = Image.open(r'''${gumMid.replace(/\\/g, '/')}''').convert('RGB')
SIDEBAR = (60, 90, 138)
def near(a,b,tol=10): return all(abs(a[i]-b[i])<=tol for i in range(3))
sw = 0
for x in range(im.size[0]):
    if near(im.getpixel((x,10)), SIDEBAR): sw += 1
    elif sw: break
    else: break
print(sw)
`;
  const mr = spawnSync('python', ['-c', measure], { encoding: 'utf8' });
  const sidebarPx = parseInt(mr.stdout.trim(), 10);
  const expect = midW / 4;
  const midOk = Math.abs(sidebarPx - expect) <= 2;
  console.log(`sidebar @${midW}: ${sidebarPx}px (want ${expect}) [${midOk ? 'PASS' : 'FAIL'}]`);
  if (!midOk) failed++;
  results.push({ tag: 'responsive-sidebar-mid', sidebarPx, expect, status: midOk ? 'pass' : 'fail' });
}

console.log('\n=== responsive default (no explicit train pair) ===');
{
  run(process.execPath, nodeTsxArgs(
    convertTs,
    'input/responsive-sidebar.html', '#layout', 'ResponsiveDefault', '800', '400',
    '--tag=responsive-default',
  ), __dirname);
  const gusxDef = readFileSync(join(hostDir, 'Content', 'generated', 'Screens', 'ResponsiveDefault.gusx'), 'utf8');
  const defPct = /Sidebar\.WidthUnits[\s\S]*?<Value xsi:type="xsd:int">1<\/Value>/.test(gusxDef)
    && /Sidebar\.Width[\s\S]*?<Value xsi:type="xsd:float">25<\/Value>/.test(gusxDef);
  console.log(`default-responsive @800 Sidebar 25%: ${defPct ? 'yes' : 'NO'} [${defPct ? 'PASS' : 'FAIL'}]`);
  if (!defPct) failed++;
  results.push({ tag: 'responsive-default', status: defPct ? 'pass' : 'fail' });
}

console.log('\n=== responsive breakpoint (structure mismatch) ===');
{
  const r = spawnSync(process.execPath, nodeTsxArgs(
    convertTs,
    'input/responsive-breakpoint.html', '#layout', 'BreakpointScreen', '1200', '400',
    '--responsive=400,1200', '--tag=responsive-breakpoint',
  ), { cwd: __dirname, encoding: 'utf8', shell: false });
  process.stdout.write(r.stdout || '');
  process.stderr.write(r.stderr || '');
  if (r.status !== 0) {
    console.log('FAIL breakpoint convert exited non-zero');
    failed++;
  } else {
    const out = r.stdout || '';
    const warned = /structure mismatches/.test(out);
    console.log(`structure-mismatch warning emitted: ${warned ? 'yes' : 'NO'} [${warned ? 'PASS' : 'FAIL'}]`);
    if (!warned) failed++;
    results.push({ tag: 'responsive-breakpoint', status: warned ? 'pass' : 'fail' });
  }
}

writeFileSync(join(regressDir, 'regress-meta.json'), JSON.stringify(results, null, 2));
console.log(`\n=== ${failed === 0 ? 'ALL PASS' : failed + ' FAILED'} ===`);
process.exit(failed === 0 ? 0 : 1);
