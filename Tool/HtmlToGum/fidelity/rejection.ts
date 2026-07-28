// @ts-nocheck
// Soft / hard rejection signals for the bookmark fidelity crawler.
// When a seed (or first page) is blocked, abort the site — do not burn
// max-pages × networkidle timeouts on the same wall.

/** @typedef {{ status?: number|null, title?: string|null, html?: string|null, url?: string|null }} RejectionProbe */

const TITLE_RE = /just a moment|attention required|access denied|access blocked|request blocked|forbidden|robot check|are you a robot|verify you are human|security check|captcha|cloudflare|unusual traffic|please enable cookies|enable javascript and cookies/i;

// Structural challenge chrome only — bare "hcaptcha" matches MediaWiki config JSON on
// normal Wikipedia pages (wgConfirmEditHCaptchaSiteKey) and false-rejects crawl.
const BODY_RE = /cf-browser-verification|challenge-platform|cdn-cgi\/challenge|hcaptcha\.com\/|class=["'][^"']*g-recaptcha|cf-turnstile|__cf_chl_|bot detection|automated queries|please verify you are a human|checking your browser before|ddos protection by|request unsuccessful|pardon our interruption/i;

/**
 * @param {RejectionProbe} probe
 * @returns {{ rejected: boolean, reason?: string }}
 */
export function detectPageRejection(probe = {}) {
  const status = probe.status ?? null;
  if (typeof status === 'number' && status >= 400) {
    return { rejected: true, reason: `http ${status}` };
  }

  const title = (probe.title || '').trim();
  if (title && TITLE_RE.test(title)) {
    // Login titles alone are weak on real sites ("Sign in · Google Accounts" on a
    // bookmark that is literally a login URL is still low-value for fidelity).
    return { rejected: true, reason: `title: ${title.slice(0, 80)}` };
  }

  const html = probe.html || '';
  if (html && BODY_RE.test(html)) {
    const m = html.match(BODY_RE);
    return { rejected: true, reason: `body: ${(m?.[0] || 'challenge').slice(0, 60)}` };
  }

  return { rejected: false };
}

/**
 * Playwright page probe used after a successful navigation.
 * @param {import('playwright-core').Page} page
 * @param {number|null} [status]
 */
export async function probePageRejection(page, status = null) {
  const snap = await page.evaluate(() => ({
    title: document.title || '',
    html: (document.documentElement?.outerHTML || '').slice(0, 80_000),
    url: location.href,
  }));
  return detectPageRejection({
    status,
    title: snap.title,
    html: snap.html,
    url: snap.url,
  });
}
