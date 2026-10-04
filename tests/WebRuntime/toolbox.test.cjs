const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');

test('context menus target controls, survive component tray rendering and send command events', () => {
  const { window, document, messages, create } = fixture(); window.formaDesigner.preview = true;
  create('target', 'button', 'root');
  create('context', 'contextmenu', 'root', { targetId: 'target', commandItems: [{ Id: 'action', Text: 'Action' }] });
  window.formaDesigner.applyAppearance({ id: 'context', kind: 'contextmenu', component: true, name: 'contextMenu1', enabled: true });
  const target = document.getElementById('target');
  target.dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true, clientX: 200, clientY: 150 }));
  let popup = document.querySelector('.forma-context-popup'); assert.ok(popup);
  popup.querySelector('button').click();
  assert.equal(messages.at(-1).id, 'context'); assert.equal(messages.at(-1).payload.itemId, 'action');
  assert.equal(document.querySelector('.forma-context-popup'), null);
  target.dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
  window.document.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
  assert.equal(document.querySelector('.forma-context-popup'), null);
  window.formaDesigner.applyAppearance({ id: 'context', kind: 'contextmenu', component: true, name: 'contextMenu1', enabled: false });
  target.dispatchEvent(new window.MouseEvent('contextmenu', { bubbles: true, cancelable: true }));
  assert.equal(document.querySelector('.forma-context-popup'), null);
});

test('dialogs show safe modal content, report results and enforce Escape dismissal settings', () => {
  const { window, document, messages, create } = fixture(); window.formaDesigner.preview = true;
  create('dialog', 'confirmationdialog', 'root', { dialogTitle: 'Confirm', message: '<script>Message</script>', buttons: 'YesNo', isOpen: true, canCancel: false });
  const modal = document.querySelector('.forma-dialog-popup'); assert.ok(modal); assert.equal(modal.open, true);
  assert.equal(modal.querySelector('script'), null); assert.equal(modal.querySelector('p').textContent, '<script>Message</script>');
  const count = messages.length;
  modal.dispatchEvent(new window.Event('cancel', { cancelable: true }));
  assert.equal(messages.length, count); assert.equal(modal.open, true);
  modal.querySelector('button').click();
  assert.equal(messages.at(-1).event, 'dialog-result'); assert.equal(messages.at(-1).payload.result, 'Yes');
  window.forma.receive({ type: 'update', id: 'dialog', properties: { isOpen: false } });
  assert.equal(document.querySelector('.forma-dialog-popup'), null);
});

test('context target selector lists named controls and dialog preview action has the right guard', () => {
  const { window, document, create } = fixture(); create('context', 'contextmenu', 'root'); create('dialog', 'dialog', 'root');
  const d = window.formaDesigner;
  d.state = { controls: [{ id: 'root', name: 'Form1', kind: 'form' }, { id: 'context', name: 'Menu1', kind: 'contextmenu', component: true }],
    propertySchema: [{ id: 'targetId', label: 'Target', category: 'Behavior', editor: 'target' }] };
  d.selectedId = 'context'; d.inspector();
  const selector = document.getElementById('prop-targetId');
  assert.equal(selector.tagName, 'SELECT'); assert.equal(selector.options[1].textContent, 'Form1');
  assert.equal(selector.options.length, 2);
  d.state = { controls: [{ id: 'dialog', kind: 'dialog', component: true, enabled: true }], propertySchema: [] };
  d.selectedId = 'dialog'; d.inspector();
  assert.equal(document.querySelector('[data-command="show-dialog"]').disabled, true);
  d.preview = true; d.inspector();
  assert.equal(document.querySelector('[data-command="show-dialog"]').disabled, false);
});

test('nested menus send leaf commands, toggle checked labels and block disabled branches', () => {
  const { window, document, messages, create } = fixture(); window.formaDesigner.preview = true;
  const commandItems = [
    { Id: 'file', Text: 'File', Items: [{ Id: 'open', Text: 'Open', Enabled: true }] },
    { Id: 'blocked', Text: 'Blocked', Enabled: false, Items: [{ Id: 'child', Text: 'Child' }] },
    { Id: 'separator', Text: '', Separator: true },
    { Id: 'grid', Text: 'Grid', Checked: true, CheckOnClick: true }
  ];
  create('menu', 'menustrip', 'root', { commandItems });
  const menu = document.getElementById('menu'), dropdown = menu.querySelector('details');
  dropdown.open = true; menu.querySelector('[data-command-item="open"]').click();
  assert.equal(messages.at(-1).event, 'command-item'); assert.equal(messages.at(-1).payload.itemId, 'open');
  assert.equal(dropdown.open, false);
  assert.equal(menu.querySelector('[data-command-item="child"]').disabled, true);
  assert.equal(menu.querySelector('[data-command-item="grid"]').getAttribute('aria-pressed'), 'true');
  const count = messages.length;
  menu.querySelector('[data-command-item="child"]').click(); assert.equal(messages.length, count);
  window.formaDesigner.applyAppearance({ id: 'menu', kind: 'menustrip', width: 500, height: 36, enabled: false, visible: true });
  menu.querySelector('[data-command-item="open"]').click(); assert.equal(messages.length, count);
});

