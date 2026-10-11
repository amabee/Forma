const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('../../src/Forma.Builder/Frontend/node_modules/playwright');
const source = file => fs.readFileSync(path.join(__dirname, '../../src/Forma.Builder/DesignerWeb', file), 'utf8');
(async () => {
  const browser = await chromium.launch({ channel: 'msedge' });
  try {
    const page = await browser.newPage();
    const errors = []; page.on('pageerror', error => errors.push(error.message));
    await page.setContent('<div id="form"><button id="button"><span>Click</span></button><input id="name" value=""></div>');
    await page.evaluate(() => { window.log = []; window.messages = []; window.forma = { send: message => window.messages.push(message) }; });
    for (const file of ['reactivity.js', 'module-runtime.bundle.js', 'component-customization.js']) await page.addScriptTag({ content: source(file) });
    await page.evaluate(() => {
      window.state = { id: 'form', globalScript: `
        for (const type of ['Click', 'Input', 'KeyDown', 'PropertyChanged']) forma.root.on(type, event => {
          window.log.push(event.toJSON());
          if (type === 'Input') window.liveValue = event.source.get('value');
        });
      `, controls: [
        { id: 'form', name: 'Form1', kind: 'form', text: 'Demo', enabled: true, visible: true },
        { id: 'button', name: 'button1', parentId: 'form', kind: 'button', enabled: true, visible: true, customization: { behavior: 'forma.on("Click", event => { event.handled = true; });' } },
        { id: 'name', name: 'nameInput', parentId: 'form', kind: 'textbox', text: '', enabled: true, visible: true },
      ] };
      window.formaCustomization.apply(window.state, true);
    });
    await page.locator('#button span').click();
    const click = await page.evaluate(() => window.log.find(event => event.eventName === 'Click'));
    assert.equal(click.origin, 'user'); assert.equal(click.isTrusted, true); assert.equal(click.handled, true);
    assert.equal(click.source.id, 'button'); assert.equal(click.subscriber.id, 'form'); assert.equal(click.originalEvent, undefined);
    await page.locator('#name').fill('Angel');
    await page.waitForFunction(() => window.log.some(event => event.eventName === 'PropertyChanged' && event.value === 'Angel'));
    const input = await page.evaluate(() => window.log.find(event => event.eventName === 'Input'));
    assert.equal(input.property, 'value'); assert.equal(input.value, 'Angel'); assert.equal(input.previousValue, ''); assert.equal(input.origin, 'user');
    assert.equal(await page.evaluate(() => window.liveValue), 'Angel');
    await page.keyboard.press('Enter');
    const key = await page.evaluate(() => window.log.find(event => event.eventName === 'KeyDown'));
    assert.equal(key.keyboard.key, 'Enter');
    await page.evaluate(() => { window.state.controls[2].text = 'Angel'; window.formaCustomization.apply(window.state, true); });
    assert.equal(await page.evaluate(() => window.log.filter(event => event.eventName === 'PropertyChanged' && event.id === 'name').length), 1);
    await page.evaluate(() => window.formaCustomization.clear());
    const count = await page.evaluate(() => window.log.length); await page.locator('#button').click();
    assert.equal(await page.evaluate(() => window.log.length), count);
    assert.deepEqual(errors, []);
    console.log('Forma event payloads passed with real mouse and keyboard input in Edge.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
