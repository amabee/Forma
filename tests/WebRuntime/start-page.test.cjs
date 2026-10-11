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
    recent: [{ path: 'C:/Designs/App.forma', name: 'App', folder: 'C:/Designs', exists: true, edited: new Date(Date.now() - 2 * 3600e3).toISOString() }, { path: 'C:/Gone.forma', name: 'Gone', folder: 'C:/', exists: false }] });
  return { window, document: window.document, messages, show };
}

test('the start screen lists recent projects with edit times and sends the chosen action', () => {
  const { document, messages, show } = fixture();
  show();
  const page = document.querySelector('.start-page');
  assert.equal(page.getAttribute('role'), 'dialog');
  assert.equal(page.getAttribute('aria-labelledby'), 'start-title');
  const recent = [...document.querySelectorAll('.start-recent-item')];
  assert.deepEqual(recent.map(item => item.querySelector('strong').textContent), ['App', 'Gone']);
  assert.match(recent[0].textContent, /Edited 2 hours ago/);
  assert.equal(recent[1].disabled, true, 'missing files cannot be opened');
  assert.match(recent[1].textContent, /Not found/);
  recent[0].click();
  assert.deepEqual(plain(messages.at(-1).payload), { command: 'open-recent', path: 'C:/Designs/App.forma' });
  assert.equal(document.querySelector('.start-page'), null, 'choosing closes the screen');
  show();
  document.querySelector('.start-template').click();
  assert.deepEqual(plain(messages.at(-1).payload), { command: 'new-from-template', template: 'login' });
  show();
  document.querySelector('.start-recent-remove').click();
  assert.deepEqual(plain(messages.at(-1).payload), { command: 'remove-recent', path: 'C:/Designs/App.forma' });
  assert.ok(document.querySelector('.start-page'), 'removing keeps the screen open');
});

test('New project, Escape, the rail and the toolbox all leave the start screen sensibly', () => {
  const { window, document, show, messages } = fixture();
  show();
  assert.equal(document.activeElement.textContent, 'New project', 'the primary action has focus');
  document.querySelector('.start-primary').click();
  assert.equal(messages.at(-1).payload.command, 'new-project');
  assert.equal(document.querySelector('.start-page'), null);
  show();
  document.body.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
  assert.equal(document.querySelector('.start-page'), null);
  show();
  const count = messages.length;
  document.querySelector('.start-resume').click();
  assert.equal(document.querySelector('.start-page'), null);
  assert.equal(messages.length, count, 'Go to the designer just closes the screen');
  show();
  document.querySelector('[data-kind="button"]').dispatchEvent(new window.MouseEvent('dblclick', { bubbles: true }));
  assert.equal(document.querySelector('.start-page'), null);
});

test('an empty recent list invites opening a project', () => {
  const { window, document, messages } = fixture();
  window.forma.receive({ type: 'designer', action: 'start-page', templates: [], recent: [] });
  assert.match(document.querySelector('.start-empty').textContent, /will show up here/);
  document.querySelector('.start-empty button').click();
  assert.equal(messages.at(-1).payload.command, 'open');
});
