const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');
// Values from the page's realm are copied so deepEqual compares structure only.
const plain = value => JSON.parse(JSON.stringify(value));

const controls = [
  { id: 'root', name: 'form1', kind: 'form', enabled: true, visible: true, x: 0, y: 0, width: 640, height: 440 },
  { id: 'a', name: 'a', kind: 'button', parentId: 'root', enabled: true, visible: true, x: 10, y: 10, width: 100, height: 30, dock: 'none' },
  { id: 'b', name: 'b', kind: 'button', parentId: 'root', enabled: true, visible: true, x: 200, y: 50, width: 60, height: 30, dock: 'none' },
  { id: 'c', name: 'c', kind: 'button', parentId: 'root', enabled: true, visible: true, x: 400, y: 90, width: 80, height: 30, dock: 'none' },
];

function fixture() {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only' });
  const { window } = dom; const messages = [];
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/selection.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  for (const id of ['a', 'b', 'c']) window.forma.receive({ type: 'create', id, control: 'button', parentId: 'root', properties: { text: id } });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  const state = (selectedIds, selectedId = selectedIds.at(-1), extra = {}) => window.formaDesigner.receive({
    action: 'state', id: 'root', title: 'Form1', status: 'Ready', selectedId, selectedIds, controls, propertySchema: [], ...extra,
  });
  const pointer = (type, target, init = {}) => {
    const event = new window.MouseEvent(type, { bubbles: true, cancelable: true, button: 0, ...init });
    Object.defineProperty(event, 'pointerId', { value: 1 });
    target.dispatchEvent(event);
  };
  const key = (key, init = {}, target = window.document.body) => target.dispatchEvent(new window.KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...init }));
  state(['root']);
  messages.length = 0;
  return { window, document: window.document, messages, state, pointer, key, last: () => messages.at(-1) };
}

test('Ctrl-click adds and removes controls and reports every selected ID', () => {
  const { document, pointer, last } = fixture();
  pointer('pointerdown', document.getElementById('a'), { ctrlKey: true });
  assert.deepEqual(plain(last().payload.ids), ['a']);
  pointer('pointerdown', document.getElementById('b'), { shiftKey: true });
  assert.equal(last().event, 'select');
  assert.equal(last().id, 'b');
  assert.deepEqual(plain(last().payload.ids), ['a', 'b']);
  pointer('pointerdown', document.getElementById('a'), { ctrlKey: true });
  assert.deepEqual(plain(last().payload.ids), ['b']);
});

test('multi-selection is outlined, enables align tools and updates the status', () => {
  const { document, state } = fixture();
  state(['a', 'b']);
  assert.ok(document.getElementById('a').classList.contains('forma-multi-selected'));
  assert.ok(document.getElementById('b').classList.contains('forma-multi-selected'));
  assert.equal(document.querySelector('.align-tools').hidden, false);
  assert.equal(document.querySelector('[data-align="left"]').disabled, false);
  assert.equal(document.querySelector('[data-align="distribute-horizontal"]').disabled, true, 'distribute needs three controls');
  assert.equal(document.getElementById('selection-status').textContent, '2 controls selected');
  state(['a']);
  assert.equal(document.getElementById('b').classList.contains('forma-multi-selected'), false);
  assert.equal(document.querySelector('.align-tools').hidden, true);
});

test('align and distribute send one move-many edit', () => {
  const { document, state, last } = fixture();
  state(['a', 'b', 'c'], 'a');
  document.querySelector('[data-align="left"]').click();
  assert.equal(last().event, 'move-many');
  assert.deepEqual(plain(last().payload.moves.map(move => move.x)), [10, 10, 10]);
  document.querySelector('[data-align="bottom"]').click();
  assert.deepEqual(plain(last().payload.moves.map(move => move.y + controls.find(c => c.id === move.id).height)), [120, 120, 120]);
  document.querySelector('[data-align="distribute-horizontal"]').click();
  // Span 10..480 holds 240px of buttons, leaving two equal 115px gaps.
  assert.deepEqual(plain(last().payload.moves.map(move => move.x)), [10, 225, 400]);
});

