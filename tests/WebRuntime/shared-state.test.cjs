const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');
const plain = value => JSON.parse(JSON.stringify(value));

function fixture(globalScript, sharedState) {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/preview.html'), 'utf8'), { runScripts: 'outside-only' });
  const { window } = dom; const messages = [];
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/reactivity.js', 'src/Forma.Builder/DesignerWeb/component-customization.js'])
    window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  window.forma.receive({ type: 'create', id: 'root', control: 'form', properties: {} });
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  window.formaDesigner.preview = true;
  window.formaCustomization.apply({ id: 'root', globalScript, sharedState, controls: [{ id: 'root', name: 'form1', kind: 'form', enabled: true, visible: true }] }, true);
  const sets = () => messages.filter(message => message.event === 'shared-set').map(message => plain(message.payload));
  return { window, messages, sets };
}

test('the first window publishes what main.js puts in forma.shared', () => {
  const { window, sets } = fixture('forma.shared.count = 0; forma.shared.user = { name: "Ana" }; forma.shared.format = () => "x"; window.shared = forma.shared;');
  assert.deepEqual(sets().map(set => [set.key, set.value]), [['count', 0], ['user', { name: 'Ana' }]], 'functions stay local');
  window.shared.count = 3;
  assert.deepEqual(sets().at(-1), { sourceId: 'root', key: 'count', value: 3, deleted: false });
  delete window.shared.user;
  assert.equal(sets().at(-1).deleted, true);
});

test('a later window starts from the shared values and main.js only fills missing ones', () => {
  const { window, sets } = fixture('forma.shared.count = 0; forma.shared.theme = "light"; window.shared = forma.shared;', { count: 5 });
  assert.equal(window.shared.count, 5, 'an initializer does not reset a value another form set');
  assert.equal(window.shared.theme, 'light');
  assert.deepEqual(sets().map(set => set.key), ['theme']);
  window.shared.count = 6;
  assert.equal(sets().at(-1).value, 6, 'after startup, assignments apply normally');
});

test('changes from other windows update forma.shared reactively without being sent back', () => {
  const { window, sets } = fixture('window.seen = []; forma.shared.count = 0; forma.effect(() => window.seen.push(forma.shared.count)); window.shared = forma.shared;');
  const before = sets().length;
  window.forma.receive({ type: 'designer', action: 'shared-changed', key: 'count', value: 9, deleted: false });
  assert.equal(window.shared.count, 9);
  assert.equal(window.seen.at(-1), 9);
  assert.equal(sets().length, before, 'remote changes are not echoed');
});

test('refs and reactive objects in forma.shared stay local to their window', () => {
  const { sets } = fixture('forma.shared.session = forma.ref(1); forma.shared.cart = forma.reactive({ items: 0 }); forma.shared.total = 3;');
  assert.deepEqual(sets().map(set => set.key), ['total']);
});
