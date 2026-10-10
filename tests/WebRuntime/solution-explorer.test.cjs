const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../../src/Forma.Builder/DesignerWeb');
function fixture(t) {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'index.html'), 'utf8'), { url: 'https://forma.test', runScripts: 'outside-only', pretendToBeVisual: true });
  t.after(() => dom.window.close());
  const { window } = dom, messages = [], doc = window.document;
  window.forma = { send: value => messages.push(value) };
  window.formaDesigner = { receive() {} };
  window.formaWorkspace = { measure() {} };
  window.eval(fs.readFileSync(path.join(base, 'solution-explorer.js'), 'utf8'));
  const control = (id, name) => ({ key: id, name, kind: 'component', controlId: id, children: [
    { key: `script:${id}`, name: 'script.js', kind: 'javascript', controlId: id, command: 'view-script' },
    { key: `css:${id}`, name: 'component.css', kind: 'css', controlId: id, command: 'view-css' },
    { key: `properties:${id}`, name: 'custom-properties.json', kind: 'json', controlId: id, command: 'view-custom-properties' }
  ] });
  const root = { key: 'project:form1', name: 'MyProject', kind: 'project', children: [
    { key: 'forms', name: 'Forms', kind: 'folder', children: [
      { key: 'form1', name: 'Form1', kind: 'form', controlId: 'form1', children: [control('button1', 'startButton')] },
      { key: 'form2', name: 'Form2', kind: 'form', controlId: 'form2', children: [control('label1', 'statusLabel')] }
    ] },
    { key: 'global-script', name: 'main.js', kind: 'javascript', controlId: 'form1', command: 'edit-global-script' },
    { key: 'assets', name: 'Assets (0)', kind: 'folder', children: [] }
  ] };
  const receive = () => window.formaDesigner.receive({ action: 'state', id: 'form1', selectedId: 'button1', explorer: root });
  const row = key => [...doc.querySelectorAll('[data-explorer-key]')].find(row => row.dataset.explorerKey === key);
  receive(); return { window, doc, messages, root, receive, row };
}
test('explorer shares the sidebar with toolbox and opens forms and component sources by stable ID', t => {
  const { doc, messages, row, window } = fixture(t);
  assert.equal(doc.getElementById('solution-explorer').hidden, false);
  assert.equal(doc.getElementById('toolbox').hidden, true);
  doc.querySelector('[data-left-panel="toolbox"]').click();
  assert.equal(doc.getElementById('toolbox').hidden, false);
  assert.equal(doc.getElementById('solution-explorer').hidden, true);
  window.formaExplorer.show('solution-explorer');
  row('form2').click(); assert.equal(messages.at(-1).id, 'form2'); assert.equal(messages.at(-1).event, 'explorer-select');
  row('button1').querySelector('button').click();
  row('script:button1').click();
  assert.equal(messages.at(-1).payload.command, 'view-script'); assert.equal(messages.at(-1).id, 'button1');
  row('global-script').click(); assert.equal(messages.at(-1).payload.command, 'edit-global-script');
});
test('search reveals nested results and unchanged designer states preserve tree nodes and expansion', t => {
  const { doc, window, receive, row } = fixture(t);
  row('button1').querySelector('button').click();
  const button = row('button1'); receive(); assert.equal(row('button1'), button);
  assert.equal(button.getAttribute('aria-expanded'), 'true');
  const search = doc.getElementById('explorer-search'); search.value = 'statusLabel'; search.dispatchEvent(new window.Event('input'));
  assert.equal(row('button1'), undefined); assert.ok(row('label1'));
  assert.equal(row('form2').getAttribute('aria-expanded'), 'true');
  search.value = ''; search.dispatchEvent(new window.Event('input'));
  assert.equal(row('button1').getAttribute('aria-expanded'), 'true');
  search.value = 'missing'; search.dispatchEvent(new window.Event('input'));
  assert.equal(doc.getElementById('explorer-empty').hidden, false);
});
test('keyboard navigation expands rows, opens files, blocks locked sources and renders names safely', t => {
  const { window, root, receive, row, messages, doc } = fixture(t);
  root.children[0].children[0].children[0].name = '<img src=x onerror=alert(1)>';
  root.children[0].children[0].children[0].children[0].locked = true; receive();
  assert.equal(row('button1').querySelector('.explorer-label').textContent, '<img src=x onerror=alert(1)>');
  assert.equal(doc.querySelector('img[src="x"]'), null);
  row('button1').focus(); row('button1').dispatchEvent(new window.KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));
  assert.equal(row('button1').getAttribute('aria-expanded'), 'true');
  const count = messages.length; row('script:button1').click(); assert.equal(messages.length, count);
  row('css:button1').dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
  assert.equal(messages.at(-1).payload.command, 'view-css');
  row('button1').dispatchEvent(new window.KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
  assert.equal(doc.activeElement.dataset.explorerKey, 'script:button1');
});


