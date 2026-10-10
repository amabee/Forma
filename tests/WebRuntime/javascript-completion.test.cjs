const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const load = () => import(pathToFileURL(path.join(__dirname, '../../src/Forma.Builder/Frontend/javascript-completion.mjs')).href);

test('worker completion requests are correlated, cancelled on disposal and restarted for active tabs', async t => {
  const originalWorker = global.Worker, originalWindow = global.window;
  t.after(() => { global.Worker = originalWorker; global.window = originalWindow; });
  const workers = [];
  global.window = { location: { href: 'https://forma-editor.local/DesignerWeb/code-editor.html' } };
  global.Worker = class {
    constructor(url) { this.url = url.href; this.messages = []; workers.push(this); }
    postMessage(message) { this.messages.push(message); }
    terminate() { this.terminated = true; }
  };
  const client = (await load()).createJavaScriptCompletionClient(); t.after(client.dispose);
  const context = { pos: 5, state: { doc: { toString: () => 'Math.' } }, aborted: false };
  const first = client.complete(context, {});
  assert.match(workers[0].url, /javascript-worker.bundle.js$/);
  const message = workers[0].messages[0];
  workers[0].onmessage({ data: { id: message.id, result: { from: 5, options: [{ label: 'floor', entry: { name: 'floor' } }] } } });
  assert.equal((await first).options[0].label, 'floor');
  const pending = client.complete(context, {});
  client.dispose(); assert.equal(await pending, null); assert.equal(workers[0].terminated, true);
  const next = client.complete(context, {}); assert.equal(workers.length, 2);
  workers[1].onerror(); assert.equal(await next, null);
  assert.equal(await client.complete(context, {}), null);
});

test('aborted completion contexts discard worker results', async t => {
  const originalWorker = global.Worker, originalWindow = global.window;
  t.after(() => { global.Worker = originalWorker; global.window = originalWindow; });
  global.window = { location: { href: 'https://forma-editor.local/DesignerWeb/code-editor.html' } };
  global.Worker = class {
    postMessage(message) { queueMicrotask(() => this.onmessage({ data: { id: message.id, result: { from: 0, options: [] } } })); }
    terminate() {}
  };
  const client = (await load()).createJavaScriptCompletionClient(); t.after(client.dispose);
  assert.equal(await client.complete({ pos: 0, state: { doc: { toString: () => '' } }, aborted: true }, {}), null);
});
