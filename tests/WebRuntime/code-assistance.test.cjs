const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const load = () => import(pathToFileURL(path.join(__dirname, '../../src/Forma.Builder/Frontend/code-assistance.mjs')).href);
const controls = [{ name: 'userAvatar', id: 'avatar-id', kind: 'avatar' }, { name: 'nameInput', id: 'input-id', kind: 'textbox' }, { name: 'inputFile', kind: 'filepicker' }];

test('shared scripting API is suggested and recognized by diagnostics', async () => {
  const { formaCompletions, diagnose } = await load();
  const suggestions = formaCompletions(context('forma.'), controls).options.map(option => option.label);
  for (const method of ['use', 'provide', 'shared']) assert.ok(suggestions.includes(method));
  assert.deepEqual(formaCompletions(context('forma.use("'), controls, {}, ['app', 'utils']).options.map(option => option.label), ['app', 'utils']);
  assert.deepEqual(await diagnose('javascript', 'forma.provide("app", { count: forma.ref(0) }); const app = forma.use("app"); forma.shared.name = "Angel";', controls), []);
});

test('shared runtime catalog suggests appearance, input, structured data and read-only properties', async () => {
  const { propertiesFor, diagnose } = await load();
  for (const [kind, properties] of Object.entries({
    button: ['fontSize', 'backColor', 'width', 'style'], textbox: ['placeholder', 'readOnly', 'maxLength'],
    avatar: ['shape', 'initials'], treeview: ['nodes', 'expandedNodes'], richtextbox: ['document'],
    tablelayoutpanel: ['columns', 'rowCount'], tooltip: ['placement', 'initialDelay'],
    filepicker: ['filter', 'dialogTitle'], pagination: ['pageSize', 'pageCount']
  })) for (const property of properties) assert.ok(propertiesFor(kind, 'get').includes(property), `${kind}.${property}`);
  assert.ok(!propertiesFor('pagination', 'set').includes('pageCount'));
  assert.ok(!propertiesFor('dialog', 'set').includes('result'));
  assert.ok(!propertiesFor('timer', 'get').includes('fontSize'));
  assert.ok(!propertiesFor('form', 'get').includes('x'));
  const source = 'forma.set("nameInput", "fontSize", 24); forma.bind("nameInput", "readOnly"); forma.set("userAvatar", "shape", "square");';
  assert.deepEqual(await diagnose('javascript', source, controls), []);
});

test('timer interval and loading/numeric properties are offered without unsupported-property diagnostics', async () => {
  const { propertiesFor, diagnose, formaCompletions } = await load();
  const known = [{ name: 'timer', kind: 'timer' }];
  assert.ok(propertiesFor('timer', 'get').includes('interval'));
  assert.ok(propertiesFor('timer', 'set').includes('interval'));
  assert.ok(formaCompletions(context('forma.set("timer", "'), known).options.some(option => option.label === 'interval'));
  assert.deepEqual(await diagnose('javascript', 'forma.set("timer", "interval", 1000);', known), []);
  const wrongType = await diagnose('javascript', 'forma.set("timer", "interval", "1000");', known);
  assert.equal(wrongType.length, 1); assert.match(wrongType[0].message, /without quotes/);
  assert.ok(propertiesFor('slider', 'set').includes('increment'));
  assert.ok(propertiesFor('spinner', 'get').includes('speed'));
  assert.ok(propertiesFor('skeleton', 'set').includes('isActive'));
});
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
test('toast scripting suggests editable settings and severity values', async () => {
  const { formaCompletions: complete, propertiesFor } = await load();
  const known = [{ name: 'notice', kind: 'toast' }];
  const labels = text => complete(context(text), known).options.map(option => option.label);
  assert.ok(labels('forma.set("notice", "').includes('variant'));
  assert.ok(labels('forma.set("notice", "variant", "').includes('caution'));
  assert.ok(labels('forma.showToast("notice", { variant: "').includes('error'));
  assert.ok(labels('forma.showToast("notice", {\n text: "Saved",\n variant: "').includes('success'));
  assert.ok(propertiesFor('toast', 'get').includes('isOpen'));
  assert.ok(!propertiesFor('toast', 'set').includes('isOpen'));
});
test('chip and button properties appear in scripting assistance', async () => {
  const { propertiesFor, diagnose } = await load();
  assert.ok(propertiesFor('chip', 'set').includes('checked'));
  assert.ok(propertiesFor('chipgroup', 'get').includes('checkedIndices'));
  assert.ok(propertiesFor('buttongroup', 'set').includes('selectedIndex'));
  assert.ok(propertiesFor('floatingactionbutton', 'set').includes('showText'));
  assert.deepEqual(await diagnose('javascript', 'forma.set("action", "iconName", "settings"); forma.bind("tags", "checkedIndices");', [{ name: 'action', kind: 'iconbutton' }, { name: 'tags', kind: 'chipgroup' }]), []);
});
test('command buttons offer menu and primary properties in scripting assistance', async () => {
  const { propertiesFor, diagnose } = await load();
  assert.ok(propertiesFor('dropdownbutton', 'get').includes('commandItems'));
  assert.ok(propertiesFor('splitbutton', 'set').includes('primaryEnabled'));
  assert.ok(propertiesFor('commandbutton', 'set').includes('description'));
  assert.deepEqual(await diagnose('javascript', 'forma.set("actions", "commandItems", []); forma.bind("split", "primaryEnabled");', [{ name: 'actions', kind: 'dropdownbutton' }, { name: 'split', kind: 'splitbutton' }]), []);
});
test('navigation and layout properties are offered to scripts', async () => {
  const { propertiesFor, diagnose } = await load();
  assert.ok(propertiesFor('breadcrumb', 'get').includes('selectedIndex'));
  assert.ok(propertiesFor('sidenavigation', 'set').includes('items'));
  assert.ok(propertiesFor('stackpanel', 'set').includes('gap'));
  assert.ok(propertiesFor('scrollablepanel', 'set').includes('scrollDirection'));
  assert.ok(propertiesFor('appshell', 'set').includes('breakpoint'));
  assert.ok(propertiesFor('accordion', 'set').includes('expanded'));
  assert.ok(propertiesFor('button', 'set').includes('dock'));
  assert.ok(!propertiesFor('toast', 'set').includes('dock'));
  assert.deepEqual(await diagnose('javascript', 'forma.set("stack", "orientation", "vertical"); forma.bind("nav", "selectedIndex");', [{ name: 'stack', kind: 'stackpanel' }, { name: 'nav', kind: 'sidenavigation' }]), []);
});
