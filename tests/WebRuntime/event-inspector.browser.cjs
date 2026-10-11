const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('../../docs-site/node_modules/playwright');
const root = path.join(__dirname, '../..');
const read = file => fs.readFileSync(path.join(root, 'src/Forma.Builder/DesignerWeb', file), 'utf8');
(async () => {
  const browser = await chromium.launch({ channel: 'msedge' });
  try {
    const page = await browser.newPage();
    const errors = []; page.on('pageerror', error => errors.push(error.message));
    await page.setContent(read('index.html').replace(/<script[^>]*>[\s\S]*?<\/script>/g, ''));
    await page.addStyleTag({ content: read('designer.css') });
    await page.evaluate(() => {
      window.messages = [];
      window.forma = { send: message => window.messages.push(message) };
      window.formaDesigner = {
        selectedId: 'button', preview: false,
        state: { controls: [{ id: 'button', name: 'button1', kind: 'button', customization: { behavior: 'forma.on("click", () => {});' } }, { id: 'timer', name: 'timer1', kind: 'timer', component: true }] },
        select(id) { this.selectedId = id; }, receive(message) { if (message.action === 'state') this.state = message; },
      };
    });
    await page.addScriptTag({ content: read('event-inspector.bundle.js') });
    await page.locator('#inspector-events-tab').click();
    assert.equal(await page.locator('#property-editors').isVisible(), false);
    await page.locator('#event-search').fill('DoubleClick');
    await page.locator('.event-handler').click();
    assert.deepEqual(await page.evaluate(() => window.messages.at(-1)), { type: 'designer', event: 'command', id: 'button', payload: { command: 'edit-event', eventName: 'DoubleClick' } });
    await page.locator('#event-search').fill('click');
    assert.ok((await page.locator('.event-handler').first().innerText()).includes('Inline handler'));
    await page.locator('#event-search').fill('');
    await page.evaluate(() => window.formaDesigner.select('timer'));
    assert.equal(await page.getByRole('button', { name: 'Click', exact: true }).count(), 0);
    await page.getByRole('button', { name: 'Tick + Add handler', exact: true }).click();
    assert.equal(await page.evaluate(() => window.messages.at(-1).id), 'timer');
    await page.evaluate(() => { window.formaDesigner.state.controls[1].locked = true; window.formaDesigner.select('timer'); });
    assert.equal(await page.locator('.event-handler:not(:disabled)').count(), 0);
    await page.evaluate(() => window.formaDesigner.select('missing'));
    assert.equal(await page.locator('.event-handler').count(), 0);
    await page.locator('#inspector-properties-tab').click();
    assert.equal(await page.locator('#events-inspector').isVisible(), false);

    await page.setContent(read('code-editor.html').replace(/<script[^>]*>[\s\S]*?<\/script>/g, ''));
    await page.addStyleTag({ content: read('code-editor.css') });
    await page.evaluate(() => {
      window.chrome = { webview: { postMessage() {}, addEventListener(type, listener) { window.deliver = message => listener({ data: message }); } } };
    });
    await page.addScriptTag({ content: read('code-editor.bundle.js') });
    await page.evaluate(() => window.deliver({ action: 'source', document: 'behavior', name: 'button1', source: { behavior: 'const preserved = 42;', css: ':host { color: red; }', characteristics: '{}' } }));
    await page.locator('#editor-behavior .cm-content').click();
    await page.keyboard.press('Control+End');
    await page.keyboard.type('\nconst unsaved = 7;');
    await page.evaluate(() => window.deliver({ action: 'event-handler', eventName: 'Click', componentName: 'button1' }));
    const script = await page.locator('#editor-behavior .cm-content').innerText();
    assert.ok(script.includes('const preserved = 42;')); assert.ok(script.includes('const unsaved = 7;'));
    assert.ok(script.includes('import { ButtonEventHandler } from "./event.js"'));
    assert.ok(script.includes('forma.on("Click", ButtonEventHandler.button1_Click)'));
    assert.ok(!script.includes('static button1_Click'));
    const eventSource = await page.locator('#editor-events .cm-content').innerText();
    assert.ok(eventSource.includes('static button1_Click(event)'));
    assert.equal(await page.locator('#editor-dirty').innerText(), 'Unsaved changes');
    await page.evaluate(() => window.deliver({ action: 'event-handler', eventName: 'click', componentName: 'button1' }));
    assert.equal(await page.locator('#editor-behavior .cm-content').innerText(), script);
    assert.equal(await page.locator('#editor-events .cm-content').innerText(), eventSource);
    await page.evaluate(() => window.deliver({ action: 'activate', document: 'css' }));
    assert.equal(await page.locator('#editor-css .cm-content').innerText(), ':host { color: red; }');
    await page.evaluate(() => window.deliver({ action: 'source', document: 'behavior', name: 'button1', source: { behavior: 'const broken = ;', css: '', characteristics: '{}' } }));
    await page.evaluate(() => window.deliver({ action: 'event-handler', eventName: 'Click', componentName: 'button1' }));
    assert.equal(await page.locator('#editor-behavior .cm-content').innerText(), 'const broken = ;');
    assert.ok((await page.locator('#editor-message').innerText()).startsWith('Fix the source'));
    assert.deepEqual(errors, []);
    console.log('Events inspector and editor navigation passed in Edge.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
