const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');

function fixture() {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only' });
  const { window } = dom;
  window.chrome = { webview: { postMessage() {}, addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/status.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  const state = (status, hasUnsavedChanges = false) => window.formaDesigner.receive({
    action: 'state', id: 'root', title: 'Form1', status, hasUnsavedChanges, canUndo: false, canRedo: false, selectedId: 'root',
    controls: [{ id: 'root', name: 'form1', kind: 'form', enabled: true, visible: true, width: 640, height: 440 }], propertySchema: [],
  });
  return { window, document: window.document, state };
}

test('problems switch the status bar to an error tone and are kept in a history', () => {
  const { document, state } = fixture();
  const bar = document.querySelector('.statusbar'), toggle = document.querySelector('.status-problems');
  state('Ready');
  assert.equal(bar.dataset.tone, '');
  assert.equal(toggle.hidden, true);
  state('Designer error: Invalid Dock value.');
  state('Designer error: Invalid Dock value.');
  assert.equal(bar.dataset.tone, 'error');
  assert.equal(toggle.hidden, false);
  assert.equal(toggle.textContent, '⚠ 1');
  state('Property updated');
  assert.equal(bar.dataset.tone, '');
  assert.equal(toggle.hidden, false, 'history survives later successful messages');
  toggle.click();
  const list = document.querySelector('.status-problem-list');
  assert.equal(list.hidden, false);
  assert.match(list.textContent, /Invalid Dock value/);
  list.querySelector('.status-problem-heading button').click();
  assert.equal(toggle.hidden, true);
});

test('the form name shows an unsaved marker while changes are unsaved', () => {
  const { document, state } = fixture();
  state('Ready', true);
  assert.equal(document.getElementById('form-name').dataset.unsaved, 'true');
  state('Saved', false);
  assert.equal(document.getElementById('form-name').dataset.unsaved, 'false');
});
