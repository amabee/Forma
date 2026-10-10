const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../../src/Forma.Builder/DesignerWeb');
test('workspace opens/closes code dock, reports bounds and filters library/properties', async t => {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'index.html'), 'utf8'), { runScripts: 'outside-only', pretendToBeVisual: true });
  t.after(() => dom.window.close());
  const { window } = dom, messages = [], received = [];
  window.forma = { send: value => messages.push(value) }; window.formaDesigner = { receive: value => received.push(value) };
  const doc = window.document;
  doc.getElementById('editor-mount').getBoundingClientRect = () => ({ x: 292, y: 440, width: 700, height: 280 });
  window.eval(fs.readFileSync(path.join(base, 'workspace.js'), 'utf8'));
  window.formaDesigner.receive({ action: 'editor-open' });
  await new Promise(resolve => window.requestAnimationFrame(resolve));
  assert.equal(doc.getElementById('editor-dock').hidden, false);
  assert.equal(messages.at(-1).event, 'editor-bounds'); assert.equal(messages.at(-1).payload.x, 292);
  doc.querySelector('[data-library="layouts"]').click();
  assert.ok([...doc.querySelectorAll('.tool-group')].some(group => group.hidden));
  doc.querySelector('[data-library="all"]').click(); assert.ok([...doc.querySelectorAll('.tool-group')].every(group => !group.hidden));
  doc.getElementById('property-editors').innerHTML = '<details class="property-group"><summary>General</summary><label>Text<input /></label><label>Font<input /></label></details>';
  const search = doc.getElementById('property-search'); search.value = 'font'; search.dispatchEvent(new window.Event('input'));
  assert.equal(doc.querySelector('.property-group label').hidden, true);
  window.formaDesigner.receive({ action: 'state', title: 'MainForm', hasUnsavedChanges: true });
  assert.equal(doc.getElementById('project-state'), null); assert.equal(received.at(-1).title, 'MainForm');
  window.formaDesigner.receive({ action: 'editor-close' }); assert.equal(doc.getElementById('editor-dock').hidden, true);
});

test('header is consolidated and theme toggle persists choice without editing the form', t => {
  function fixture(saved) {
    const dom = new JSDOM(fs.readFileSync(path.join(base, 'index.html'), 'utf8'), { url: 'https://forma.test', runScripts: 'outside-only', pretendToBeVisual: true });
    t.after(() => dom.window.close()); const { window } = dom, messages = [];
    window.forma = { send: value => messages.push(value) }; window.formaDesigner = { receive() {} };
    if (saved) window.localStorage.setItem('forma.workspace.theme', saved);
    window.eval(fs.readFileSync(path.join(base, 'workspace.js'), 'utf8'));
    return { window, messages, doc: window.document };
  }
  const { window, messages, doc } = fixture();
  assert.ok(doc.querySelector('.project-bar > .menubar'));
  assert.equal(doc.getElementById('project-caption'), null);
  assert.equal(doc.querySelectorAll('.project-bar > [data-preview], .project-bar > [data-command="save"]').length, 0);
  const toggle = doc.getElementById('theme-toggle');
  assert.equal(doc.documentElement.dataset.theme, 'dark'); toggle.click();
  assert.equal(doc.documentElement.dataset.theme, 'light');
  assert.equal(toggle.getAttribute('aria-label'), 'Switch to dark mode');
  assert.equal(window.localStorage.getItem('forma.workspace.theme'), 'light');
  assert.equal(messages.at(-1).event, 'theme'); assert.equal(messages.at(-1).payload.theme, 'light');
  const restored = fixture(window.localStorage.getItem('forma.workspace.theme'));
  assert.equal(restored.doc.documentElement.dataset.theme, 'light');
  restored.doc.getElementById('theme-toggle').click(); assert.equal(restored.doc.documentElement.dataset.theme, 'dark');
  assert.ok(messages.every(message => message.event !== 'property'));
});


