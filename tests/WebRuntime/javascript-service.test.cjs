const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { pathToFileURL } = require('node:url');
const base = path.join(__dirname, '../../src/Forma.Builder');
const libsFolder = path.join(base, 'Frontend/node_modules/typescript/lib');
const libraries = Object.fromEntries(fs.readdirSync(libsFolder).filter(name => /^lib.*\.d\.ts$/.test(name)).map(name => [name, fs.readFileSync(path.join(libsFolder, name), 'utf8')]));
const load = () => import(pathToFileURL(path.join(base, 'Frontend/javascript-service.mjs')).href);
const context = {
  controls: [{ name: 'timer1', id: 'timer-id', kind: 'timer' }, { name: 'nameInput', kind: 'textbox' }, { name: 'pages', kind: 'pagination' }],
  globalSource: 'const count = forma.ref(0); forma.provide("timer", { count, enabled: forma.bind("timer1", "enabled"), increment() { count.value++; } });',
};
const query = (source, extra = {}) => ({ source, position: source.length, context, ...extra });

test('language service infers shared modules, live refs, computed values and nested custom JSON', async t => {
  const service = (await load()).createJavaScriptService(libraries); t.after(service.dispose);
  const labels = source => service.complete(query(source))?.options.map(option => option.label) ?? [];
  assert.deepEqual(labels('const timer = forma.use("timer"); timer.'), ['count', 'enabled', 'increment']);
  assert.deepEqual(labels('const timer = forma.use("timer"); timer.enabled.'), ['subscribe', 'value']);
  assert.ok(labels('const timer = forma.use("timer"); timer.count.value.').includes('toFixed'));
  assert.ok(labels('const name = forma.bind("nameInput", "value"); name.value.').includes('trim'));
  const shared = service.complete(query('forma.shared.user.', { context: { ...context, globalSource: 'forma.shared.user = forma.reactive({ name: "", age: 0 });' } }));
  assert.deepEqual(shared.options.map(option => option.label), ['age', 'name']);
  const result = service.complete(query('component.properties.user.', { custom: { user: { name: '', age: 0 } } }));
  assert.deepEqual(result.options.map(option => option.label), ['age', 'name']);
  const source = 'const count = forma.computed(() => 1); count.value';
  assert.match(service.hover(query(source)).signature, /FormaReadOnlyRef<number>/);
  assert.match(service.hover(query('const page = forma.bind("pages", "pageCount"); page.value')).signature, /FormaReadOnlyRef<number>/);
});

test('language service suggests JavaScript, DOM, scoped callback members and function signatures', async t => {
  const service = (await load()).createJavaScriptService(libraries); t.after(service.dispose);
  const labels = source => service.complete(query(source))?.options.map(option => option.label) ?? [];
  assert.ok(labels('const names = ["Angel"]; names.').includes('map'));
  assert.ok(labels('const names = ["Angel"]; names.map(name => name.').includes('toUpperCase'));
  assert.ok(labels('const el = forma.find("nameInput"); el.').includes('querySelector'));
  assert.ok(labels('forma.on("KeyDown", event => { event.').includes('key'));
  assert.ok(labels('Math.').includes('floor'));
  const request = query('const names = ["Angel"]; names.');
  const option = service.complete(request).options.find(option => option.label === 'map');
  assert.match(service.details({ ...request, entry: option.entry }).signature, /map/);
  const signature = service.signature(query('forma.set("timer1", "interval", '));
  assert.equal(signature.active, 2); assert.match(signature.parameters[2], /number/);
});

test('global source changes refresh module shapes and globals stay isolated from component locals', async t => {
  const service = (await load()).createJavaScriptService(libraries); t.after(service.dispose);
  const request = query('const timer = forma.use("timer"); timer.');
  service.complete(request);
  const next = service.complete({ ...request, context: { ...context, globalSource: 'forma.provide("timer", { reset() {} });' } });
  assert.deepEqual(next.options.map(option => option.label), ['reset']);
  assert.equal(service.complete(query('count.')), null);
  const global = service.complete(query('const user = forma.reactive({ name: "" }); user.', { globalScript: true }));
  assert.deepEqual(global.options.map(option => option.label), ['name']);
});

test('bundled worker performs offline inference without executing user scripts', () => {
  const replies = [], self = { postMessage: message => replies.push(message) };
  const sandbox = vm.createContext({ self, console, setTimeout, clearTimeout });
  vm.runInContext(fs.readFileSync(path.join(base, 'DesignerWeb/javascript-worker.bundle.js'), 'utf8'), sandbox);
  self.onmessage({ data: { id: 1, method: 'complete', request: query('throw new Error("must not execute"); const timer = forma.use("timer"); timer.') } });
  assert.equal(replies[0].error, undefined);
  assert.deepEqual(Array.from(replies[0].result.options, option => option.label), ['count', 'enabled', 'increment']);
});

test('imported provider types and nested project-file imports complete without executing code', async t => {
  const service = (await load()).createJavaScriptService(libraries); t.after(service.dispose);
  const projectFiles = [
    { path: 'providers/counter.js', content: '/** @param {FormaApi} forma */ export function createCounter(forma) { return { count: forma.ref(0), increment() {} }; }' },
    { path: 'providers/model.js', content: 'export const user = { name: "", age: 0 };' }
  ];
  const importedContext = { ...context, projectFiles, globalSource: 'import { createCounter } from "./providers/counter.js"; forma.provide("counter", createCounter(forma));' };
  const result = service.complete(query('forma.use("counter").', { context: importedContext }));
  assert.deepEqual(result.options.map(option => option.label), ['count', 'increment']);
  const ref = service.complete(query('forma.use("counter").count.', { context: importedContext }));
  assert.deepEqual(ref.options.map(option => option.label), ['subscribe', 'value']);
  const nested = service.complete(query('import { user } from "./model.js"; user.', { context: { ...importedContext, filePath: 'providers/consumer.js' } }));
  assert.deepEqual(nested.options.map(option => option.label), ['age', 'name']);
});
