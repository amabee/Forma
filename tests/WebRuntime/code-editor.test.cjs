const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../../src/Forma.Builder');
const loadFormat = () => import(pathToFileURL(path.join(base, 'Frontend/code-format.mjs')).href);

test('formatter supports CSS, JavaScript and JSON, is repeatable and rejects malformed source', async () => {
  const { formatCode } = await loadFormat();
  for (const [language, source] of [['css', ':host{color:red}'], ['javascript', 'api.on("click",()=>{const x=1;});'], ['json', '{"name":"Forma","count":1}']]) {
    const result = await formatCode(language, source);
    assert.ok(result.includes('\n')); assert.equal(await formatCode(language, result), result);
  }
  await assert.rejects(formatCode('json', '{oops'));
  await assert.rejects(formatCode('javascript', 'const ='));
});

function fixture(t) {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'DesignerWeb/code-editor.html'), 'utf8'), { runScripts: 'outside-only', pretendToBeVisual: true });
  const { window } = dom; const messages = [];
  window.chrome = { webview: { postMessage: message => messages.push(message), addEventListener() {} } };
  window.Range.prototype.getClientRects = () => [];
  window.Range.prototype.getBoundingClientRect = () => ({ left: 0, top: 0, right: 0, bottom: 0, width: 0, height: 0 });
  window.eval(fs.readFileSync(path.join(base, 'DesignerWeb/code-editor.bundle.js'), 'utf8'));
  t.after(() => { window.dispatchEvent(new window.Event('pagehide')); window.close(); });
  return { window, editor: window.formaCodeEditor, messages };
}

test('editor has line numbers, syntax colors, tabs, formats and saves all sources to the host', async t => {
  const { window, editor, messages } = fixture(t);
  const source = { css: ':host{color:red}', behavior: 'api.on("click",()=>{});', characteristics: '{"count":1}' };
  editor.receive({ action: 'source', source });
  assert.equal(messages[0].event, 'ready');
  assert.ok(window.document.querySelector('.cm-lineNumbers'));
  editor.activate('behavior');
  assert.equal(window.document.getElementById('editor-css').hidden, true);
  assert.equal(window.document.querySelector('[data-tab="behavior"]').getAttribute('aria-selected'), 'true');
  assert.ok(window.document.querySelector('#editor-behavior .cm-line span[class]'));
  await editor.command('format');
  assert.match(editor.source().behavior, /\(\) =>/);
  await editor.command('save');
  const saved = messages.at(-1); assert.equal(saved.event, 'save');
  assert.match(saved.source.css, /color: red;/);
  assert.equal(window.document.querySelector('[data-command="save"]').disabled, true);
  // JSON property order from the host does not affect dirty detection.
  editor.receive({ action: 'saved', source: { characteristics: saved.source.characteristics, behavior: saved.source.behavior, css: saved.source.css } });
  assert.equal(window.document.getElementById('editor-dirty').textContent, 'Saved');
  assert.equal(window.document.querySelector('[data-command="save"]').disabled, false);
});

test('invalid format does not partly change sources and a host error permits retry', async t => {
  const { window, editor, messages } = fixture(t);
  const source = { css: ':host{color:red}', behavior: 'const =', characteristics: '{}' };
  editor.receive({ action: 'source', source });
  await editor.command('save');
  assert.equal(JSON.stringify(editor.source()), JSON.stringify(source));
  assert.equal(messages.some(m => m.event === 'save'), false);
  assert.match(window.document.getElementById('editor-message').textContent, /Format failed/);
  window.document.getElementById('format-on-save').checked = false;
  await editor.command('save');
  assert.equal(messages.at(-1).event, 'save');
  editor.receive({ action: 'error', status: 'Save failed' });
  assert.equal(window.document.querySelector('[data-command="save"]').disabled, false);
  editor.views.get('css').dispatch({ changes: { from: 0, insert: '/* edited */' } });
  editor.receive({ action: 'saved', source });
  assert.equal(window.document.getElementById('editor-dirty').textContent, 'Unsaved changes');
});

test('selection has a contrasting color and remains visible under the active line', t => {
  const { window, editor } = fixture(t);
  editor.receive({ action: 'source', source: { css: ':host { color: red; }', behavior: '', characteristics: '{}' } });
  const view = editor.views.get('css');
  view.dispatch({ selection: { anchor: 0, head: 5 } }); view.focus();
  assert.equal(view.state.selection.main.empty, false);
  const line = view.dom.querySelector('.cm-activeLine');
  assert.equal(window.getComputedStyle(line).backgroundColor, 'rgba(148, 163, 184, 0.08)');
  // jsdom has no text geometry; probe the same selection layer's theme rule.
  const box = window.document.createElement('div'); box.className = 'cm-selectionBackground';
  view.dom.querySelector('.cm-selectionLayer').appendChild(box);
  assert.equal(window.getComputedStyle(box).backgroundColor, 'rgb(37, 99, 235)');
  assert.equal(window.document.querySelectorAll('.editor-footer').length, 1);
});