test('context menus create real project items by key and keep component sources fixed', t => {
  const { window, row, messages, doc, root, receive } = fixture(t);
  row('project:form1').dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: 20, clientY: 30 }));
  const menu = doc.querySelector('.explorer-context-menu'); assert.equal(menu.hidden, false);
  assert.ok(menu.querySelector('[data-explorer-action="new-js"]'));
  assert.ok(menu.querySelector('[data-explorer-action="import-file"]'));
  menu.querySelector('[data-explorer-action="new-folder"]').click();
  assert.equal(messages.at(-1).event, 'explorer-action');
  assert.deepEqual(JSON.parse(JSON.stringify(messages.at(-1).payload)), { action: 'new-folder', key: 'project:form1' });
  root.children.push({ key: 'files', name: 'Files', kind: 'folder', children: [{ key: 'file:folder-id', name: 'Scripts', kind: 'project-folder', itemId: 'folder-id', children: [{ key: 'file:helper-id', name: 'helpers.js', kind: 'javascript', command: 'open-project-file', itemId: 'helper-id' }] }] });
  receive();
  row('file:folder-id').dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
  menu.querySelector('[data-explorer-action="new-css"]').click();
  assert.equal(messages.at(-1).payload.key, 'file:folder-id');
  row('file:helper-id').click(); assert.equal(messages.at(-1).payload.action, 'open-project-file');
  assert.equal(messages.at(-1).payload.key, 'file:helper-id');
  row('button1').dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
  assert.ok(menu.querySelector('[data-explorer-action="rename-control"]'));
  assert.equal(menu.querySelector('[data-explorer-action="new-folder"]'), null);
  menu.querySelector('[data-explorer-action="view-script"]').click();
  assert.equal(messages.at(-1).id, 'button1'); assert.equal(messages.at(-1).payload.command, 'view-script');
});

test('keyboard context menu restores focus and offers Remove Form only for multiple forms', t => {
  const { window, row, doc } = fixture(t);
  row('form1').dispatchEvent(new window.KeyboardEvent('keydown', { key: 'F10', shiftKey: true, bubbles: true, cancelable: true }));
  const menu = doc.querySelector('.explorer-context-menu');
  assert.equal(menu.hidden, false); assert.equal(menu.querySelector('[data-explorer-action="delete-form"]').disabled, false);
  menu.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
  assert.ok(menu.contains(doc.activeElement));
  menu.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true, cancelable: true }));
  assert.equal(menu.hidden, true); assert.equal(doc.activeElement.dataset.explorerKey, 'form1');
});


test('context menu stays inside the sidebar while the native editor covers the center', t => {
  const { window, row, doc } = fixture(t);
  const menu = doc.querySelector('.explorer-context-menu');
  Object.defineProperty(menu, 'offsetWidth', { value: 208 });
  doc.getElementById('solution-explorer').getBoundingClientRect = () => ({ right: 264 });
  doc.getElementById('editor-dock').hidden = false;
  row('project:form1').dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: 240, clientY: 30 }));
  assert.ok(Number.parseFloat(menu.style.left) + menu.offsetWidth <= 264);
});
