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
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/accessibility.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  for (const id of ['b', 'a']) window.forma.receive({ type: 'create', id, control: 'button', parentId: 'root', properties: {} });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  window.formaDesigner.receive({ action: 'state', id: 'root', title: 'Form1', status: 'Ready', selectedId: 'root', selectedIds: ['root'], propertySchema: [],
    controls: [{ id: 'root', kind: 'form', enabled: true, visible: true }, { id: 'b', kind: 'button', parentId: 'root', enabled: true, visible: true, tabIndex: 2 }, { id: 'a', kind: 'button', parentId: 'root', enabled: true, visible: true, tabIndex: 1 }] });
  messages.length = 0;
  const key = (target, key, init = {}) => { const event = new window.KeyboardEvent('keydown', { key, bubbles: true, cancelable: true, ...init }); target.dispatchEvent(event); return event; };
  return { window, document: window.document, messages, key };
}

test('top menus expose menu roles and move with the arrow keys', () => {
  const { document, key } = fixture();
  const [file, edit] = document.querySelectorAll('.menubar details');
  const summary = file.querySelector('summary');
  assert.equal(summary.getAttribute('aria-haspopup'), 'menu');
  assert.equal(file.querySelector('.menu').getAttribute('role'), 'menu');
  assert.equal(file.querySelector('.menu button').getAttribute('role'), 'menuitem');
  key(summary, 'ArrowDown');
  assert.equal(file.open, true);
  const items = [...file.querySelectorAll('.menu button:not(:disabled)')];
  assert.equal(document.activeElement, items[0]);
  key(items[0], 'ArrowDown');
  assert.equal(document.activeElement, items[1]);
  key(items[1], 'End');
  assert.equal(document.activeElement, items.at(-1));
  key(document.activeElement, 'ArrowRight');
  assert.equal(edit.open, true);
  assert.equal(file.open, false);
  assert.equal(document.querySelector('[data-view-option="grid"]').getAttribute('role'), 'menuitemcheckbox');
});

test('the design surface takes focus and Tab walks controls in tab order, then lets focus leave', () => {
  const { document, key, messages } = fixture();
  const root = document.getElementById('root');
  assert.equal(root.tabIndex, 0);
  root.focus();
  assert.equal(key(root, 'Tab').defaultPrevented, true);
  assert.deepEqual(plain([messages.at(-1).event, messages.at(-1).id]), ['select', 'a']);
  key(root, 'Tab');
  assert.equal(messages.at(-1).id, 'b');
  assert.equal(key(root, 'Tab').defaultPrevented, false, 'after the last control Tab moves on');
  assert.equal(key(root, 'Tab', { shiftKey: true }).defaultPrevented, true);
  assert.equal(messages.at(-1).id, 'a');
});

test('left sidebar panels are tab panels labelled by their tabs and the theme button is not a toggle', () => {
  const { document } = fixture();
  assert.equal(document.getElementById('toolbox').getAttribute('role'), 'tabpanel');
  assert.equal(document.getElementById('toolbox').getAttribute('aria-labelledby'), 'left-tab-toolbox');
  assert.equal(document.getElementById('solution-explorer').getAttribute('aria-labelledby'), 'left-tab-explorer');
  assert.equal(document.getElementById('theme-toggle').hasAttribute('aria-pressed'), false);
  assert.equal(document.getElementById('selection-status').getAttribute('aria-live'), 'polite');
});

test('runtime tab controls use roving focus, aria-controls and arrow keys', () => {
  const { window, document, messages, key } = fixture();
  window.formaDesigner.preview = true;
  window.forma.receive({ type: 'create', id: 'tabs', control: 'tabcontrol', parentId: 'root', properties: { tabs: ['One', 'Two', 'Three'], selectedTab: 0 } });
  const tabs = [...document.getElementById('tabs').querySelectorAll('[role="tab"]')];
  assert.deepEqual(tabs.map(tab => tab.tabIndex), [0, -1, -1]);
  assert.ok(tabs[0].getAttribute('aria-controls'));
  tabs[0].focus();
  key(tabs[0], 'ArrowRight');
  assert.deepEqual(plain(messages.at(-1).payload), { selectedTab: 1, expanded: true });
  key(document.activeElement, 'End');
  assert.equal(messages.at(-1).payload.selectedTab, 2);
});
