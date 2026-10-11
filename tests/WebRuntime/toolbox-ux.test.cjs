const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');

function fixture({ storage = {} } = {}) {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only', url: 'http://localhost/' });
  const { window } = dom; const messages = [];
  for (const [key, value] of Object.entries(storage)) window.localStorage.setItem(key, value);
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/toolbox.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  const search = window.document.getElementById('search');
  const type = text => { search.value = text; search.dispatchEvent(new window.Event('input', { bubbles: true })); };
  const key = name => search.dispatchEvent(new window.KeyboardEvent('keydown', { key: name, bubbles: true }));
  const group = name => [...window.document.querySelectorAll('.tool-group')].find(item => item.querySelector('summary').textContent.trim() === name);
  return { window, document: window.document, messages, type, key, group, search };
}

test('only Layout and Controls start open, and group choices are remembered', () => {
  const { window, group } = fixture();
  assert.equal(group('Layout').open, true);
  assert.equal(group('Controls').open, true);
  assert.equal(group('Modern UI').open, false);
  group('Modern UI').open = true;
  group('Modern UI').dispatchEvent(new window.Event('toggle'));
  const saved = JSON.parse(window.localStorage.getItem('forma.toolbox.groups'));
  assert.equal(saved['Modern UI'], true);
  const again = fixture({ storage: { 'forma.toolbox.groups': JSON.stringify(saved) } });
  assert.equal(again.group('Modern UI').open, true);
});

test('search opens matching groups, hides empty ones and restores the previous layout', () => {
  const { document, type, group } = fixture();
  type('slider');
  const visible = [...document.querySelectorAll('.tool-group [data-kind]')].filter(tool => !tool.hidden && !tool.closest('.search-empty'));
  assert.deepEqual(visible.map(tool => tool.dataset.kind), ['slider']);
  assert.ok(group('Layout').classList.contains('search-empty'));
  assert.equal(document.querySelector('.toolbox-empty').hidden, true);
  type('zzz');
  assert.equal(document.querySelector('.toolbox-empty').hidden, false);
  assert.match(document.querySelector('.toolbox-empty').textContent, /No controls match "zzz"/);
  type('');
  assert.equal(group('Layout').open, true);
  assert.equal(group('Modern UI').open, false);
  assert.equal(group('Layout').classList.contains('search-empty'), false);
});

test('Enter inserts the first match and Escape clears the search', () => {
  const { messages, type, key, search } = fixture();
  type('datagrid');
  key('Enter');
  const drop = messages.find(message => message.event === 'drop');
  assert.equal(drop.payload.control, 'datagridview');
  key('Escape');
  assert.equal(search.value, '');
});

test('used tools appear in a Recently used group that inserts like the original', async () => {
  const { window, document, messages } = fixture();
  const recent = document.querySelector('.recent-tools');
  assert.equal(recent.hidden, true);
  document.querySelector('.tool-group:not(.recent-tools) [data-kind="label"]').dispatchEvent(new window.MouseEvent('dblclick', { bubbles: true }));
  await new Promise(resolve => setTimeout(resolve));
  assert.equal(recent.hidden, false);
  const copy = recent.querySelector('[data-kind="label"]');
  assert.ok(copy);
  messages.length = 0;
  copy.dispatchEvent(new window.MouseEvent('dblclick', { bubbles: true }));
  assert.equal(messages.find(message => message.event === 'drop').payload.control, 'label');
});
