import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
/** Tool/HtmlToGum root (parent of converter/) */
export const htmlToGumRoot = resolve(__dirname, '..');
export const samplesRoot = join(htmlToGumRoot, 'samples');

/** Absolute path to a file under samples/, e.g. samplePath('features/inventory.html') */
export function samplePath(...parts: string[]) {
  return join(samplesRoot, ...parts);
}

