const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const load = () => import(pathToFileURL(path.join(__dirname, '../../src/Forma.Builder/Frontend/code-assistance.mjs')).href);
const controls = [{ name: 'userAvatar', id: 'avatar-id', kind: 'avatar' }, { name: 'nameInput', id: 'input-id', kind: 'textbox' }, { name: 'inputFile', kind: 'filepicker' }];
function context(text) { return { pos: text.length, state: { doc: { sliceString: () => text } } }; }
test('Forma completion suggests API, known names, control-specific properties and custom values', async () => {
  const { formaCompletions: complete } = await load();
  const labels = (text, values) => complete(context(text), controls, values).options.map(x => x.label);
  assert.ok(labels('api.').includes('set'));
  assert.deepEqual(labels('api.set("us'), ['userAvatar', 'nameInput', 'inputFile']);
  assert.ok(labels('api.set("userAvatar", "').includes('source'));
  assert.ok(!labels('api.set("nameInput", "').includes('value'));
  assert.ok(labels('api.get("nameInput", "').includes('value'));
  assert.ok(labels('api.get("inputFile", "').includes('selectedPath'));
  assert.ok(!labels('api.set("inputFile", "').includes('selectedPath'));
  assert.deepEqual(labels('component.properties.', { greeting: 'Hello', count: 1 }), ['greeting', 'count']);
  assert.equal(complete(context('const apiName = 1;'), controls), null);
});
test('syntax diagnostics point to malformed JS/CSS/JSON and require an object for custom values', async () => {
  const { diagnose } = await load();
  for (const [language, source] of [['javascript', 'const = 3'], ['css', ':host { color:'], ['json', '{"count":}'], ['json', '[]']]) {
    const errors = await diagnose(language, source, controls);
    assert.equal(errors[0].severity, 'error'); assert.ok(errors[0].from >= 0 && errors[0].to <= source.length);
  }
  assert.deepEqual(await diagnose('javascript', 'api.on("click", async () => { api.set("userAvatar", "source", "https://example.com/avatar.png"); });', controls), []);
  assert.deepEqual(await diagnose('json', '{"count": 0}', controls), []);
});
test('diagnostics catch unsupported properties and unknown components without executing behavior', async () => {
  const { diagnose } = await load();
  const source = 'throw new Error("must not run"); api.set("nameInput", "value", "x"); api.get("missing", "text");';
  const issues = await diagnose('javascript', source, controls);
  assert.equal(issues.length, 2); assert.ok(issues.every(issue => issue.severity === 'warning'));
  assert.match(issues[0].message, /not supported/); assert.equal(source.slice(issues[0].from, issues[0].to), '"value"');
  assert.match(issues[1].message, /does not exist/);
  assert.deepEqual(await diagnose('javascript', 'api.set(dynamicName, dynamicProperty, nextValue);', controls), []);
});
test('forma API offers grid methods and properties and validates grid targets', async () => {
  const { formaCompletions: complete, diagnose } = await load();
  const all = [...controls, { name: 'employees', kind: 'datagridview' }];
  const labels = text => complete(context(text), all).options.map(x => x.label);
  assert.ok(labels('forma.').includes('addRow'));
  assert.deepEqual(labels('forma.addRow("'), ['employees']);
  assert.ok(labels('forma.set("employees", "').includes('rows'));
  assert.ok(labels('forma.get("employees", "').includes('columns'));
  assert.ok(labels('forma.on("').includes('Load'));
  assert.deepEqual(await diagnose('javascript', 'forma.addRow("employees", ["Angel"]); forma.set("employees", "rows", []);', all), []);
  const issues = await diagnose('javascript', 'forma.addRow("nameInput", []); forma.set("employees", "missing", 1);', all);
  assert.equal(issues.length, 2); assert.match(issues[0].message, /DataGridView/);
  assert.match(issues[1].message, /forma.set/);
});
test('reactive API and binding values are suggested and checked', async () => {
  const { formaCompletions: complete, diagnose } = await load();
  const labels = text => complete(context(text), controls).options.map(x => x.label);
  for (const method of ['ref', 'reactive', 'bind', 'computed', 'watch', 'effect']) assert.ok(labels('forma.').includes(method));
  assert.ok(labels('forma.bind("nameInput", "').includes('value'));
  assert.deepEqual(labels('const name = forma.bind("nameInput", "value"); name.'), ['value', 'subscribe']);
  assert.deepEqual(labels('const total = forma.computed(() => 10); total.'), ['value', 'subscribe']);
  assert.deepEqual(await diagnose('javascript', 'const name = forma.bind("nameInput", "value"); forma.effect(() => name.value);', controls), []);
  const issues = await diagnose('javascript', 'forma.bind("nameInput", "unsupported"); forma.bind("missing", "value");', controls);
  assert.equal(issues.length, 2);
});
