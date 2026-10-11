const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../../src/Forma.Builder/DesignerWeb');
function fixture(t, globalScript = '', scripts = {}) {
  const { window } = new JSDOM('<div id="form"><div id="panel"><button id="button"><span id="inner">Click</span></button><input id="name" value=""><input id="number" type="number" value="2"><input id="check" type="checkbox"><div id="grid"></div></div></div><div id="timer"></div>', { runScripts: 'outside-only' });
  t.after(() => window.close());
  const messages = []; window.log = []; window.forma = { send: message => messages.push(message) };
  for (const file of ['reactivity.js', 'module-runtime.bundle.js', 'component-customization.js']) window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  const controls = [
    { id: 'form', name: 'Form1', kind: 'form', text: 'Hello', width: 640, height: 440 },
    { id: 'panel', name: 'panel1', kind: 'panel', parentId: 'form' },
    { id: 'button', name: 'button1', kind: 'button', parentId: 'panel', text: 'Click' },
    { id: 'name', name: 'nameInput', kind: 'textbox', parentId: 'panel', text: '' },
    { id: 'number', name: 'numberInput', kind: 'numericupdown', parentId: 'panel', number: 2 },
    { id: 'check', name: 'checkInput', kind: 'checkbox', parentId: 'panel', checked: false },
    { id: 'grid', name: 'grid1', kind: 'datagridview', parentId: 'panel', columns: ['Name'], rows: [['Alice']] },
    { id: 'timer', name: 'timer1', kind: 'timer', parentId: 'form', component: true, interval: 1000 },
  ].map(item => ({ ...item, enabled: true, visible: true, ...(scripts[item.id] ? { customization: { behavior: scripts[item.id] } } : {}) }));
  const state = { id: 'form', globalScript, controls, propertyOrigins: [] };
  const apply = () => window.formaCustomization.apply(state, true);
  apply();
  return { window, messages, controls, state, apply, item: id => controls.find(item => item.id === id), element: id => window.document.getElementById(id) };
}

test('event metadata preserves native events and shares identity and handled status through bubbling', t => {
  const { window, element, messages } = fixture(t, 'forma.root.on("Click", event => { window.rootEvent = event; });', { button: 'forma.on("Click", event => { window.buttonEvent = event; event.handled = true; });' });
  const native = new window.MouseEvent('click', { bubbles: true, cancelable: true, clientX: 21, clientY: 45, ctrlKey: true });
  element('inner').dispatchEvent(native);
  const own = window.buttonEvent, root = window.rootEvent;
  assert.equal(root.eventId, own.eventId); assert.ok(root.eventId.startsWith('evt_'));
  assert.ok(Number.isFinite(root.timestamp)); assert.equal(root.eventName, 'Click'); assert.equal(root.type, 'click');
  assert.equal(root.source.id, 'button'); assert.equal(root.id, 'button'); assert.equal(root.source.name, 'button1');
  assert.equal(root.subscriber.id, 'form'); assert.equal(own.subscriber.id, 'button');
  assert.deepEqual(Array.from(root.path, component => component.id), ['button', 'panel', 'form']);
  assert.equal(root.phase, 'bubble'); assert.equal(own.phase, 'target'); assert.equal(root.handled, true);
  assert.equal(root.originalEvent, native); assert.equal(root.target, element('inner')); assert.equal(root.currentTarget, element('form'));
  assert.ok(root instanceof window.MouseEvent); assert.equal(root.constructor, window.MouseEvent);
  assert.equal(root.mouse.x, 21); assert.equal(root.mouse.y, 45); assert.equal(root.modifiers.ctrl, true);
  assert.equal(root.property, undefined); assert.equal(root.value, undefined); assert.equal(root.origin, 'programmatic');
  assert.equal(root.source.get('text'), 'Click'); root.source.set('text', 'Updated');
  assert.equal(messages.at(-1).id, 'button'); assert.equal(messages.at(-1).payload.value, 'Updated');
  assert.equal(root.source.bind('text').value, 'Click');
  const serialized = JSON.parse(JSON.stringify(root));
  assert.deepEqual(serialized.source, { id: 'button', name: 'button1', kind: 'button' });
  assert.equal(serialized.originalEvent, undefined); assert.equal(serialized.source.element, undefined);
  assert.equal(serialized.eventId, root.eventId); assert.equal(serialized.handled, true);
});

test('cancellation delegates to the browser and handled does not stop propagation', t => {
  const { window, element } = fixture(t, 'forma.root.on("Click", () => window.log.push("root"));', { button: 'forma.on("Click", event => { window.event = event; event.preventDefault(); event.stopPropagation(); });' });
  const native = new window.MouseEvent('click', { bubbles: true, cancelable: true });
  assert.equal(element('button').dispatchEvent(native), false);
  assert.equal(window.event.defaultPrevented, true); assert.equal(window.event.propagationStopped, true);
  assert.deepEqual([...window.log], []);
  assert.throws(() => { window.event.handled = 'yes'; }, /boolean/);
});