test('dragging a member of the selection moves the whole group', () => {
  const { document, state, pointer, last } = fixture();
  state(['a', 'b']);
  pointer('pointerdown', document.getElementById('b'), { clientX: 100, clientY: 100 });
  pointer('pointermove', document.getElementById('b'), { clientX: 120, clientY: 105 });
  assert.match(document.getElementById('a').style.transform, /translate\(20px, 5px\)/);
  pointer('pointerup', document.getElementById('b'), { clientX: 120, clientY: 105 });
  assert.equal(last().event, 'move-many');
  assert.deepEqual(plain(last().payload.moves), [{ id: 'a', x: 30, y: 15 }, { id: 'b', x: 220, y: 55 }]);
  assert.equal(document.getElementById('a').style.transform, '');
});

test('keyboard: Delete, arrows and clipboard shortcuts act on the selection but not inside fields', () => {
  const { document, state, key, last, messages } = fixture();
  state(['a', 'c']);
  key('Delete');
  assert.deepEqual(plain([last().event, last().payload.command, last().payload.ids]), ['command', 'delete', ['a', 'c']]);
  key('ArrowRight', { shiftKey: true });
  assert.deepEqual(plain(last().payload.moves), [{ id: 'a', x: 20, y: 10 }, { id: 'c', x: 410, y: 90 }]);
  key('d', { ctrlKey: true });
  assert.equal(last().payload.command, 'duplicate');
  key('v', { ctrlKey: true });
  assert.equal(last().payload.command, 'paste');
  const count = messages.length;
  const field = document.getElementById('search');
  key('c', { ctrlKey: true }, field);
  assert.equal(messages.length, count, 'native copy inside inputs is untouched');
  key('a', { ctrlKey: true });
  assert.deepEqual(plain(last().payload.ids), ['a', 'b', 'c']);
});

test('paste is only enabled when the host reports clipboard content', () => {
  const { document, state } = fixture();
  state(['a']);
  assert.equal(document.querySelector('[data-selection-command="paste"]').disabled, true);
  state(['a'], 'a', { canPaste: true });
  assert.equal(document.querySelector('[data-selection-command="paste"]').disabled, false);
});

test('drag-selecting on the form background picks the controls the rectangle touches', () => {
  const { document, pointer, last } = fixture();
  const box = (left, top, width, height) => () => ({ left, top, right: left + width, bottom: top + height, width, height, x: left, y: top });
  const root = document.getElementById('root');
  root.getBoundingClientRect = box(0, 0, 640, 440);
  for (const control of controls.slice(1)) document.getElementById(control.id).getBoundingClientRect = box(control.x, control.y, control.width, control.height);
  pointer('pointerdown', root, { clientX: 5, clientY: 5 });
  pointer('pointermove', root, { clientX: 250, clientY: 70 });
  assert.ok(document.getElementById('selection-marquee'));
  pointer('pointerup', root, { clientX: 250, clientY: 70 });
  assert.equal(document.getElementById('selection-marquee'), null);
  assert.equal(last().event, 'select');
  assert.deepEqual(plain(last().payload.ids), ['a', 'b']);
});

test('the inspector shows shared properties, marks differing values Mixed and edits every control', () => {
  const { document, window, last } = fixture();
  const schema = [
    { id: 'text', label: 'Text', category: 'General', editor: 'text' },
    { id: 'width', label: 'Width', category: 'Layout', editor: 'number' },
    { id: 'enabled', label: 'Enabled', category: 'Behavior', editor: 'checkbox' },
    { id: 'iconName', label: 'Icon', category: 'Content', editor: 'text' },
  ];
  const items = controls.map(control => ({ ...control, text: control.id === 'c' ? 'Other' : 'Same', enabled: control.id !== 'b' }));
  window.formaDesigner.receive({ action: 'state', id: 'root', title: 'Form1', status: 'Ready', selectedId: 'b', selectedIds: ['a', 'b'],
    sharedProperties: ['text', 'width', 'enabled'], controls: items, propertySchema: schema });
  const text = document.getElementById('prop-text'), width = document.getElementById('prop-width'), enabled = document.getElementById('prop-enabled');
  assert.equal(text.value, 'Same');
  assert.equal(text.dataset.mixed, undefined);
  assert.equal(width.dataset.mixed, 'true', 'widths 100 and 60 differ');
  assert.equal(width.placeholder, 'Mixed');
  assert.equal(enabled.indeterminate, true);
  assert.ok(document.getElementById('prop-iconName').closest('label').classList.contains('multi-hidden'));
  width.value = '120';
  width.dispatchEvent(new window.Event('change', { bubbles: true }));
  assert.deepEqual(plain([last().event, last().payload.property, last().payload.value, last().payload.ids]), ['property', 'width', 120, ['a', 'b']]);
});
