// Optional rendered-layout smoke check: npm ci in src/Forma.Builder/Frontend, then run this file.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('../../src/Forma.Builder/Frontend/node_modules/playwright');
const root = path.join(__dirname, '../..');
(async () => {
  const browser = await chromium.launch({ channel: process.env.DOCS_BROWSER_CHANNEL || 'msedge' });
  try {
    const page = await browser.newPage({ viewport: { width: 1200, height: 900 } });
    const errors = []; page.on('pageerror', error => errors.push(error.message));
    await page.setContent('<!doctype html><html><head></head><body class="preview-mode"></body></html>');
    await page.addStyleTag({ content: fs.readFileSync(path.join(root, process.argv.includes('--core') ? 'src/Forma.WebView2/Web/css/forma.css' : 'src/Forma.Builder/DesignerWeb/designer.css'), 'utf8') });
    await page.evaluate(() => {
      window.chrome = { webview: { postMessage() {}, addEventListener() {} } };
      const Native = window.ResizeObserver; window.observers = [];
      window.ResizeObserver = class extends Native {
        constructor(callback) { super(callback); window.observers.push(this); }
        disconnect() { this.disconnected = true; super.disconnect(); }
      };
    });
    await page.addScriptTag({ content: fs.readFileSync(path.join(root, 'src/Forma.WebView2/Web/scripts/forma.js'), 'utf8') });
    await page.evaluate(() => {
      const create = (id, control, parentId, properties = {}) => window.forma.create({ id, control, parentId, properties });
      create('root', 'form');
      const root = document.getElementById('root'); Object.assign(root.style, { width: '1100px', height: '850px', position: 'relative' });
      create('outer', 'panel', 'root'); Object.assign(document.getElementById('outer').style, { width: '700px', height: '240px', position: 'relative' });
      create('grid', 'responsivegrid', 'outer', { breakpoint: 500, columns: 3, narrowColumns: 1, gap: 12 });
      Object.assign(document.getElementById('grid').style, { width: '100%', height: '240px' });
      for (let i = 0; i < 3; i++) {
        create('field' + i, 'textbox', 'grid', { x: 50, y: 30 });
        Object.assign(document.getElementById('field' + i).style, { width: '180px', height: '40px' });
      }
      document.getElementById('field0').value = 'Preserve me';
      create('ratio', 'aspectratiocontainer', 'grid', { aspectRatio: 2 });
      create('label', 'label', 'ratio', { text: 'Ratio content' });
      create('stack', 'responsivestack', 'root', { breakpoint: 500, orientation: 'horizontal', narrowOrientation: 'vertical', gap: 12 });
      Object.assign(document.getElementById('stack').style, { width: '600px', height: '160px' });
      for (let i = 0; i < 2; i++) { create('button' + i, 'button', 'stack'); Object.assign(document.getElementById('button' + i).style, { width: '140px', height: '36px' }); }
      create('panes', 'breakpointcontainer', 'root', { breakpoint: 500 });
      Object.assign(document.getElementById('panes').style, { width: '600px', height: '140px' });
      create('wide', 'textbox', 'panes', { layoutSlot: 1, x: 4, y: 4 });
      create('narrow', 'textbox', 'panes', { layoutSlot: 2, x: 8, y: 8 });
    });
    await page.waitForFunction(() => document.getElementById('grid').dataset.narrow === 'false' && document.getElementById('ratio').offsetHeight > 40);
    let layout = await page.evaluate(() => {
      const first = document.getElementById('field0').getBoundingClientRect(), second = document.getElementById('field1').getBoundingClientRect(), ratio = document.getElementById('ratio');
      return { width: first.width, top: first.top, secondTop: second.top, ratioWidth: ratio.offsetWidth, ratioHeight: ratio.offsetHeight };
    });
    assert.ok(layout.width > 210 && layout.width < 235); assert.equal(layout.top, layout.secondTop);
    assert.ok(Math.abs(layout.ratioHeight - layout.ratioWidth / 2) <= 1);
    await page.evaluate(() => { document.getElementById('outer').style.width = '499px'; document.getElementById('stack').style.width = '320px'; document.getElementById('wide').focus(); document.getElementById('panes').style.width = '320px'; });
    await page.waitForFunction(() => document.getElementById('grid').dataset.narrow === 'true' && document.getElementById('stack').dataset.narrow === 'true' && document.getElementById('panes').dataset.narrow === 'true');
    const narrow = await page.evaluate(() => {
      const first = document.getElementById('field0'), second = document.getElementById('field1'), ratio = document.getElementById('ratio');
      return { width: first.offsetWidth, value: first.value, nextRow: second.getBoundingClientRect().top > first.getBoundingClientRect().top,
        ratioWidth: ratio.offsetWidth, ratioHeight: ratio.offsetHeight, wideHidden: document.getElementById('wide').hidden,
        narrowHidden: document.getElementById('narrow').hidden, focus: document.activeElement.id,
        direction: document.querySelector('#stack > .layout-content').style.flexDirection,
        header: getComputedStyle(document.querySelector('#panes > .layout-header')).display };
    });
    assert.ok(narrow.width > 450); assert.equal(narrow.nextRow, true); assert.equal(narrow.value, 'Preserve me');
    assert.ok(Math.abs(narrow.ratioHeight - narrow.ratioWidth / 2) <= 1);
    assert.equal(narrow.direction, 'column'); assert.equal(narrow.wideHidden, true); assert.equal(narrow.narrowHidden, false);
    assert.equal(narrow.focus, 'panes'); assert.equal(narrow.header, 'none');
    await page.evaluate(() => { document.getElementById('outer').style.width = '700px'; document.getElementById('grid').style.width = '500px'; });
    await page.waitForFunction(() => document.getElementById('grid').dataset.narrow === 'false');
    await page.evaluate(() => window.forma.remove({ id: 'root' }));
    assert.equal(await page.evaluate(() => window.observers.every(observer => observer.disconnected)), true);
    assert.deepEqual(errors, []);
    console.log('Rendered responsive layouts passed: nested width, grid tracks, stack direction, pane focus/state, aspect ratio, and observer disposal.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
