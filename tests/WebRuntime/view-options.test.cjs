const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');

function fixture() {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only', url: 'http://localhost/' });
  const { window } = dom;
  window.chrome = { webview: { postMessage() {}, addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/view-options.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  for (const [id, control] of [['second', 'button'], ['first', 'textbox'], ['caption', 'label'], ['hidden', 'button']])
    window.forma.receive({ type: 'create', id, control, parentId: 'root', properties: {} });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  const item = (id, kind, extra = {}) => ({ id, name: id, kind, parentId: 'root', enabled: true, visible: true, ...extra });
  window.formaDesigner.receive({ action: 'state', id: 'root', title: 'Form1', status: 'Ready', selectedId: 'root', selectedIds: ['root'], propertySchema: [],
    controls: [{ id: 'root', kind: 'form', enabled: true, visible: true }, item('second', 'button', { tabIndex: 2 }), item('first', 'textbox', { tabIndex: 1 }), item('caption', 'label'), item('hidden', 'button', { visible: false })] });
  const toggle = name => window.document.querySelector(`[data-view-option="${name}"]`).click();
  return { window, document: window.document, toggle };
}

test('Show tab order numbers focusable controls by tab index, skipping labels and hidden controls', () => {
  const { document, toggle } = fixture();
  assert.equal(document.querySelector('.tab-order-layer'), null);
  toggle('tabOrder');
  assert.equal(document.querySelector('[data-view-option="tabOrder"]').getAttribute('aria-pressed'), 'true');
  assert.deepEqual([...document.querySelectorAll('.tab-order-badge')].map(badge => badge.textContent), ['1', '2']);
  toggle('tabOrder');
  assert.equal(document.querySelector('.tab-order-layer'), null);
});

test('Show grid draws a grid and snaps positions on axes without an alignment guide', () => {
  const { window, document, toggle } = fixture();
  assert.deepEqual(JSON.parse(JSON.stringify(window.formaView.snap(13, 21, []))), { x: 13, y: 21 });
  toggle('grid');
  assert.ok(document.getElementById('root').classList.contains('show-grid'));
  assert.deepEqual(JSON.parse(JSON.stringify(window.formaView.snap(13, 21, []))), { x: 16, y: 24 });
  assert.deepEqual(JSON.parse(JSON.stringify(window.formaView.snap(13, 21, [{ axis: 'x' }]))), { x: 13, y: 24 });
  assert.equal(window.localStorage.getItem('forma.view.grid'), 'true');
});
