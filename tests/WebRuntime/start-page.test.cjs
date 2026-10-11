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
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/start-page.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  const show = () => window.forma.receive({ type: 'designer', action: 'start-page',
    templates: [{ id: 'login', name: 'Login form', description: 'Fields and a button' }],
    recent: [{ path: 'C:/Designs/App.forma', name: 'App', folder: 'C:/Designs', exists: true }, { path: 'C:/Gone.forma', name: 'Gone', folder: 'C:/', exists: false }] });
  return { window, document: window.document, messages, show };
}

test('the start page lists templates and recent projects and sends the chosen action', () => {
  const { document, messages, show } = fixture();
  show();
  const page = document.querySelector('.start-page');
  assert.ok(page);
  assert.equal(page.getAttribute('role'), 'dialog');
  const recent = [...document.querySelectorAll('.start-recent-item')];
  assert.deepEqual(recent.map(item => item.querySelector('strong').textContent), ['App', 'Gone']);
  assert.equal(recent[1].disabled, true, 'missing files cannot be opened');
  recent[0].click();
  assert.deepEqual(plain(messages.at(-1).payload), { command: 'open-recent', path: 'C:/Designs/App.forma' });
  assert.equal(document.querySelector('.start-page'), null, 'choosing closes the page');
  show();
  [...document.querySelectorAll('.start-action')].find(button => button.textContent.includes('Login form')).click();
  assert.deepEqual(plain(messages.at(-1).payload), { command: 'new-from-template', template: 'login' });
});

test('Escape, Blank form and using the toolbox dismiss the start page', () => {
  const { window, document, show, messages } = fixture();
  show();
  document.body.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
  assert.equal(document.querySelector('.start-page'), null);
  show();
  const count = messages.length;
  document.querySelector('.start-action').click();
  assert.equal(document.querySelector('.start-page'), null);
  assert.equal(messages.length, count, 'Blank form just closes the page');
  show();
  document.querySelector('[data-kind="button"]').dispatchEvent(new window.MouseEvent('dblclick', { bubbles: true }));
  assert.equal(document.querySelector('.start-page'), null);
});
