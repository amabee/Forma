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
