const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');

function fixture(behavior) {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/preview.html'), 'utf8'), { runScripts: 'outside-only' });
  const { window } = dom; const messages = [];
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/reactivity.js', 'src/Forma.Builder/DesignerWeb/component-customization.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.forma.receive({ type: 'create', id: 'open', control: 'button', parentId: 'root', properties: { text: 'Open' } });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  window.formaDesigner.preview = true;
  window.formaCustomization.apply({ id: 'root', controls: [
    { id: 'root', name: 'form1', kind: 'form', enabled: true, visible: true },
    { id: 'open', name: 'openButton', kind: 'button', enabled: true, visible: true, customization: { behavior } },
  ] }, true);
  return { window, document: window.document, messages };
}

test('showForm asks the host for a window and resolves with the closed form\'s result', async () => {
  const { window, document, messages } = fixture('forma.on("click", async () => { window.outcome = await forma.showForm("settings", { modal: true }); });');
  document.getElementById('open').click();
  const request = messages.find(message => message.event === 'show-form');
  assert.ok(request);
  assert.equal(request.payload.form, 'settings');
  assert.equal(request.payload.modal, true);
  window.forma.receive({ type: 'designer', action: 'form-closed', requestId: request.payload.requestId, result: { saved: true } });
  await new Promise(resolve => setTimeout(resolve));
  assert.deepEqual(JSON.parse(JSON.stringify(window.outcome)), { saved: true });
});

test('showForm rejects when the form does not exist, and closeForm sends the result', async () => {
  const { window, document, messages } = fixture('forma.on("click", () => { forma.showForm("missing").catch(error => { window.failure = error.message; }); forma.closeForm({ choice: "ok" }); });');
  document.getElementById('open').click();
  const request = messages.find(message => message.event === 'show-form');
  window.forma.receive({ type: 'designer', action: 'form-closed', requestId: request.payload.requestId, error: "There is no form named 'missing'." });
  await new Promise(resolve => setTimeout(resolve));
  assert.equal(window.failure, "There is no form named 'missing'.");
  const close = messages.find(message => message.event === 'close-form');
  assert.deepEqual(JSON.parse(JSON.stringify(close.payload.result)), { choice: 'ok' });
});
