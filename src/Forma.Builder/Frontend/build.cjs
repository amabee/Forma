const fs = require('node:fs');
const path = require('node:path');
const destination = path.join(__dirname, '../DesignerWeb/icons');
const source = path.join(__dirname, 'node_modules/lucide-static');
const names = ['file-plus', 'folder-open', 'save', 'undo-2', 'redo-2', 'play', 'monitor', 'search',
  'chevron-down', 'chevron-up', 'chevron-right', 'house', 'x', 'panel-top', 'group', 'columns-2',
  'panels-top-left', 'list', 'grid-2x2', 'rectangle-horizontal', 'text-cursor-input', 'type',
  'square-check', 'circle', 'chevrons-up-down', 'list-ordered', 'image', 'timer', 'settings', 'table', 'sliders-horizontal', 'gauge', 'toggle-left', 'toggle-right', 'calendar', 'clock', 'calendar-clock', 'palette', 'lock', 'link', 'list-checks'];
fs.mkdirSync(destination, { recursive: true });
for (const name of names) fs.copyFileSync(path.join(source, 'icons', `${name}.svg`), path.join(destination, `${name}.svg`));
fs.copyFileSync(path.join(source, 'LICENSE'), path.join(destination, 'LICENSE-LUCIDE.txt'));
fs.writeFileSync(path.join(destination, 'README.md'), '# Lucide icons\n\nDownloaded from the pinned lucide-static npm package. Source: https://lucide.dev/\nLicense: LICENSE-LUCIDE.txt. Rebuild with npm run build from ../Frontend.\n');