test('Design/Code tabs preserve an open editor and route component context actions by ID', async t => {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'index.html'), 'utf8'), { runScripts: 'outside-only', pretendToBeVisual: true });
  t.after(() => dom.window.close());
  const { window } = dom, messages = [], doc = window.document;
  const control = doc.createElement('button'); control.id = 'button-id'; control.dataset.formaType = 'button';
  doc.getElementById('canvas-host').append(control);
  window.forma = { send: message => messages.push(message) };
  window.formaDesigner = { receive() {}, select() {}, cancelDrag() {}, state: { controls: [{ id: 'button-id', name: 'startButton', kind: 'button' }] } };
  window.eval(fs.readFileSync(path.join(base, 'workspace.js'), 'utf8'));
  control.dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: 80, clientY: 90 }));
  assert.equal(doc.getElementById('component-source-menu').hidden, false);
  doc.querySelector('[data-source-command="view-script"]').click();
  assert.equal(messages.at(-1).id, 'button-id');
  assert.equal(messages.at(-1).payload.command, 'view-script');
  assert.equal(doc.getElementById('component-source-menu').hidden, true);
  window.formaDesigner.receive({ action: 'editor-open', name: 'startButton' });
  assert.equal(doc.getElementById('stage').hidden, true);
  assert.equal(doc.getElementById('workspace-code-tab').textContent, 'startButton · Code');
  doc.getElementById('workspace-design-tab').click();
  assert.equal(doc.getElementById('stage').hidden, false);
  assert.equal(doc.getElementById('editor-dock').hidden, true);
  assert.equal(messages.at(-1).payload.visible, false);
  assert.equal(doc.getElementById('workspace-code-tab').hidden, false);
  doc.getElementById('workspace-code-tab').click();
  assert.equal(doc.getElementById('editor-dock').hidden, false);
  assert.equal(messages.at(-1).payload.visible, true);
  window.formaDesigner.state.controls[0].locked = true;
  control.dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
  assert.ok([...doc.querySelectorAll('[data-source-command]')].every(button => button.disabled));
});


test('multiple component tabs retain their identities, activate independently and close by key', t => {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'index.html'), 'utf8'), { runScripts: 'outside-only', pretendToBeVisual: true });
  t.after(() => dom.window.close());
  const { window } = dom, doc = window.document, messages = [];
  window.forma = { send: message => messages.push(message) }; window.formaDesigner = { receive() {} };
  window.eval(fs.readFileSync(path.join(base, 'workspace.js'), 'utf8'));
  const editors = [{ key: 'button-id', name: 'button1', dirty: true }, { key: 'label-id', name: 'label1', dirty: false }, { key: 'global:form', name: 'Global script', dirty: false }];
  window.formaDesigner.receive({ action: 'editor-tabs', activeKey: 'label-id', editors, show: true });
  const buttons = [...doc.querySelectorAll('[data-editor-key]')];
  assert.equal(buttons.length, 3);
  assert.match(buttons[0].textContent, /button1.*●/);
  assert.equal(buttons[1].getAttribute('aria-selected'), 'true');
  buttons[0].click();
  assert.equal(messages.at(-1).event, 'editor-activate');
  assert.equal(messages.at(-1).payload.key, 'button-id');
  window.formaDesigner.receive({ action: 'editor-tabs', activeKey: 'button-id', editors, show: true });
  doc.querySelector('[data-close-editor="label-id"]').click();
  assert.equal(messages.at(-1).event, 'editor-close-tab');
  assert.equal(messages.at(-1).payload.key, 'label-id');
  window.formaDesigner.receive({ action: 'editor-tabs', activeKey: 'button-id', editors: [editors[0], editors[2]] });
  assert.equal(doc.querySelector('[data-editor-key="button-id"]').getAttribute('aria-selected'), 'true');
  doc.getElementById('workspace-design-tab').click();
  window.formaDesigner.receive({ action: 'editor-tabs', activeKey: 'button-id', editors: [editors[0], editors[2]] });
  assert.equal(doc.getElementById('editor-dock').hidden, true);
  assert.equal(doc.querySelectorAll('[data-editor-key]').length, 2);
  window.formaDesigner.receive({ action: 'editor-tabs', editors: [], activeKey: null });
  assert.equal(doc.getElementById('stage').hidden, false);
  assert.equal(doc.querySelectorAll('[data-editor-key]').length, 0);
});
