const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');
function fixture(behavior) {
  const { window } = new JSDOM('<button id="button"></button><input id="name" value=""><span id="label"></span>', { runScripts: 'outside-only' });
  const messages = []; window.log = [];
  window.forma = { send: message => messages.push(message) };
  for (const file of ['reactivity.js', 'component-customization.js']) window.eval(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb', file), 'utf8'));
  const controls = [
    { id: 'button', name: 'submitButton', kind: 'button', enabled: true, visible: true, customization: { behavior } },
    { id: 'name', name: 'firstName', kind: 'textbox', enabled: true, visible: true, text: '' },
    { id: 'label', name: 'greetingLabel', kind: 'label', enabled: true, visible: true, text: '' }
  ];
  const apply = () => window.formaCustomization.apply({ controls }, true);
  apply();
  return { window, controls, apply, messages, input: window.document.getElementById('name') };
}
test('binding declared outside events reads current input and watches live user and host updates', () => {
  const { window, input, controls, apply, messages } = fixture(`
    const name = forma.bind('firstName', 'value');
    window.nameRef = name;
    forma.on('Click', () => window.log.push(name.value));
    name.subscribe((value, previous) => window.log.push([value, previous]));
    forma.effect(() => forma.set('greetingLabel', 'text', name.value));
  `);
  const click = () => window.document.getElementById('button').click();
  input.value = 'Angel'; input.dispatchEvent(new window.Event('input', { bubbles: true }));
  assert.deepEqual(Array.from(window.log[0]), ['Angel', '']); click(); assert.equal(window.log[1], 'Angel');
  assert.equal(messages.at(-1).payload.value, 'Angel');
  const count = messages.length; apply(); apply(); assert.equal(messages.length, count);
  input.value = 'Jane'; controls[1].text = 'Jane'; apply();
  assert.deepEqual(Array.from(window.log.at(-1)), ['Jane', 'Angel']);
  assert.equal(messages.at(-1).payload.value, 'Jane');
  window.nameRef.value = 'John'; assert.equal(messages.at(-1).payload.property, 'text');
  assert.equal(messages.at(-1).payload.value, 'John');
  controls[1].name = 'renamed'; apply(); input.value = 'Renamed'; click(); assert.equal(window.log.at(-1), 'Renamed');
});
test('refs, shallow reactive objects, computed values and dynamic dependencies stay reactive', () => {
  const { window, messages } = fixture(`
    const count = forma.ref(1);
    const state = forma.reactive({ useCount: true, alternate: 10 });
    const result = forma.computed(() => state.useCount ? count.value * 2 : state.alternate);
    window.countRef = count; window.stateRef = state; window.resultRef = result;
    forma.effect(() => window.log.push(result.value));
  `);
  assert.deepEqual(Array.from(window.log), [2]);
  window.countRef.value++; assert.deepEqual(Array.from(window.log), [2, 4]);
  window.stateRef.useCount = false; assert.equal(window.resultRef.value, 10);
  const count = window.log.length; window.countRef.value = 5; assert.equal(window.log.length, count);
  window.stateRef.alternate = 12; assert.equal(window.log.at(-1), 12);
  window.stateRef.alternate = 12; assert.equal(window.log.at(-1), 12); assert.equal(window.log.length, count + 1);
  assert.throws(() => window.eval('"use strict"; window.resultRef.value = 9;'), /getter|read only|Cannot set/i);
  assert.equal(messages.length, 0);
});
test('watch immediate options, unsubscribe and effect cleanup run correctly and stop on destruction', () => {
  const { window, input, messages } = fixture(`
    const count = forma.ref(0); window.countRef = count;
    window.stopWatch = forma.watch(count, (value, old) => window.log.push(['watch', value, old]), { immediate: true });
    forma.effect(() => {
      const value = count.value; window.log.push(['effect', value]);
      return () => window.log.push(['cleanup', value]);
    });
    forma.bind('firstName', 'value').subscribe(value => window.log.push(['input', value]));
  `);
  assert.deepEqual(Array.from(window.log[0]), ['watch', 0, undefined]);
  window.countRef.value = 1;
  assert.deepEqual(Array.from(window.log, row => row[0]), ['watch', 'effect', 'watch', 'cleanup', 'effect']);
  window.stopWatch(); window.countRef.value = 2;
  assert.equal(window.log.filter(row => row[0] === 'watch').length, 2);
  window.formaCustomization.clear(); const count = window.log.length;
  input.value = 'After close'; input.dispatchEvent(new window.Event('input', { bubbles: true }));
  window.countRef.value = 3; assert.equal(window.log.length, count);
  assert.equal(messages.length, 0);
});
test('removed or replaced controls refresh bindings by stable ID without leaking old behavior effects', () => {
  const { window, controls, apply, messages } = fixture(`
    window.binding = forma.bind('firstName', 'value');
    forma.effect(() => window.log.push(window.binding.value));
  `);
  const replacement = window.document.createElement('input'); replacement.id = 'name'; replacement.value = 'Replacement';
  window.document.getElementById('name').replaceWith(replacement); apply();
  assert.equal(window.log.at(-1), 'Replacement');
  controls.splice(1, 1); replacement.remove(); apply(); assert.equal(window.log.at(-1), undefined);
  assert.equal(window.binding.value, undefined); assert.throws(() => { window.binding.value = 'Bad'; }, /must match/);
  controls[0].customization.behavior = 'window.newRef = forma.ref(0); forma.effect(() => window.log.push(window.newRef.value));';
  apply(); window.newRef.value = 1; assert.equal(window.log.at(-1), 1);
  assert.equal(messages.length, 0);
});
test('array bindings deduplicate host snapshots and return defensive grid copies', () => {
  const { window, controls, apply } = fixture('');
  controls[2].kind = 'datagridview'; controls[2].rows = [['Angel']];
  controls[0].customization.behavior = `const rows = forma.bind('greetingLabel', 'rows'); window.rowsRef = rows; forma.effect(() => window.log.push(rows.value));`;
  apply(); const count = window.log.length;
  controls[2].rows = [['Angel']]; apply(); assert.equal(window.log.length, count);
  window.rowsRef.value[0][0] = 'Mutation'; assert.equal(controls[2].rows[0][0], 'Angel');
  controls[2].rows = [['Jane']]; apply(); assert.equal(window.log.at(-1)[0][0], 'Jane');
});
test('reactive callback errors and feedback loops are reported without freezing the runtime', () => {
  const { window, messages } = fixture(`
    const ref = forma.ref(0); window.ref = ref;
    forma.effect(() => { if (ref.value === 1) throw new Error('reactive failed'); });
    forma.watch(ref, () => { throw new Error('watch failed'); });
  `);
  window.ref.value = 1;
  assert.ok(messages.some(message => message.payload.message === 'reactive failed'));
  assert.ok(messages.some(message => message.payload.message === 'watch failed'));
  window.ref.value = 2;
  const scope = window.formaReactivity.createScope({ error: error => window.log.push(error.message) });
  const ref = scope.ref(0); scope.effect(() => { ref.value++; });
  assert.match(window.log.at(-1), /did not stabilize/);
  scope.dispose();
});
