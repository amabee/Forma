const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const base = path.join(__dirname, '../..');
const read = file => fs.readFileSync(path.join(base, file), 'utf8');

// forma.css is generated from Frontend/runtime.css + controls.css; designer.css shares controls.css.
test('the runtime stylesheet carries the shared control styles and its own defaults', () => {
  const runtime = read('src/Forma.WebView2/Web/css/forma.css');
  for (const rule of ['[data-forma-type="datagridview"]', '[data-forma-type="tabcontrol"]', '.forma-dialog-popup', '.command-popup', '[data-forma-type="chip"]'])
    assert.ok(runtime.includes(rule), `shared rule ${rule}`);
  for (const token of ['--forma-accent', '--forma-surface', '--forma-border'])
    assert.ok(runtime.includes(token), `theme token ${token}`);
});

test('designer tooling stays out of the runtime stylesheet', () => {
  const runtime = read('src/Forma.WebView2/Web/css/forma.css');
  for (const rule of ['forma-multi-selected', 'selection-outline', 'snap-guide', 'drop-target', 'show-grid', 'start-page', 'palette', 'cursor: move', 'preview-mode'])
    assert.equal(runtime.includes(rule), false, `designer-only ${rule}`);
});

test('the designer and the runtime build from the same control styles', () => {
  assert.match(read('src/Forma.Builder/Frontend/input.css'), /@import "\.\/controls\.css";/);
  assert.match(read('src/Forma.Builder/Frontend/runtime.css'), /@import "\.\/controls\.css";/);
  assert.match(read('src/Forma.Builder/Frontend/package.json'), /-i runtime\.css -o \.\.\/\.\.\/Forma\.WebView2\/Web\/css\/forma\.css/);
});
