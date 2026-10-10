import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const site = fileURLToPath(new URL('../', import.meta.url));
const dist = path.join(site, 'dist');
const base = ('/' + (process.env.DOCS_BASE || '').replace(/^\/+|\/+$/g, '')).replace(/\/$/, '');
const navigation = JSON.parse(fs.readFileSync(path.join(site, 'src/generated/navigation.json'), 'utf8'));
const errors = [];
function walk(folder) { return fs.readdirSync(folder, { withFileTypes: true }).flatMap(entry => entry.isDirectory() ? walk(path.join(folder, entry.name)) : [path.join(folder, entry.name)]); }
const htmlFiles = walk(dist).filter(file => file.endsWith('.html'));
for (const file of htmlFiles) {
  const html = fs.readFileSync(file, 'utf8');
  const relative = path.relative(dist, file).replaceAll('\\', '/');
  const pageUrl = new URL(`${base}/${relative.replace(/index\.html$/, '')}`, 'https://docs.local');
  for (const match of html.matchAll(/(?:href|src)="([^"]+)"/g)) {
    const target = match[1].replaceAll('&amp;', '&');
    if (/^(https?:|mailto:|data:|javascript:)/.test(target)) continue;
    const resolved = new URL(target, pageUrl);
    if (base && !resolved.pathname.startsWith(base + '/')) { errors.push(`${relative}: link escapes deployment base: ${target}`); continue; }
    const pathname = decodeURIComponent(resolved.pathname.slice(base.length));
    let destination = path.join(dist, pathname);
    if (fs.existsSync(destination) && fs.statSync(destination).isDirectory()) destination = path.join(destination, 'index.html');
    if (!fs.existsSync(destination)) { errors.push(`${relative}: missing ${target}`); continue; }
    if (resolved.hash && destination.endsWith('.html')) {
      const id = decodeURIComponent(resolved.hash.slice(1));
      const destinationHtml = destination === file ? html : fs.readFileSync(destination, 'utf8');
      if (!destinationHtml.includes(`id="${id}"`)) errors.push(`${relative}: missing anchor ${target}`);
    }
  }
  if (!html.includes('<title>') || !html.includes('name="description"')) errors.push(`${relative}: missing page metadata`);
}
const componentPages = navigation.filter(page => page.kind);
for (const page of componentPages) {
  const file = path.join(dist, page.slug, 'index.html');
  if (!fs.existsSync(file)) errors.push(`Missing component page ${page.title}`);
  else {
    const html = fs.readFileSync(file, 'utf8');
    for (const section of ['Runtime property API', 'Component properties', 'JavaScript example', 'C# model reference']) if (!html.includes(section)) errors.push(`${page.title}: missing ${section}`);
  }
}
if (errors.length) { console.error(errors.join('\n')); process.exitCode = 1; }
else console.log(`Verified ${htmlFiles.length} pages: internal links, anchors, assets, metadata, and ${componentPages.length} complete component references.`);
