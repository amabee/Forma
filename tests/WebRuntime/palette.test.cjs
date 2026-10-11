const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');
const plain = value => JSON.parse(JSON.stringify(value));

function fixture() {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only' });
  const { window } = dom; const messages = [];
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/palette.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.forma.receive({ type: 'create', id: 'ok', control: 'button', parentId: 'root', properties: {} });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  window.formaDesigner.receive({ action: 'state', id: 'root', title: 'Form1', status: 'Ready', selectedId: 'root', selectedIds: ['root'], propertySchema: [],
    forms: [{ id: 'root', name: 'form1', title: 'Form1' }, { id: 'other', name: 'form2', title: 'Settings' }],
    controls: [{ id: 'root', name: 'form1', kind: 'form', enabled: true, visible: true }, { id: 'ok', name: 'okButton', kind: 'button', parentId: 'root', enabled: true, visible: true }] });
  messages.length = 0;
  const key = (key, init = {}) => window.document.body.dispatchEvent(new window.KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...init }));
  const type = text => { const input = window.document.querySelector('.palette-input'); input.value = text; input.dispatchEvent(new window.Event('input', { bubbles: true })); return input; };
  return { window, document: window.document, messages, key, type };
}

test('Ctrl+K opens the palette; typing filters commands, controls and forms', () => {
  const { document, key, type, messages } = fixture();
  key('k', { ctrlKey: true });
  assert.ok(document.querySelector('.palette-input'));
  type('save as');
  assert.match(document.querySelector('.palette-item[aria-selected="true"] .palette-label').textContent, /Save as/);
  type('okbutton');
  const input = type('go okbutton');
  input.dispatchEvent(new document.defaultView.KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
  assert.equal(document.querySelector('.palette'), null, 'running an item closes the palette');
  assert.deepEqual(plain([messages.at(-1).event, messages.at(-1).id]), ['select', 'ok']);
  key('k', { ctrlKey: true });
  type('switch settings');
  document.querySelector('.palette-item').dispatchEvent(new document.defaultView.MouseEvent('pointerdown', { bubbles: true }));
  assert.deepEqual(plain([messages.at(-1).event, messages.at(-1).payload]), ['select-form', { id: 'other' }]);
});

test('palette can add controls from the toolbox and arrows move the highlight', () => {
  const { document, key, type, messages } = fixture();
  key('k', { ctrlKey: true });
  const input = type('add slider');
  assert.equal(document.querySelector('.palette-item[aria-selected="true"] .palette-label').textContent, 'Add Slider');
  type('add');
  input.dispatchEvent(new document.defaultView.KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
  assert.equal(document.querySelectorAll('.palette-item')[1].getAttribute('aria-selected'), 'true');
  type('add slider');
  input.dispatchEvent(new document.defaultView.KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
  assert.deepEqual(plain([messages.at(-1).event, messages.at(-1).payload.control]), ['drop', 'slider']);
  key('k', { ctrlKey: true });
  type('zzzz');
  assert.equal(document.querySelector('.palette-empty').textContent, 'No matches');
  key('Escape');
  assert.equal(document.querySelector('.palette'), null);
});

test('F1 shows the keyboard shortcuts sheet', () => {
  const { document, key } = fixture();
  key('F1');
  const sheet = document.querySelector('.palette.shortcuts');
  assert.ok(sheet);
  assert.match(sheet.textContent, /Ctrl\+D.*Duplicate/s);
  sheet.querySelector('.palette-close').click();
  assert.equal(document.querySelector('.palette'), null);
});
