const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');
const plain = value => JSON.parse(JSON.stringify(value));

const schema = [
  { id: 'name', label: 'Name', category: 'General', editor: 'text' },
  { id: 'text', label: 'Text', category: 'General', editor: 'text' },
  { id: 'width', label: 'Width', category: 'Layout', editor: 'number' },
  { id: 'backColor', label: 'Background', category: 'Appearance', editor: 'color' },
];
const defaults = { name: 'button', text: 'Continue', width: 120, backColor: '#2878ff' };

function fixture(item) {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only', url: 'http://localhost/' });
  const { window } = dom; const messages = [];
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.forma.receive({ type: 'create', id: 'go', control: 'button', parentId: 'root', properties: {} });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  const state = values => window.formaDesigner.receive({ action: 'state', id: 'root', title: 'Form1', status: 'Ready', selectedId: 'go', selectedIds: ['go'], propertySchema: schema, defaults,
    controls: [{ id: 'root', name: 'form1', kind: 'form', enabled: true, visible: true }, { id: 'go', kind: 'button', parentId: 'root', enabled: true, visible: true, dock: 'none', x: 0, y: 0, height: 36, ...values }] });
  state(item);
  return { window, document: window.document, messages, state };
}

test('fields changed from the default are marked and reset with one click', () => {
  const { document, messages, state } = fixture({ name: 'button1', text: 'Save', width: 120, backColor: '#2878ff' });
  const row = id => document.getElementById(`prop-${id}`).closest('label');
  assert.ok(row('text').classList.contains('property-modified'));
  assert.equal(row('width').classList.contains('property-modified'), false);
  assert.equal(row('name').classList.contains('property-modified'), false, 'names are never offered a reset');
  row('text').querySelector('.property-reset').click();
  assert.deepEqual(plain([messages.at(-1).event, messages.at(-1).payload]), ['property', { property: 'text', value: 'Continue' }]);
  state({ name: 'button1', text: 'Continue', width: 120, backColor: '#2878ff' });
  assert.equal(row('text').classList.contains('property-modified'), false);
  assert.equal(row('text').querySelector('.property-reset').hidden, true);
});

test('color fields offer swatches and remember recently used colors', () => {
  const { window, document, messages } = fixture({ name: 'button1', text: 'Continue', width: 120, backColor: '#2878ff' });
  const input = document.getElementById('prop-backColor');
  const swatches = () => [...input.closest('label').querySelectorAll('.color-swatches button')];
  assert.ok(swatches().length >= 8);
  swatches().find(button => button.title === '#dc2626').click();
  assert.deepEqual(plain(messages.at(-1).payload), { property: 'backColor', value: '#dc2626' });
  assert.equal(swatches()[0].title, '#dc2626', 'the color just used comes first');
  assert.deepEqual(JSON.parse(window.localStorage.getItem('forma.inspector.colors')), ['#dc2626']);
});

test('properties overridden by component.css say so and can hand control back to the panel', () => {
  const css = '/* example:\n  background-color: #000;\n*/\n:host {\n  color: #1f2937;\n  background-color: #1a1a22;\n  padding: 8px;\n}\n\n:host:hover {\n  background-color: red;\n}';
  const { document, messages } = fixture({ name: 'button1', text: 'Continue', width: 120, backColor: '#2878ff', customization: { Css: css, Behavior: 'b', Events: '', Characteristics: '{}' } });
  const note = document.getElementById('prop-backColor').closest('label').querySelector('.property-override');
  assert.equal(note.hidden, false);
  assert.match(note.textContent, /Overridden by component\.css \(background-color\)/);
  assert.equal(document.getElementById('prop-width').closest('label').querySelector('.property-override'), null);
  note.querySelector('button').click();
  const payload = plain(messages.at(-1).payload);
  assert.equal(messages.at(-1).event, 'customize');
  assert.equal(payload.behavior, 'b');
  // Only the :host rule loses its background; the comment and :hover rule stay.
  assert.equal(payload.css, '/* example:\n  background-color: #000;\n*/\n:host {\n  color: #1f2937;\n  padding: 8px;\n}\n\n:host:hover {\n  background-color: red;\n}');
});

test('layout-shifting component.css without its own field gets a notice that removes it', () => {
  const css = ':host {\n  color: #fff;\n  border: 1px solid #d3deee;\n  padding: 8px 8px 8px 8px;\n}';
  const { document, messages } = fixture({ name: 'button1', text: 'Continue', width: 120, backColor: '#2878ff', customization: { Css: css, Behavior: '', Events: '', Characteristics: '{}' } });
  const notice = document.getElementById('css-override-summary');
  assert.equal(notice.hidden, false);
  assert.match(notice.textContent, /sets border, padding on this button/);
  notice.querySelector('button').click();
  assert.equal(plain(messages.at(-1).payload).css, ':host {\n  color: #fff;\n}');
});
