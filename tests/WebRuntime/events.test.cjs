const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');
function fixture(source, observe = false) {
  const { window } = new JSDOM('<div id="control"><input required></div><button id="other"></button>', { runScripts: 'outside-only' });
  window.log = []; window.messages = [];
  window.forma = { send: m => window.messages.push(m) };
  if (observe) window.ResizeObserver = class {
    constructor(callback) { window.observer = this; this.callback = callback; }
    observe() {} disconnect() { this.disconnected = true; }
  };
  window.eval(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/reactivity.js'), 'utf8'));
  window.eval(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/component-customization.js'), 'utf8'));
  const item = { id: 'control', name: 'control1', enabled: true, visible: true, x: 0, y: 0, width: 100, height: 30,
    customization: { behavior: source, characteristics: '{}' } };
  const apply = () => window.formaCustomization.apply({ controls: [item] }, true);
  return { window, item, apply, element: window.document.getElementById('control') };
}
test('all named events are suggested and native aliases retain original event payloads', async () => {
  const { eventNames, formaCompletions } = await import(pathToFileURL(path.join(base, 'src/Forma.Builder/Frontend/code-assistance.mjs')).href);
  const lifecycle = ['Load', 'Ready', 'Created', 'Mounted', 'Updated', 'Destroyed', 'Resize', 'Move', 'Layout', 'Validating', 'Validated'];
  const native = eventNames.filter(name => !lifecycle.includes(name));
  const { window, apply, element } = fixture(native.map(name => `api.on(${JSON.stringify(name)}, e => window.log.push([${JSON.stringify(name)}, e.type]));`).join('\n'));
  apply();
  for (const name of native) {
    const type = ({ DoubleClick: 'dblclick', MouseWheel: 'wheel' })[name] ?? name.toLowerCase();
    element.dispatchEvent(new window.Event(type, { bubbles: true, cancelable: true }));
    assert.deepEqual(Array.from(window.log.at(-1)), [name, type]);
  }
  const text = 'api.on("';
  const options = formaCompletions({ pos: text.length, state: { doc: { sliceString: () => text } } }).options;
  assert.ok(eventNames.every(name => options.some(option => option.label === name)));
});
test('startup runs after every script is installed, updates deduplicate, destroyed runs before cleanup', () => {
  const { window, item } = fixture(`
    for (const name of ['Created','Mounted','Ready','Load','Updated','Move','Resize','Layout','Destroyed'])
      api.on(name, e => window.log.push([name, e.detail.previous?.x]));
    api.on('Load', () => api.find('other1').click());
    api.cleanup(() => window.log.push(['cleanup']));
  `);
  const other = { id: 'other', name: 'other1', enabled: true, visible: true,
    customization: { behavior: 'api.on("Click", () => window.log.push(["other-click"]));' } };
  const update = () => window.formaCustomization.apply({ controls: [item, other] }, true);
  update();
  assert.deepEqual(Array.from(window.log, e => e[0]), ['Created', 'Mounted', 'Ready', 'Load', 'other-click']);
  update(); assert.equal(window.log.length, 5);
  item.x = 20; item.width = 120; update();
  assert.deepEqual(Array.from(window.log.slice(5), e => e[0]), ['Updated', 'Move', 'Resize', 'Layout']);
  assert.equal(window.log[5][1], 0);
  window.formaCustomization.clear();
  assert.deepEqual(Array.from(window.log.slice(-2), e => e[0]), ['Destroyed', 'cleanup']);
  update(); assert.ok(window.log.filter(e => e[0] === 'Load').length === 2);
  assert.equal(window.messages.length, 0);
});
test('focus inside composite controls validates, can cancel, and submit/reset have hooks', () => {
  const { window, apply, element } = fixture(`
    window.api = api;
    api.on('Focus', e => window.log.push(['focus', e.target.tagName]));
    api.on('Validating', e => { window.log.push(['validating']); if (window.cancel) e.preventDefault(); });
    api.on('Validated', () => window.log.push(['validated']));
    api.on('Submit', e => { window.log.push(['submit']); if (window.cancelSubmit) e.preventDefault(); });
    api.on('Reset', () => window.log.push(['reset']));
  `);
  apply(); const input = element.querySelector('input');
  input.dispatchEvent(new window.FocusEvent('focus'));
  assert.equal(window.log[0][1], 'INPUT');
  assert.equal(window.api.submit(), false); assert.ok(!window.log.some(e => e[0] === 'submit'));
  input.value = 'Valid'; window.cancel = true;
  assert.equal(window.api.validate(), false); assert.ok(!window.log.some(e => e[0] === 'validated'));
  window.cancel = false; input.dispatchEvent(new window.FocusEvent('blur'));
  assert.equal(window.log.at(-1)[0], 'validated');
  assert.equal(window.api.submit(), true);
  window.cancelSubmit = true; assert.equal(window.api.submit(), false);
  assert.equal(window.api.reset(), true); assert.equal(window.log.at(-1)[0], 'reset');
});
test('resize observes actual rendered dimensions and disconnects on destruction', () => {
  const { window, apply } = fixture(`api.on('Resize', e => window.log.push(e.detail.current.width)); api.on('Layout', () => window.log.push('layout'));`, true);
  apply(); const observer = window.observer;
  observer.callback([{ contentRect: { width: 100, height: 30 } }]); assert.equal(window.log.length, 0);
  observer.callback([{ contentRect: { width: 130, height: 30 } }]);
  assert.deepEqual(Array.from(window.log), [130, 'layout']);
  window.formaCustomization.clear(); assert.ok(observer.disconnected);
  observer.callback([{ contentRect: { width: 140, height: 30 } }]); assert.equal(window.log.length, 2);
});
test('hidden lifecycle still runs, native image load is distinct, errors are reported and removal disposes once', async () => {
  const { window, item, apply, element } = fixture(`
    api.on('Load', () => window.log.push('load'));
    api.on('Click', async () => { throw new Error('handler failed'); });
    api.on('Destroyed', () => window.log.push('destroyed'));
  `);
  item.visible = false; apply(); assert.deepEqual(Array.from(window.log), ['load']);
  element.dispatchEvent(new window.Event('load')); assert.equal(window.log.length, 1);
  element.click(); await Promise.resolve(); assert.equal(window.messages.length, 0);
  item.visible = true; apply(); element.click(); await Promise.resolve();
  assert.equal(window.messages[0].payload.message, 'handler failed');
  window.formaCustomization.apply({ controls: [] }, true);
  window.formaCustomization.clear(); assert.deepEqual(Array.from(window.log), ['load', 'destroyed']);
});
test('forma aliases api without replacing the bridge and sends atomic grid operations', () => {
  const { window, item } = fixture(`
    window.scriptForma = forma;
    window.sameApi = forma === api;
    forma.on('Load', () => {
      forma.set('grid1', 'columns', ['Name', 'Department']);
      forma.set('grid1', 'rows', []);
      forma.addRow('grid1', ['Angel', 'Engineering']);
      forma.addRow('grid1', ['Jane', 'Design']);
      forma.updateRow('grid1', 0, ['Angel', 'HR']);
      forma.setCell('grid1', 1, 1, 'Operations');
      forma.removeRow('grid1', 0);
      forma.clearRows('grid1');
    });
  `);
  const bridge = window.forma;
  const grid = { id: 'other', name: 'grid1', kind: 'datagridview', columns: ['Name'], rows: [['Initial']], readOnly: true, sortingEnabled: true, filteringEnabled: true };
  window.formaCustomization.apply({ controls: [item, grid] }, true);
  assert.equal(window.forma, bridge); assert.ok(window.sameApi);
  assert.deepEqual(window.messages.map(m => m.event), ['set', 'set', 'grid', 'grid', 'grid', 'grid', 'grid', 'grid']);
  assert.deepEqual(window.messages.slice(2).map(m => m.payload.operation), ['addRow', 'addRow', 'updateRow', 'setCell', 'removeRow', 'clearRows']);
  assert.ok(window.messages.every(m => m.id === 'other' && m.payload.sourceId === 'control'));
  const rows = window.scriptForma.get('grid1', 'rows'); rows[0][0] = 'changed';
  const columns = window.scriptForma.get('grid1', 'columns'); columns[0] = 'changed';
  assert.equal(grid.rows[0][0], 'Initial'); assert.equal(grid.columns[0], 'Name');
  assert.equal(window.scriptForma.get('grid1', 'readOnly'), true);
  assert.throws(() => window.scriptForma.addRow('grid1', ['Bad', 10]), /strings/);
  assert.throws(() => window.scriptForma.set('grid1', 'rows', [null]), /strings/);
  assert.throws(() => window.scriptForma.removeRow('grid1', -1), /indices/);
  assert.throws(() => window.scriptForma.addRow('control1', ['Bad']), /DataGridView/);
});
