const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');
test('rich formatting toggles selected runs and preserves selection without inserting HTML', () => {
  const { window, document, messages, create } = fixture();
  create('rich', 'richtextbox', 'root', { document: [{ kind: 'paragraph', runs: [{ text: '<script>text</script>', bold: false, italic: false, underline: false }] }] });
  const rich = document.getElementById('rich'), editor = rich.querySelector('.rich-content');
  assert.equal(editor.querySelector('script'), null);
  window.formaDesigner.preview = true;
  const range = document.createRange(); range.selectNodeContents(editor.querySelector('span'));
  window.getSelection().removeAllRanges(); window.getSelection().addRange(range);
  rich.querySelector('[data-rich-format="underline"]').click();
  assert.equal(messages.at(-1).payload.document[0].runs[0].underline, true);
  assert.equal(window.getSelection().toString(), '<script>text</script>');
  rich.querySelector('[data-rich-format="underline"]').click();
  assert.equal(messages.at(-1).payload.document[0].runs[0].underline, false);
  assert.equal(editor.querySelector('span').style.textDecoration, 'none');
});
test('PictureBox renders images and rich editing can enter design mode explicitly', () => {
  const { window, document, create } = fixture();
  create('picture', 'picturebox', 'root', { source: 'data:image/png;base64,AQID', sizeMode: 'cover' });
  assert.equal(document.getElementById('picture').style.objectFit, 'cover');
  create('rich', 'richtextbox', 'root', { document: [] });
  const d = window.formaDesigner;
  d.state = { controls: [{ id: 'rich', kind: 'richtextbox', width: 200, height: 100, enabled: true, visible: true, locked: false }], propertySchema: [] }; d.selectedId = 'rich';
  d.applyAppearance(d.state.controls[0]); d.inspector();
  assert.equal(document.getElementById('rich').querySelector('.rich-content').contentEditable, 'false');
  document.querySelector('[data-command="edit-rich"]').click();
  assert.equal(document.getElementById('rich').querySelector('.rich-content').contentEditable, 'true');
});
test('masked input formats characters and preserves the editing position', () => {
  const { window, document, messages, create } = fixture();
  create('masked', 'maskedtextbox', 'root', { mask: '000-0000', text: '' });
  const input = document.getElementById('masked');
  input.value = '1234567'; input.setSelectionRange(3, 3);
  input.dispatchEvent(new window.Event('input', { bubbles: true }));
  assert.equal(input.value, '123-4567'); assert.equal(input.selectionStart, 4);
  assert.equal(messages.at(-1).payload.text, '123-4567');
});
test('checked-list updates reuse focused rows and emit item indices', () => {
  const { window, document, messages, create } = fixture();
  create('list', 'checkedlistbox', 'root', { items: ['A', 'B'], checkedIndices: [0] });
  const list = document.getElementById('list'), second = list.querySelectorAll('input')[1];
  assert.equal(list.querySelector('input').checked, true);
  second.checked = true; second.dispatchEvent(new window.Event('change', { bubbles: true }));
  assert.equal(messages.at(-1).payload.index, 1);
  window.forma.update({ id: 'list', properties: { items: ['A', 'B'], checkedIndices: [0, 1] } });
  assert.equal(list.querySelectorAll('input')[1], second); assert.equal(second.checked, true);
});
test('links route preview clicks through the host and respect disabled state', () => {
  const { window, document, messages, create } = fixture();
  create('link', 'linklabel', 'root', { text: 'Website', url: 'https://example.org/' });
  const link = document.getElementById('link'); window.formaDesigner.preview = true;
  link.click(); assert.equal(messages.at(-1).event, 'link');
  const count = messages.length; link.setAttribute('aria-disabled', 'true'); link.click();
  assert.equal(messages.length, count);
});
test('Undo/Redo shortcuts target the design while native field undo remains available', () => {
  const { window, document, messages } = fixture();
  for (const [key, shiftKey, command] of [['z', false, 'undo'], ['z', true, 'redo'], ['y', false, 'redo']]) {
    document.body.dispatchEvent(new window.KeyboardEvent('keydown', { key, ctrlKey: true, shiftKey, bubbles: true, cancelable: true }));
    assert.equal(messages.at(-1).payload.command, command);
  }
  const count = messages.length;
  document.getElementById('search').dispatchEvent(new window.KeyboardEvent('keydown', { key: 'z', ctrlKey: true, bubbles: true }));
  assert.equal(messages.length, count);
  const d = window.formaDesigner;
  d.receive({ action: 'state', controls: [], propertySchema: [], canUndo: true, canRedo: false });
  assert.equal(document.querySelector('[data-command="undo"]').disabled, false);
  assert.equal(document.querySelector('[data-command="redo"]').disabled, true);
});
test('Save/Open menus and shortcuts remain available while editing properties', () => {
  const { window, document, messages } = fixture();
  assert.equal(document.querySelector('[data-command="save"]').disabled, false);
  assert.equal(document.querySelector('[data-command="open"]').disabled, false);
  const input = document.getElementById('search');
  for (const [key, shiftKey, command] of [['s', false, 'save'], ['s', true, 'save-as'], ['o', false, 'open']]) {
    const event = new window.KeyboardEvent('keydown', { key, ctrlKey: true, shiftKey, bubbles: true, cancelable: true });
    input.dispatchEvent(event); assert.equal(event.defaultPrevented, true);
    assert.equal(messages.at(-1).payload.command, command);
  }
});
test('practical inputs render native values and emit typed input events', () => {
  const { window, document, create, messages } = fixture();
  create('slider', 'slider', 'root', { minimum: 10, maximum: 30, increment: 2, number: 20 });
  const slider = document.getElementById('slider');
  assert.equal(slider.min, '10'); assert.equal(slider.max, '30'); assert.equal(slider.step, '2');
  slider.value = '24'; slider.dispatchEvent(new window.Event('input', { bubbles: true }));
  assert.equal(messages.at(-1).payload.value, 24);
  create('date', 'datepicker', 'root', { dateValue: '2026-10-03' });
  assert.equal(document.getElementById('date').value, '2026-10-03');
  create('password', 'passwordbox', 'root', { text: 'secret' });
  assert.equal(document.getElementById('password').type, 'password');
  create('progress', 'progressbar', 'root', { minimum: 10, maximum: 30, number: 20 });
  assert.equal(document.getElementById('progress').value, 10);
  assert.equal(document.getElementById('progress').max, 20);
});
test('toggle buttons carry checked state and circular progress exposes its value', () => {
  const { document, create, window, messages } = fixture();
  create('toggle', 'togglebutton', 'root', { checked: true });
  const toggle = document.getElementById('toggle'); window.formaDesigner.preview = true;
  toggle.click(); assert.equal(messages.at(-1).payload.checked, false);
  create('circle', 'circularprogress', 'root', { minimum: 0, maximum: 200, number: 50 });
  assert.equal(document.getElementById('circle').textContent, '25%');
  assert.equal(document.getElementById('circle').getAttribute('aria-valuenow'), '50');
});
function fixture() {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/index.html'), 'utf8'), { runScripts: 'outside-only' });
  const messages = []; const window = dom.window;
  window.chrome = { webview: { postMessage: m => messages.push(m), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js']) window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
  const create = (id, control, parentId, properties = {}) => window.forma.receive({ type: 'create', id, control, parentId, properties });
  create('root', 'form'); window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  return { window, document: window.document, messages, create };
}
test('every toolbox kind creates a real DOM control and all icons exist locally', () => {
  const { document, create } = fixture();
  for (const tool of document.querySelectorAll('[data-kind]')) {
    assert.equal(tool.disabled, false);
    create(tool.dataset.kind, tool.dataset.kind, 'root', { text: 'Example' });
    assert.ok(document.getElementById(tool.dataset.kind));
  }
  for (const icon of document.querySelectorAll('img.icon-svg')) assert.ok(fs.existsSync(path.join(base, 'src/Forma.Builder/DesignerWeb', icon.getAttribute('src'))));
});
test('managed layouts keep children in flow and tabs show the assigned page', () => {
  const { document, create, window } = fixture();
  create('flow', 'flowlayoutpanel', 'root', { gap: 12 });
  create('button', 'button', 'flow', { x: 30, y: 40 });
  assert.equal(document.getElementById('button').style.position, 'relative');
  assert.equal(document.getElementById('button').parentElement.className, 'layout-content');
  create('tabs', 'tabcontrol', 'root', { tabs: ['One', 'Two'], selectedTab: 0 });
  create('second', 'label', 'tabs', { layoutSlot: 2 });
  assert.equal(document.getElementById('second').hidden, true);
  window.forma.update({ id: 'tabs', properties: { tabs: ['One', 'Two'], selectedTab: 1 } });
  assert.equal(document.getElementById('second').hidden, false);
});
test('native checkbox and selection events carry values to C#', () => {
  const { document, create, window, messages } = fixture();
  create('check', 'checkbox', 'root', { checked: false });
  const input = document.getElementById('check').querySelector('input'); input.checked = true;
  input.dispatchEvent(new window.Event('change', { bubbles: true }));
  assert.equal(messages.at(-1).payload.checked, true);
  create('combo', 'combobox', 'root', { items: ['A', 'B'], selectedIndex: 0 });
  const combo = document.getElementById('combo'); combo.selectedIndex = 1;
  combo.dispatchEvent(new window.Event('change', { bubbles: true }));
  assert.equal(messages.at(-1).payload.selectedIndex, 1);
});
test('nonvisual components move into the tray and New clears old tray items', () => {
  const { document, create, window } = fixture();
  create('timer', 'timer', 'root');
  window.formaDesigner.applyAppearance({ id: 'timer', component: true, name: 'timer1' });
  assert.equal(document.getElementById('timer').parentElement.id, 'component-tray');
  assert.equal(document.getElementById('timer').hidden, false);
  window.formaDesigner.receive({ action: 'initialize', id: 'root' });
  assert.equal(document.getElementById('component-tray').children.length, 0);
});

test('switches retain native checkboxes with a separate thumb track and respect locking', () => {
  const { window, document, create } = fixture(); create('image', 'image', 'root');
  const d = window.formaDesigner; d.selectedId = 'image';
  d.state = { controls: [{ id: 'image', kind: 'image', locked: false, enabled: true }], propertySchema: [
    { id: 'enabled', label: 'Enabled', category: 'Behavior', editor: 'checkbox' }
  ] };
  d.inspector();
  const input = document.getElementById('prop-enabled');
  assert.equal(input.checked, true);
  assert.ok(input.nextElementSibling.classList.contains('switch-track'));
  input.click(); assert.equal(input.checked, false);
  const picker = document.querySelector('[data-command="choose-image"]'); assert.ok(picker);
  d.state.controls[0].locked = true; d.inspector();
  assert.equal(input.disabled, true); assert.equal(picker.disabled, true);
});

test('snapping picks nearby alignment and spacing while respecting threshold and parent bounds', () => {
  const { window } = fixture(), snap = window.formaDesigner.snapPosition;
  const item = { width: 40, height: 20 };
  assert.equal(snap(item, 298, 70, 640, 440, []).x, 300);
  assert.equal(snap(item, 289, 70, 640, 440, []).x, 289);
  const gap = snap(item, 110, 70, 640, 440, [{ x: 0, y: 0, width: 100, height: 20 }]);
  assert.equal(gap.x, 108); assert.equal(gap.guides.find(g => g.axis === 'x').label, '8px');
  assert.equal(snap(item, 598, 70, 640, 440, []).x, 600);
});

test('layer and image picker actions send explicit designer commands', () => {
  const { window, document, messages, create } = fixture(); create('image', 'image', 'root');
  const d = window.formaDesigner; d.selectedId = 'image';
  d.state = { controls: [{ id: 'image', kind: 'image', locked: false }], propertySchema: [] }; d.inspector();
  for (const command of ['choose-image', 'bring-front', 'send-back']) {
    document.querySelector(`[data-command="${command}"]`).click();
    assert.equal(messages.at(-1).event, 'command'); assert.equal(messages.at(-1).payload.command, command);
  }
});