test('Input gives current and previous values; PropertyChanged follows Input and deduplicates native confirmation', async t => {
  const { window, state, apply, item, element } = fixture(t, `
    forma.root.on('Input', ({ id, property, value, previousValue }) => window.log.push(['input', id, property, value, previousValue]));
    forma.root.on('PropertyChanged', event => window.log.push(['property', event.id, event.property, event.value, event.previousValue, event.origin]));
  `);
  element('name').value = 'Angel'; element('name').dispatchEvent(new window.Event('input', { bubbles: true }));
  assert.deepEqual(Array.from(window.log[0]), ['input', 'name', 'value', 'Angel', '']);
  assert.equal(window.log.length, 1); await Promise.resolve();
  assert.deepEqual(Array.from(window.log[1]), ['property', 'name', 'value', 'Angel', '', 'programmatic']);
  // An unrelated state publication must not revert the pending DOM value.
  item('button').text = 'Changed'; apply();
  assert.equal(window.log.filter(event => event[1] === 'name').length, 2);
  item('name').text = 'Angel'; apply();
  assert.equal(window.log.filter(event => event[1] === 'name').length, 2);
  element('name').value = 'Maria'; item('name').text = 'Maria'; state.propertyOrigins = [{ id: 'name', property: 'text', origin: 'binding' }]; apply();
  assert.deepEqual(Array.from(window.log.at(-1)), ['property', 'name', 'value', 'Maria', 'Angel', 'binding']);
  apply(); assert.equal(window.log.filter(event => event[1] === 'name').length, 3);
});

test('numeric and checkbox changes carry typed values and preview cleanup cancels pending notifications', async t => {
  const { window, element } = fixture(t, 'forma.root.on("Change", event => window.log.push([event.property, event.value, event.previousValue])); forma.root.on("PropertyChanged", () => window.log.push("property"));');
  element('number').value = '7'; element('number').dispatchEvent(new window.Event('change', { bubbles: true }));
  assert.deepEqual(Array.from(window.log[0]), ['value', 7, 2]);
  element('check').checked = true; element('check').dispatchEvent(new window.Event('change', { bubbles: true }));
  assert.deepEqual(Array.from(window.log[1]), ['checked', true, false]);
  window.formaCustomization.clear(); await Promise.resolve();
  assert.equal(window.log.length, 2);
});

test('confirmed model changes bubble from nonvisual components with origins and defensive values', t => {
  const { window, state, apply, item, element } = fixture(t, 'forma.root.on("PropertyChanged", event => { window.event = event; window.log.push([event.id, event.property, event.value, event.previousValue, event.origin]); });');
  item('timer').interval = 2000; state.propertyOrigins = [{ id: 'timer', property: 'interval', origin: 'programmatic' }]; apply();
  assert.deepEqual(Array.from(window.log[0]), ['timer', 'interval', 2000, 1000, 'programmatic']);
  assert.equal(window.event.source.kind, 'timer'); assert.equal(window.event.subscriber.id, 'form');
  assert.equal(window.event.source.element, element('timer'));
  assert.equal(window.event.phase, 'bubble'); assert.equal(window.event.cancelable, false);
  item('timer').enabled = false; state.propertyOrigins = []; apply();
  assert.equal(window.log.at(-1)[4], 'native');
});

test('property snapshots use actual committed values, keep arrays defensive, and mark binding writes', t => {
  const { window, state, apply, item, messages } = fixture(t, 'forma.root.on("PropertyChanged", event => { window.event = event; window.log.push([event.id, event.property, event.value, event.previousValue, event.origin]); }); window.root = forma.root;');
  item('number').number = 100; state.propertyOrigins = [{ id: 'number', property: 'value', origin: 'programmatic' }]; apply();
  assert.deepEqual(Array.from(window.log[0]), ['number', 'value', 100, 2, 'programmatic']);
  item('grid').rows = [['Alice'], ['Bob']]; apply();
  window.event.value[0][0] = 'mutated';
  assert.deepEqual(item('grid').rows, [['Alice'], ['Bob']]);
  window.root.bind('text').value = 'Updated title';
  assert.equal(messages.at(-1).payload.origin, 'binding');
});

test('native bridge events retain details and keyboard data remains available', t => {
  const { window, element } = fixture(t, 'forma.root.on("Tick", event => { window.tick = event; }); forma.root.on("KeyDown", event => { window.key = event; });');
  window.formaCustomization.event('timer', 'tick', { count: 3 });
  assert.equal(window.tick.id, 'timer'); assert.equal(window.tick.origin, 'native'); assert.equal(window.tick.detail.count, 3);
  element('name').dispatchEvent(new window.KeyboardEvent('keydown', { bubbles: true, key: 'Enter', code: 'Enter', shiftKey: true }));
  assert.equal(window.key.keyboard.key, 'Enter'); assert.equal(window.key.key, 'Enter'); assert.equal(window.key.modifiers.shift, true);
});

test('Destroyed identifies the removed component, and stale handles cannot add listeners', t => {
  const { window, state, apply, item } = fixture(t, '', { button: 'forma.on("Destroyed", event => { window.destroyed = event; });' });
  item('button').name = 'renamedButton'; apply();
  state.controls = state.controls.filter(control => control.id !== 'button'); apply();
  assert.equal(window.destroyed.id, 'button'); assert.equal(window.destroyed.source.name, 'renamedButton');
  assert.equal(window.destroyed.source.kind, 'button'); assert.equal(window.destroyed.subscriber.id, 'button');
  assert.throws(() => window.destroyed.source.on('Click', () => {}), /destroyed/);
});