test('toolbars expose orientation and status bars render text safely', () => {
  const { window, document, messages, create } = fixture(); window.formaDesigner.preview = true;
  for (const kind of ['toolbar', 'toolstrip']) {
    create(kind, kind, 'root', { commandItems: [{ id: 'save', text: 'Save' }], orientation: 'vertical' });
    const toolbar = document.getElementById(kind);
    assert.equal(toolbar.dataset.orientation, 'vertical'); assert.equal(toolbar.getAttribute('role'), 'toolbar');
    toolbar.querySelector('button').click(); assert.equal(messages.at(-1).payload.itemId, 'save');
  }
  create('status', 'statusbar', 'root', { text: '<script>Ready</script>', rightText: 'Ln 1' });
  const status = document.getElementById('status');
  assert.equal(status.querySelector('script'), null); assert.equal(status.querySelector('.status-primary').textContent, '<script>Ready</script>');
  assert.equal(status.querySelector('.status-secondary').textContent, 'Ln 1'); assert.equal(status.getAttribute('role'), 'status');
});

test('path pickers display selections, request host dialogs and respect disabled preview state', () => {
  const { window, document, messages, create } = fixture();
  window.formaDesigner.preview = true;
  for (const kind of ['filepicker', 'folderpicker']) {
    create(kind, kind, 'root', { selectedPath: 'C:\\Example', dialogTitle: 'Choose example', text: 'Choose…' });
    const picker = document.getElementById(kind);
    assert.equal(picker.querySelector('input').value, 'C:\\Example');
    assert.equal(picker.querySelector('input').readOnly, true);
    assert.equal(picker.querySelector('button').textContent, 'Choose…');
    picker.querySelector('button').click();
    assert.equal(messages.at(-1).event, 'browse'); assert.equal(messages.at(-1).id, kind);
    window.formaDesigner.applyAppearance({ id: kind, kind, enabled: false, visible: true, width: 320, height: 40 });
    const count = messages.length;
    picker.querySelector('button').click(); assert.equal(messages.length, count);
  }
});

test('property grid renders safe categorized rows, preserves editors and enforces read-only values', () => {
  const { window, document, messages, create } = fixture();
  window.formaDesigner.preview = true;
  const entries = [{ Name: 'Title', Value: '<script>text</script>', Category: 'General', ReadOnly: false },
    { Name: 'ID', Value: '123', Category: 'Advanced', ReadOnly: true }];
  create('grid', 'propertygrid', 'root', { entries, readOnly: false });
  const grid = document.getElementById('grid'), inputs = grid.querySelectorAll('input');
  assert.equal(grid.querySelector('script'), null);
  assert.equal(grid.querySelectorAll('.property-category').length, 2);
  assert.equal(inputs[0].value, '<script>text</script>'); assert.equal(inputs[1].readOnly, true);
  inputs[0].value = 'Edited'; inputs[0].dispatchEvent(new window.Event('change'));
  assert.equal(messages.at(-1).event, 'property-value'); assert.equal(messages.at(-1).payload.index, 0);
  entries[0].Value = 'Edited';
  window.forma.receive({ type: 'update', id: 'grid', properties: { entries, readOnly: true } });
  assert.equal(grid.querySelector('input'), inputs[0]); assert.equal(inputs[0].readOnly, true);
  const count = messages.length;
  inputs[0].dispatchEvent(new window.Event('change')); assert.equal(messages.length, count);
  window.formaDesigner.preview = false;
  window.formaDesigner.applyAppearance({ id: 'grid', kind: 'propertygrid', enabled: true, visible: true, width: 300, height: 200, readOnly: false });
  assert.equal(inputs[0].readOnly, true);
});

test('picker inspector actions send choose-path commands and honor locks', () => {
  const { window, document, messages, create } = fixture(); create('picker', 'filepicker', 'root');
  const d = window.formaDesigner; d.selectedId = 'picker';
  d.state = { controls: [{ id: 'picker', kind: 'filepicker', locked: false }], propertySchema: [] };
  d.inspector(); document.querySelector('[data-command="choose-path"]').click();
  assert.equal(messages.at(-1).event, 'command'); assert.equal(messages.at(-1).payload.command, 'choose-path');
  d.state.controls[0].locked = true; d.inspector();
  assert.equal(document.querySelector('[data-command="choose-path"]').disabled, true);
});
test('data widgets emit selection, expansion and page events with boundary buttons', () => {
  const { window, document, messages, create } = fixture(); window.formaDesigner.preview = true;
  create('tree', 'treeview', 'root', { nodes: [{ Id: 'a', Text: 'Parent', Children: [{ Id: 'b', Text: 'Child' }] }], expandedNodes: ['a'] });
  const tree = document.getElementById('tree'); tree.querySelectorAll('button')[2].click();
  assert.equal(messages.at(-1).payload.node, 'b'); assert.equal(messages.at(-1).event, 'tree-select');
  tree.querySelector('button').click(); assert.equal(messages.at(-1).payload.expanded, false);
  create('pages', 'pagination', 'root', { page: 1, pageCount: 3 });
  const buttons = document.getElementById('pages').querySelectorAll('button');
  assert.equal(buttons[0].disabled, true); buttons[1].click(); assert.equal(messages.at(-1).payload.page, 2);
  create('list', 'listview', 'root', { items: ['A', 'B'], selectedIndex: 1 });
  assert.equal(document.getElementById('list').selectedIndex, 1);
});
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
