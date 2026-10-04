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
