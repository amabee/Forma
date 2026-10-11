const { test } = require('node:test');
const assert = require('node:assert/strict');
const { pathToFileURL } = require('node:url');
const path = require('node:path');
const load = () => import(pathToFileURL(path.join(__dirname, '../../src/Forma.Builder/Frontend/event-handlers.mjs')).href);

test('event navigation recognizes aliases and named or inline handlers without duplicates', async () => {
  const { prepareHandler, inspectHandlers } = await load();
  const source = 'const handler = (event) => { console.log(event); };\napi.on("dblclick", handler);\nforma.on("click", () => { alert("hi"); });';
  const result = prepareHandler(source, 'DoubleClick', 'button1');
  assert.equal(result.created, false); assert.equal(result.source, source);
  assert.equal(result.from, source.indexOf('{') + 1);
  assert.equal(prepareHandler(source, 'Click', 'button1').created, false);
  assert.equal(inspectHandlers(source).handlers[0].name, 'handler');
});

test('scaffolding preserves the entire draft, ignores comments and strings, and avoids name collisions', async () => {
  const { prepareHandler } = await load();
  const source = 'import data from "./helper.js";\nconst ButtonEventHandler = 1;\n// forma.on("Click", fake);\nconst example = \'forma.on("Click", fake)\';';
  const events = 'export class ButtonEventHandler { static button1_Click(event) {} }';
  const result = prepareHandler(source, 'Click', 'button1', events);
  assert.ok(result.created); assert.ok(result.source.includes(source));
  assert.ok(result.source.includes('ButtonEventHandler as ButtonEventHandler2'));
  assert.ok(result.events.includes('static button1_Click2(event)'));
  assert.ok(result.events.slice(result.from).startsWith('// Add'));
  const reopened = prepareHandler(result.source, 'click', 'button1', result.events);
  assert.equal(reopened.created, false); assert.equal(reopened.document, 'events');
  assert.equal(prepareHandler(result.source, 'Change', 'button1', result.events).source.match(/import \{ ButtonEventHandler/g).length, 1);
});

test('syntax errors keep the draft untouched and unsafe component names produce valid JavaScript', async () => {
  const { prepareHandler, inspectHandlers } = await load();
  assert.ok(prepareHandler('const broken = ;', 'Click', 'button1').error);
  const result = prepareHandler('', 'row-selection', '3 bad-name');
  assert.equal(inspectHandlers(result.source).error, null);
  assert.equal(inspectHandlers(result.source).handlers[0].event, 'row-selection');
});

test('nonvisual components offer lifecycle and their own events, not mouse events', async () => {
  const { eventsFor } = await load();
  const timer = eventsFor({ kind: 'timer', component: true });
  assert.deepEqual(timer.Component, ['Tick']); assert.equal(timer.Mouse, undefined);
  assert.ok(eventsFor({ kind: 'button' }).Mouse.includes('Click'));
  assert.deepEqual(eventsFor({ kind: 'datagridview' }).Component, ['row-selection']);
});
