const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');

function fixture({ locked = false } = {}) {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only' });
  const { window } = dom; const messages = [];
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/event-inspector.bundle.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.forma.receive({ type: 'create', id: 'go', control: 'button', parentId: 'root', properties: { text: 'Go' } });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  window.formaDesigner.receive({ action: 'state', id: 'root', title: 'Form1', status: 'Ready', selectedId: 'go', selectedIds: ['go'], propertySchema: [],
    controls: [{ id: 'root', name: 'form1', kind: 'form', enabled: true, visible: true }, { id: 'go', name: 'button1', kind: 'button', parentId: 'root', enabled: true, visible: true, locked }] });
  messages.length = 0;
  const dblclick = target => target.dispatchEvent(new window.MouseEvent('dblclick', { bubbles: true, cancelable: true }));
  return { window, document: window.document, messages, dblclick };
}

test('double-clicking a control opens its default event handler', () => {
  const { document, messages, dblclick } = fixture();
  dblclick(document.getElementById('go'));
  const message = messages.at(-1);
  assert.equal(message.event, 'command');
  assert.equal(message.id, 'go');
  assert.equal(message.payload.command, 'edit-event');
  assert.equal(message.payload.eventName, 'Click');
  dblclick(document.getElementById('root'));
  assert.equal(messages.at(-1).payload.eventName, 'Load');
});

test('locked controls and preview mode ignore double-clicks', () => {
  const locked = fixture({ locked: true });
  locked.dblclick(locked.document.getElementById('go'));
  assert.equal(locked.messages.length, 0);
  const preview = fixture();
  preview.window.formaDesigner.preview = true;
  preview.dblclick(preview.document.getElementById('go'));
  assert.equal(preview.messages.length, 0);
});
