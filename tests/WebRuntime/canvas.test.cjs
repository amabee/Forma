const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');
const plain = value => JSON.parse(JSON.stringify(value));

const controls = [
  { id: 'root', name: 'form1', kind: 'form', enabled: true, visible: true, x: 0, y: 0, width: 640, height: 440 },
  { id: 'panel', name: 'panel1', kind: 'panel', parentId: 'root', enabled: true, visible: true, x: 10, y: 10, width: 300, height: 200, dock: 'none' },
  { id: 'button', name: 'button1', kind: 'button', parentId: 'panel', enabled: true, visible: true, x: 5, y: 5, width: 100, height: 30, dock: 'none' },
];

function fixture() {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only' });
  const { window } = dom; const messages = [];
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/selection.js', 'src/Forma.Builder/DesignerWeb/canvas.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.forma.receive({ type: 'create', id: 'panel', control: 'panel', parentId: 'root', properties: {} });
  window.forma.receive({ type: 'create', id: 'button', control: 'button', parentId: 'panel', properties: { text: 'Go' } });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  const state = selectedId => window.formaDesigner.receive({ action: 'state', id: 'root', title: 'Form1', status: 'Ready', selectedId, selectedIds: [selectedId], controls, propertySchema: [] });
  const key = (key, init = {}) => window.document.body.dispatchEvent(new window.KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...init }));
  state('button');
  messages.length = 0;
  return { window, document: window.document, messages, key, state, last: () => messages.at(-1) };
}

test('keyboard zoom steps through levels and Ctrl+0 resets', () => {
  const { window, key } = fixture();
  key('=', { ctrlKey: true });
  assert.equal(window.formaDesigner.zoom, 1.1);
  key('-', { ctrlKey: true }); key('-', { ctrlKey: true });
  assert.equal(window.formaDesigner.zoom, 0.9);
  key('0', { ctrlKey: true });
  assert.equal(window.formaDesigner.zoom, 1);
});

test('Ctrl+wheel zooms the canvas and plain wheel scrolls normally', () => {
  const { window, document } = fixture();
  const stage = document.getElementById('stage');
  const wheel = init => { const event = new window.WheelEvent('wheel', { bubbles: true, cancelable: true, ...init }); stage.dispatchEvent(event); return event; };
  assert.equal(wheel({ deltaY: -100 }).defaultPrevented, false);
  assert.equal(window.formaDesigner.zoom, 1);
  assert.equal(wheel({ deltaY: -100, ctrlKey: true }).defaultPrevented, true);
  assert.equal(window.formaDesigner.zoom, 1.1);
});

test('Fit picks a level that shows the whole form in the stage', () => {
  const { window, document } = fixture();
  const stage = document.getElementById('stage');
  Object.defineProperty(stage, 'clientWidth', { value: 400 });
  Object.defineProperty(stage, 'clientHeight', { value: 600 });
  const zoom = document.getElementById('zoom');
  zoom.value = 'fit';
  zoom.dispatchEvent(new window.Event('change', { bubbles: true }));
  // (400 - 48) / (640 + 2) is the limiting ratio.
  assert.equal(window.formaDesigner.zoom, 0.55);
  assert.equal(zoom.selectedOptions[0].textContent, '55%');
});

test('the selection path lists parents and selects one when clicked', () => {
  const { document, last } = fixture();
  const segments = [...document.querySelectorAll('#selection-path button')];
  assert.deepEqual(segments.map(button => button.textContent), ['form1', 'panel1', 'button1']);
  assert.equal(segments[2].getAttribute('aria-current'), 'true');
  segments[1].click();
  assert.deepEqual(plain([last().event, last().id]), ['select', 'panel']);
});

test('Alt+Shift+Arrow resizes the selected control, Ctrl makes bigger steps', () => {
  const { key, last } = fixture();
  key('ArrowRight', { altKey: true, shiftKey: true });
  assert.deepEqual(plain([last().event, last().payload.width, last().payload.height]), ['resize', 101, 30]);
  key('ArrowUp', { altKey: true, shiftKey: true, ctrlKey: true });
  assert.deepEqual(plain([last().payload.width, last().payload.height]), [100, 20]);
});

test('form size presets resize the form in one edit and name the current size', () => {
  const { document, last } = fixture();
  assert.equal(document.getElementById('device-caption').textContent, 'Small window (640 × 440)');
  document.getElementById('device-button').click();
  const items = [...document.querySelectorAll('#device-menu button')];
  assert.equal(document.getElementById('device-menu').hidden, false);
  assert.equal(items.find(item => item.textContent.startsWith('Small window')).getAttribute('aria-checked'), 'true');
  items.find(item => item.textContent.startsWith('Laptop')).click();
  assert.deepEqual(plain([last().event, last().id, last().payload]), ['resize', 'root', { width: 1024, height: 640 }]);
  assert.equal(document.getElementById('device-menu').hidden, true);
});
