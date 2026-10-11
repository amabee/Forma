const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../../src/Forma.Builder');
const load = () => import(pathToFileURL(path.join(base, 'Frontend/module-runtime.mjs')).href);

test('nested named/default/re-export imports and JSON share one instance per session', async () => {
  const { createModuleSession } = await load();
  const files = [
    { path: 'providers/count.js', content: 'import data from "../data.json"; forma.runs++; export const state = { count: data.start }; export default state;' },
    { path: 'providers/index.js', content: 'export { default, state } from "./count.js";' },
    { path: 'data.json', content: '{"start": 3}' },
  ];
  const api = { runs: 0 };
  const session = createModuleSession(files);
  session.execute('import state, { state as same } from "./providers"; forma.same = state === same; state.count++; forma.state = state;', api, {});
  session.execute('import { state } from "./providers/count.js"; forma.count = state.count;', api, {});
  assert.equal(api.runs, 1); assert.equal(api.same, true); assert.equal(api.count, 4);
  createModuleSession(files).execute('import state from "./providers"; forma.count = state.count;', api, {});
  assert.equal(api.runs, 2); assert.equal(api.count, 3);
  assert.throws(() => session.execute('import "https://example.com/x.js";', api, {}), /relative project/);
  assert.throws(() => session.execute('import "../outside.js";', api, {}), /escapes/);
  assert.throws(() => session.execute('import "./missing.js";', api, {}), /Cannot find/);
});

test('imported providers initialize before components, survive refresh and reset with Preview', t => {
  const { window } = new JSDOM('<div id="form"></div><button id="button"></button>', { runScripts: 'outside-only' });
  t.after(() => window.close());
  const messages = []; window.forma = { send: message => messages.push(message) };
  for (const file of ['reactivity.js', 'module-runtime.bundle.js', 'component-customization.js']) window.eval(fs.readFileSync(path.join(base, 'DesignerWeb', file), 'utf8'));
  const state = {
    id: 'form', globalScript: 'import { createCounter } from "./providers/counter.js"; forma.provide("counter", createCounter(forma));',
    projectFiles: [{ path: 'providers/counter.js', content: 'export function createCounter(forma) { return { count: forma.ref(0) }; }' }],
    controls: [
      { id: 'form', name: 'Form1', kind: 'form', enabled: true, visible: true },
      { id: 'button', name: 'button1', kind: 'button', enabled: true, visible: true, customization: { behavior: 'const counter = forma.use("counter"); window.counter = counter; forma.on("click", () => counter.count.value++);' } }
    ]
  };
  window.formaCustomization.apply(state, true);
  window.document.getElementById('button').click();
  const original = window.counter; assert.equal(original.count.value, 1);
  window.formaCustomization.apply(state, true); assert.equal(window.counter, original);
  window.formaCustomization.clear(); window.formaCustomization.apply(state, true);
  assert.notEqual(window.counter, original); assert.equal(window.counter.count.value, 0);
  state.projectFiles[0].content = 'throw new Error("provider broke");';
  window.formaCustomization.apply(state, true);
  assert.ok(messages.some(message => message.event === 'error' && message.payload.message.includes('provider broke')));
  window.document.getElementById('button').click(); assert.equal(window.counter.count.value, 0);
});

test('component event modules use the owning API while imported providers stay shared', async () => {
  const { createModuleSession } = await load();
  const session = createModuleSession([{ path: 'shared.js', content: 'forma.runs++; export const state = { count: 0 };' }]);
  const globalApi = { runs: 0 }; session.execute('import "./shared.js";', globalApi, {});
  const script = 'import { ButtonEventHandler } from "./event.js"; forma.on("click", ButtonEventHandler.click);';
  const events = 'import { state } from "./shared.js"; export class ButtonEventHandler { static click(event) { state.count++; forma.result = [component.id, state.count, event.detail]; } }';
  const first = { on(name, callback) { this.callback = callback; } }, second = { on(name, callback) { this.callback = callback; } };
  for (const [api, id] of [[first, 'first'], [second, 'second']]) session.execute(script, api, { id }, 'script.js', [{ path: 'event.js', content: events }]);
  first.callback({ detail: 1 }); second.callback({ detail: 2 }); first.callback({ detail: 3 });
  assert.deepEqual(first.result, ['first', 3, 3]); assert.deepEqual(second.result, ['second', 2, 2]);
  assert.equal(globalApi.runs, 1); assert.equal(globalApi.result, undefined);
});

test('editing event.js restarts only that component and Preview cleanup removes its handlers', t => {
  const { window } = new JSDOM('<div id="form"></div><button id="first"></button><button id="second"></button>', { runScripts: 'outside-only' });
  t.after(() => window.close());
  window.forma = { send() {} }; window.calls = [];
  for (const file of ['reactivity.js', 'module-runtime.bundle.js', 'component-customization.js']) window.eval(fs.readFileSync(path.join(base, 'DesignerWeb', file), 'utf8'));
  const script = 'import { ButtonEventHandler } from "./event.js"; forma.on("Click", ButtonEventHandler.click);';
  const state = { id: 'form', controls: [{ id: 'form', kind: 'form' }, ...['first', 'second'].map(id => ({ id, name: id, kind: 'button', enabled: true, visible: true, customization: { behavior: script, events: 'export class ButtonEventHandler { static click() { window.calls.push(component.id); } }' } }))] };
  window.formaCustomization.apply(state, true);
  window.document.getElementById('first').click(); window.document.getElementById('second').click();
  assert.deepEqual([...window.calls], ['first', 'second']);
  state.controls[1].customization.events = 'export class ButtonEventHandler { static click() { window.calls.push("edited:" + component.id); } }';
  window.formaCustomization.apply(state, true);
  window.document.getElementById('first').click(); window.document.getElementById('second').click();
  assert.deepEqual([...window.calls], ['first', 'second', 'edited:first', 'second']);
  window.formaCustomization.clear(); window.document.getElementById('first').click();
  assert.equal(window.calls.length, 4);
});
