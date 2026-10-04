const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('../../src/Forma.Builder/Frontend/node_modules/jsdom');
const base = path.join(__dirname, '../..');

test('FilePicker path can set Avatar source and getter follows updated preview state', () => {
  const { window, document, create, messages } = fixture('preview.html');
  create('apply', 'button', 'root'); create('picker', 'filepicker', 'root', { selectedPath: 'C:\\Pictures\\face.png' });
  create('avatar', 'avatar', 'root', { source: '', text: 'User' });
  const controls = [
    { id: 'root', name: 'Form1', kind: 'form', width: 640, height: 440 },
    { id: 'picker', name: 'inputFile', kind: 'filepicker', enabled: true, visible: true },
    { id: 'avatar', name: 'userAvatar', kind: 'avatar', source: '', enabled: true, visible: true },
    { id: 'apply', name: 'applyButton', kind: 'button', enabled: true, visible: true, customization: {
      behavior: 'api.on("click", () => { api.set("userAvatar", "source", api.get("inputFile", "selectedPath")); component.properties.seen = api.get("userAvatar", "source"); });'
    } }
  ];
  window.formaDesigner.receive({ action: 'runtime-preview', id: 'root', controls });
  document.getElementById('apply').click();
  assert.equal(messages.at(-1).payload.property, 'source');
  assert.equal(messages.at(-1).payload.value, 'C:\\Pictures\\face.png');
  controls[2].source = 'file:///C:/Pictures/face.png';
  window.forma.receive({ type: 'update', id: 'avatar', properties: { source: controls[2].source } });
  window.formaDesigner.receive({ action: 'runtime-preview', id: 'root', controls });
  assert.equal(document.getElementById('avatar').querySelector('img').getAttribute('src'), controls[2].source);
  assert.equal(messages.some(m => m.type === 'custom' && m.event === 'error'), false);
});

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
function fixture(page = 'index.html') {
  const dom = new JSDOM(fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb', page), 'utf8'), { runScripts: 'outside-only' });
  const messages = []; const window = dom.window;
  window.chrome = { webview: { postMessage: m => messages.push(m), addEventListener() {} } };
  for (const file of ['src/Forma.WebView2/Web/scripts/data-grid.js', 'src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.WebView2/Web/scripts/tooltips.js', 'src/Forma.WebView2/Web/scripts/icons.js', 'src/Forma.WebView2/Web/scripts/modern-controls.js', 'src/Forma.Builder/DesignerWeb/designer.js', 'src/Forma.Builder/DesignerWeb/component-customization.js']) window.eval(fs.readFileSync(path.join(base, file), 'utf8'));
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

test('split panes and table cells preserve children when orientation and grid dimensions change', () => {
  const { document, create, window } = fixture();
  create('split', 'splitcontainer', 'root', { orientation: 'horizontal' });
  create('left', 'button', 'split', { layoutSlot: 1, x: 0, y: 0 });
  create('right', 'label', 'split', { layoutSlot: 2 });
  const split = document.getElementById('split');
  assert.equal(split.querySelectorAll('.layout-pane').length, 2);
  assert.equal(document.getElementById('right').parentElement.dataset.layoutSlot, '2');
  assert.equal(document.getElementById('left').style.position, 'absolute');
  window.forma.update({ id: 'split', properties: { orientation: 'vertical' } });
  assert.equal(split.querySelector('.layout-content').style.gridTemplateRows, 'repeat(2, minmax(0px, 1fr))');
  assert.equal(document.getElementById('right').parentElement.dataset.layoutSlot, '2');
  create('table', 'tablelayoutpanel', 'root', { columns: 3, rowCount: 2 });
  create('cellChild', 'button', 'table', { layoutSlot: 6 });
  const table = document.getElementById('table');
  assert.equal(table.querySelectorAll('.layout-cell').length, 6);
  window.forma.update({ id: 'cellChild', properties: { layoutSlot: 2 } });
  assert.equal(document.getElementById('cellChild').parentElement.dataset.layoutSlot, '2');
  window.forma.update({ id: 'table', properties: { columns: 2, rowCount: 3 } });
  assert.equal(table.querySelectorAll('.layout-cell').length, 6);
  assert.equal(table.querySelector('[data-layout-slot="2"]').getAttribute('aria-label'), 'Row 1, column 2');
  assert.equal(document.getElementById('cellChild').parentElement.dataset.layoutSlot, '2');
});

test('vertical tabs expose orientation and both add actions target the tab control', () => {
  const { document, create, window, messages } = fixture();
  create('tabs', 'tabcontrol', 'root', { orientation: 'vertical', tabs: ['One'] });
  const tabs = document.getElementById('tabs');
  assert.equal(tabs.dataset.orientation, 'vertical');
  assert.equal(tabs.querySelector('[role="tablist"]').getAttribute('aria-orientation'), 'vertical');
  tabs.querySelector('[data-tab-add]').click();
  assert.equal(messages.at(-1).id, 'tabs');
  assert.equal(messages.at(-1).payload.command, 'add-tab');
  const d = window.formaDesigner;
  d.state = { controls: [{ id: 'tabs', kind: 'tabcontrol', locked: false }], propertySchema: [] };
  d.selectedId = 'tabs'; d.inspector();
  document.querySelector('[data-command="add-tab"]').click();
  assert.equal(messages.at(-1).id, 'tabs');
  assert.equal(messages.at(-1).payload.command, 'add-tab');
});

test('standalone preview runs inputs, hides components and keeps Escape out of design mode', () => {
  const { window, document, create, messages } = fixture('preview.html');
  create('input', 'textbox', 'root', { text: 'Original' });
  create('timer', 'timer', 'root');
  const item = { id: 'input', kind: 'textbox', parentId: 'root', width: 180, height: 36,
    enabled: true, visible: true, focusable: true, fontSize: 14, opacity: 100, maxLength: 100 };
  window.formaDesigner.receive({ action: 'runtime-preview', id: 'root', controls: [
    { id: 'root', kind: 'form', width: 640, height: 440 }, item,
    { id: 'timer', kind: 'timer', component: true, enabled: true }
  ] });
  assert.equal(window.formaDesigner.preview, true);
  assert.equal(document.getElementById('timer').hidden, true);
  const input = document.getElementById('input');
  assert.equal(input.disabled, false);
  input.value = 'Test value'; input.dispatchEvent(new window.Event('input', { bubbles: true }));
  assert.equal(messages.at(-1).type, 'event'); assert.equal(messages.at(-1).payload.text, 'Test value');
  document.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
  assert.equal(window.formaDesigner.preview, true);
  assert.equal(messages.filter(message => message.event === 'preview').length, 0);
  assert.equal(document.querySelector('#selection-outline'), null);
  assert.equal(document.querySelector('.toolbox, .toolbar, .properties'), null);
});

test('component CSS is scoped and behavior runs once per preview with independent custom values', () => {
  const { window, document, create, messages } = fixture('preview.html');
  create('button', 'button', 'root', { text: 'Click' }); create('label', 'label', 'root', { text: 'Count' });
  const customization = { css: ':host { color: red; } :host:hover { background-color: blue; }',
    behavior: 'api.on("click", () => { component.characteristics.count += 1; api.set("label1", "text", String(component.characteristics.count)); });',
    characteristics: '{"count":0}' };
  const controls = [
    { id: 'root', kind: 'form', name: 'Form1', width: 640, height: 440 },
    { id: 'button', kind: 'button', name: 'button1', enabled: true, visible: true, customization },
    { id: 'label', kind: 'label', name: 'label1', enabled: true, visible: true }
  ];
  window.formaCustomization.apply({ controls }, false);
  document.getElementById('button').click();
  assert.equal(messages.filter(message => message.type === 'custom').length, 0);
  const style = [...document.head.querySelectorAll('style')].find(style => style.textContent.includes('[id="button"]'));
  assert.ok(style); assert.match(style.textContent, /color: red !important/);
  assert.match(style.textContent, /\[id="button"\]:hover/);
  window.formaDesigner.receive({ action: 'runtime-preview', id: 'root', controls });
  document.getElementById('button').click();
  assert.equal(messages.at(-1).type, 'custom'); assert.equal(messages.at(-1).id, 'label');
  assert.equal(messages.at(-1).payload.value, '1');
  window.formaDesigner.receive({ action: 'runtime-preview', id: 'root', controls: JSON.parse(JSON.stringify(controls)) });
  document.getElementById('button').click();
  assert.equal(messages.at(-1).payload.value, '2');
  assert.equal(messages.filter(message => message.type === 'custom' && message.event === 'set').length, 2);
  assert.equal(customization.characteristics, '{"count":0}');
  window.formaCustomization.clear();
  const count = messages.length; document.getElementById('button').click(); assert.equal(messages.length, count + 1); // Native click only.
});

test('component sources report errors once and clean up timer listeners', () => {
  const { window, document, create, messages } = fixture('preview.html');
  create('timer', 'timer', 'root');
  const item = { id: 'timer', kind: 'timer', name: 'timer1', enabled: true, visible: true,
    customization: { css: 'body { color: red; }', behavior: 'throw new Error("Bad script");', characteristics: '{}' } };
  const state = { controls: [item] };
  window.formaCustomization.apply(state, true); window.formaCustomization.apply(state, true);
  assert.equal(messages.filter(message => message.event === 'error').length, 2);
  assert.equal(document.head.querySelectorAll('style').length, 0);
  item.customization = { behavior: 'api.on("tick", () => api.set(component.id, "text", "Tick"));', characteristics: '{}' };
  window.formaCustomization.apply(state, true);
  window.formaCustomization.event('timer', 'tick'); assert.equal(messages.at(-1).event, 'set');
  const count = messages.length;
  window.formaCustomization.clear(); window.formaCustomization.event('timer', 'tick'); assert.equal(messages.length, count);
});

test('custom properties live in Advanced for nonvisual components and the form', () => {
  const { window, document, create } = fixture();
  create('timer', 'timer', 'root');
  const d = window.formaDesigner;
  for (const item of [{ id: 'timer', kind: 'timer', component: true }, { id: 'root', kind: 'form' }]) {
    d.state = { controls: [item], propertySchema: [] }; d.selectedId = item.id; d.inspector();
    const button = document.querySelector('[data-command="edit-custom-properties"]');
    assert.ok(button);
    assert.equal(button.textContent, 'Custom Properties…');
    assert.equal(button.closest('details').dataset.category, 'Advanced');
    assert.equal(document.getElementById('prop-customCss'), null);
    assert.equal(document.getElementById('prop-zIndex'), null);
    assert.equal(document.querySelector('[data-command="bring-front"]'), null);
  }
});

test('dragging managed children commits flow order and table destination cells', () => {
  for (const kind of ['flowlayoutpanel', 'tablelayoutpanel']) {
    const { window, document, create, messages } = fixture();
    create('layout', kind, 'root', { columns: 2, rowCount: 1 });
    create('first', 'button', 'layout', { x: 0, y: 0, layoutSlot: 1 });
    create('second', 'label', 'layout', { x: 0, y: 0, layoutSlot: 2 });
    const root = document.getElementById('root'), layout = document.getElementById('layout');
    const first = document.getElementById('first'), second = document.getElementById('second');
    const box = (el, left, top, width, height) => {
      el.getBoundingClientRect = () => ({ left, top, width, height, right: left + width, bottom: top + height });
      Object.defineProperties(el, { clientWidth: { value: width, configurable: true }, clientHeight: { value: height, configurable: true } });
    };
    box(root, 0, 0, 640, 440); box(layout, 0, 0, 400, 160);
    box(layout.querySelector('.layout-content'), 0, 0, 400, 160);
    box(first, 0, 0, 100, 36); box(second, 200, 0, 100, 36);
    const cells = [...layout.querySelectorAll('.layout-cell')];
    cells.forEach((cell, i) => box(cell, i * 200, 0, 200, 160));
    document.elementsFromPoint = () => [cells[1] ?? second, layout, root];
    first.setPointerCapture = () => {}; first.hasPointerCapture = () => false;
    const d = window.formaDesigner;
    d.state = { controls: [
      { id: 'root', kind: 'form', width: 640, height: 440 },
      { id: 'layout', kind, parentId: 'root', width: 400, height: 160 },
      ...['first', 'second'].map(id => ({ id, kind: id === 'first' ? 'button' : 'label', parentId: 'layout', x: 0, y: 0, width: 100, height: 36 }))
    ] };
    const pointer = (type, x, y) => {
      const event = new window.MouseEvent(type, { bubbles: true, cancelable: true, button: 0, clientX: x, clientY: y, altKey: true });
      Object.defineProperty(event, 'pointerId', { value: 1 });
      (type === 'pointerdown' ? first : document).dispatchEvent(event);
    };
    pointer('pointerdown', 10, 10); pointer('pointermove', 310, 10);
    assert.match(first.style.transform, /translate/);
    pointer('pointerup', 310, 10);
    const move = messages.filter(m => m.event === 'move');
    assert.equal(move.length, 1);
    assert.equal(move[0].id, 'first');
    assert.equal(first.style.transform, '');
    assert.equal(first.style.position, 'relative');
    if (kind === 'flowlayoutpanel') assert.equal(move[0].payload.index, 1);
    else {
      assert.equal(move[0].payload.layoutSlot, 2);
      assert.equal(move[0].payload.parentId, undefined);
    }
  }
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

test('grid sorting, filtering, selection and editing use original source row indices', () => {
  const { window, document, create, messages } = fixture('preview.html'); window.formaDesigner.preview = true;
  create('grid', 'datagridview', 'root', { columns: ['Name', 'Number'], rows: [['Ten', '10'], ['Two', '2']], sortColumn: 1,
    sortDirection: 'ascending', readOnly: false, filteringEnabled: true, sortingEnabled: true });
  const grid = document.getElementById('grid');
  assert.deepEqual([...grid.querySelectorAll('[data-grid-row]')].map(row => row.dataset.gridRow), ['1', '0']);
  grid.querySelector('th button').click(); assert.equal(messages.at(-1).event, 'grid-sort');
  const row = grid.querySelector('[data-grid-row="1"]'), cell = row.querySelector('td');
  row.click(); assert.equal(messages.at(-1).payload.row, 1);
  window.forma.update({ id: 'grid', properties: { selectedRow: 1 } });
  assert.equal(grid.querySelector('[data-grid-row="1"] td'), cell);
  assert.equal(row.getAttribute('aria-selected'), 'true');
  cell.textContent = 'Edited'; cell.dispatchEvent(new window.Event('blur'));
  assert.equal(messages.at(-1).event, 'cell'); assert.equal(messages.at(-1).payload.row, 1);
  const filter = grid.querySelector('.grid-filter'); filter.focus(); filter.value = 'Ten'; filter.dispatchEvent(new window.Event('input'));
  assert.equal(messages.at(-1).event, 'grid-filter');
  window.forma.update({ id: 'grid', properties: { filterText: 'Ten' } });
  assert.equal(document.activeElement, filter); assert.equal(grid.querySelectorAll('[data-grid-row]').length, 1);
  assert.equal(grid.querySelector('[data-grid-row]').dataset.gridRow, '0');
  window.forma.update({ id: 'grid', properties: { filterText: 'none' } }); assert.equal(grid.querySelector('.grid-empty').textContent, 'No matching rows');
});

test('grid keyboard selection works while design mode and disabled grids block interaction', () => {
  const { window, document, create, messages } = fixture('preview.html'); window.formaDesigner.preview = true;
  create('grid', 'datagridview', 'root', { columns: ['Name'], rows: [['A'], ['B']], readOnly: false });
  const grid = document.getElementById('grid');
  grid.querySelector('[data-grid-row="0"]').dispatchEvent(new window.KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
  assert.equal(messages.at(-1).event, 'grid-select'); assert.equal(messages.at(-1).payload.row, 1);
  window.formaDesigner.applyAppearance({ id: 'grid', kind: 'datagridview', enabled: false, visible: true, readOnly: false });
  const count = messages.length; grid.querySelector('th button').click(); grid.querySelector('[data-grid-row]').click();
  assert.equal(messages.length, count); assert.equal(grid.querySelector('td').contentEditable, 'false');
  window.formaDesigner.preview = false; window.formaDataGrid.render(grid, {});
  assert.equal(grid.querySelector('th button').disabled, true);
});

test('tooltip supports keyboard focus, safe text and aria cleanup with Escape and removal', async () => {
  const { window, document, create } = fixture('preview.html'); window.formaDesigner.preview = true;
  create('target', 'button', 'root');
  create('tip', 'tooltip', 'root', { text: '<script>Helpful</script>', targetId: 'target', initialDelay: 0, showDuration: 5000, placement: 'bottom' });
  const target = document.getElementById('target'); target.title = 'Native'; target.setAttribute('aria-describedby', 'existing');
  target.dispatchEvent(new window.FocusEvent('focusin', { bubbles: true }));
  await new Promise(resolve => window.setTimeout(resolve, 10));
  const popup = document.querySelector('[role="tooltip"]'); assert.ok(popup); assert.equal(popup.textContent, '<script>Helpful</script>');
  assert.equal(popup.querySelector('script'), null); assert.match(target.getAttribute('aria-describedby'), /existing tip-popup/);
  document.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
  assert.equal(document.querySelector('[role="tooltip"]'), null); assert.equal(target.getAttribute('aria-describedby'), 'existing'); assert.equal(target.title, 'Native');
  window.formaDesigner.preview = true;
  target.dispatchEvent(new window.MouseEvent('mouseover', { bubbles: true })); await new Promise(resolve => window.setTimeout(resolve, 10));
  assert.ok(document.querySelector('[role="tooltip"]'));
  window.forma.receive({ type: 'remove', id: 'target' }); assert.equal(document.querySelector('[role="tooltip"]'), null);
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


test('modern controls preserve card children and render safe avatar, divider and spinner content', () => {
  const { window, document, create } = fixture();
  create('card', 'card', 'root', { text: 'Profile', description: 'Details' });
  create('child', 'button', 'card', { text: 'Save' });
  const child = document.getElementById('child');
  window.forma.update({ id: 'card', properties: { headerVisible: false, description: 'Changed' } });
  assert.equal(document.querySelector('#card > .layout-header').hidden, true);
  assert.equal(document.querySelector('#card > .layout-content').style.height, '100%');
  assert.equal(document.getElementById('child'), child);
  create('avatar', 'avatar', 'root', { text: 'Ada Lovelace', shape: 'circle', source: 'data:image/png;base64,AA==' });
  const avatar = document.getElementById('avatar');
  assert.equal(avatar.querySelector('.avatar-initials').textContent, 'AL');
  avatar.querySelector('img').dispatchEvent(new window.Event('error'));
  assert.equal(avatar.querySelector('.avatar-initials').hidden, false);
  window.forma.update({ id: 'avatar', properties: { initials: 'AB', shape: 'square' } });
  assert.equal(avatar.dataset.shape, 'square'); assert.equal(avatar.querySelector('.avatar-initials').textContent, 'AB');
  create('divider', 'divider', 'root', { text: '<b>Section</b>', orientation: 'vertical', thickness: 3, lineStyle: 'dashed' });
  assert.equal(document.getElementById('divider').getAttribute('aria-orientation'), 'vertical');
  assert.equal(document.querySelector('#divider b'), null);
  assert.equal(document.querySelector('#divider .divider-line').style.borderLeftWidth, '3px');
  create('spinner', 'spinner', 'root', { isActive: true, speed: 300 });
  assert.equal(document.querySelector('#spinner .modern-spinner').style.animationPlayState, 'paused');
  window.formaDesigner.preview = true; window.formaModern.refresh(document.getElementById('spinner'), { enabled: true });
  assert.equal(document.querySelector('#spinner .modern-spinner').style.animationPlayState, 'running');
  assert.equal(document.querySelector('#spinner .modern-spinner').style.animationDuration, '300ms');
  window.close();
});

test('toasts honor preview, dismissal and timeout without restarting on repeated state', () => {
  const { window, document, create, messages } = fixture();
  const scheduled = []; window.setTimeout = callback => { scheduled.push(callback); return scheduled.length; }; window.clearTimeout = () => {};
  create('notice', 'toast', 'root', { text: '<b>Saved</b>', isOpen: true, duration: 500, dismissible: false });
  assert.equal(document.querySelector('.forma-toast-popup'), null);
  window.formaDesigner.preview = true;
  window.formaModern.refresh(document.getElementById('notice'), { enabled: true, visible: true });
  const popup = document.querySelector('.forma-toast-popup'); assert.ok(popup);
  assert.equal(popup.querySelector('b'), null); assert.equal(popup.querySelector('button'), null);
  window.formaModern.refresh(document.getElementById('notice'), { enabled: true });
  assert.equal(document.querySelector('.forma-toast-popup'), popup); assert.equal(scheduled.length, 1);
  scheduled[0](); assert.equal(document.querySelector('.forma-toast-popup'), null);
  assert.equal(messages.at(-1).event, 'toast-close'); assert.equal(messages.at(-1).payload.reason, 'timeout');
  window.forma.update({ id: 'notice', properties: { dismissible: true } });
  document.querySelector('.forma-toast-popup button').click(); assert.equal(messages.at(-1).payload.reason, 'dismiss');
  window.close();
});

test('loading overlays cover their target and restore shared busy state on cleanup', () => {
  const { window, document, create } = fixture(); create('target', 'panel', 'root');
  const target = document.getElementById('target'); target.setAttribute('aria-busy', 'false');
  target.getBoundingClientRect = () => ({ left: 12, top: 20, width: 250, height: 180 });
  create('overlay', 'loadingoverlay', 'root', { targetId: 'target', isActive: true });
  assert.equal(document.querySelector('.forma-loading-popup'), null);
  window.formaDesigner.preview = true;
  window.formaModern.refresh(document.getElementById('overlay'), { enabled: true });
  const popup = document.querySelector('.forma-loading-popup'); assert.equal(popup.style.width, '250px'); assert.equal(popup.style.left, '12px');
  assert.equal(target.getAttribute('aria-busy'), 'true'); assert.equal(document.getElementById('overlay').hidden, true);
  create('second-overlay', 'loadingoverlay', 'root', { targetId: 'target', isActive: true });
  window.forma.update({ id: 'overlay', properties: { isActive: false } });
  assert.equal(target.getAttribute('aria-busy'), 'true');
  window.forma.remove({ id: 'second-overlay' });
  assert.equal(target.getAttribute('aria-busy'), 'false'); assert.equal(document.querySelector('.forma-loading-popup'), null);
  window.close();
});

test('custom properties can trigger toast and loading state through the preview bridge', () => {
  const { window, document, create, messages } = fixture(); window.formaDesigner.preview = true;
  create('button', 'button', 'root'); create('toast', 'toast', 'root'); create('overlay', 'loadingoverlay', 'root');
  window.formaCustomization.apply({ controls: [
    { id: 'button', name: 'button1', kind: 'button', enabled: true, visible: true, customization: { behavior: 'api.on("click", () => { api.showToast("toast1"); api.set("overlay1", "isActive", true); });' } },
    { id: 'toast', name: 'toast1', kind: 'toast', enabled: true, visible: true },
    { id: 'overlay', name: 'overlay1', kind: 'loadingoverlay', isActive: false, enabled: true, visible: true }
  ] }, true);
  document.getElementById('button').click();
  assert.equal(messages.at(-2).event, 'show-toast'); assert.equal(messages.at(-2).id, 'toast');
  assert.equal(messages.at(-1).payload.property, 'isActive'); assert.equal(messages.at(-1).payload.value, true);
  window.close();
});


test('Tooltip and modern toolbox entries support dragging onto cards', () => {
  const { window, document, create, messages } = fixture(); create('card', 'card', 'root');
  const host = document.querySelector('#card > .layout-content');
  host.getBoundingClientRect = () => ({ left: 20, top: 80, right: 320, bottom: 280 });
  for (const kind of ['icon', 'emptystate', 'skeleton', 'tooltip', 'card', 'badge', 'avatar', 'divider', 'toast', 'spinner', 'loadingoverlay']) {
    let transfer = '';
    const dataTransfer = { setData: (_, value) => { transfer = value; }, getData: () => transfer };
    const drag = new window.Event('dragstart', { bubbles: true }); Object.defineProperty(drag, 'dataTransfer', { value: dataTransfer });
    document.querySelector(`[data-kind="${kind}"]`).dispatchEvent(drag); assert.equal(transfer, `forma:${kind}`);
    const drop = new window.MouseEvent('drop', { bubbles: true, cancelable: true, clientX: 40, clientY: 100 });
    Object.defineProperty(drop, 'dataTransfer', { value: dataTransfer }); host.dispatchEvent(drop);
    assert.equal(messages.at(-1).event, 'drop'); assert.equal(messages.at(-1).id, 'card');
    assert.equal(messages.at(-1).payload.control, kind); assert.equal(messages.at(-1).payload.y, 20);
  }
  window.close();
});


test('badge typography follows inspector changes instead of a fixed inner font size', () => {
  const { window, document, create } = fixture(); create('badge', 'badge', 'root', { text: 'Large label' });
  const stylesheet = fs.readFileSync(path.join(base, 'src/Forma.Builder/DesignerWeb/designer.css'), 'utf8');
  const badgeRule = stylesheet.match(/\.badge-content\{([^}]+)\}/); assert.ok(badgeRule);
  // Check the shipped Tailwind output: an inner font would override the host's inherited size.
  assert.doesNotMatch(badgeRule[1], /font(?:-size|-weight)?:/);
  for (const preview of [false, true]) {
    window.formaDesigner.preview = preview;
    for (const size of [14, 32]) {
      window.formaDesigner.applyAppearance({ id: 'badge', kind: 'badge', fontSize: size, fontWeight: 'bold', enabled: true, visible: true });
      assert.equal(document.getElementById('badge').style.fontSize, `${size}px`);
      assert.equal(document.getElementById('badge').style.fontWeight, 'bold');
      assert.equal(document.querySelector('#badge .badge-content').textContent, 'Large label');
    }
  }
  window.close();
});

test('avatar image loads, survives appearance refreshes and falls back after removal', () => {
  const { window, document, create } = fixture();
  create('avatar', 'avatar', 'root', { text: 'Ada Lovelace' });
  const avatar = document.getElementById('avatar'), image = avatar.querySelector('img'), initials = avatar.querySelector('.avatar-initials');
  window.forma.update({ id: 'avatar', properties: { source: 'file:///C:/Users/Example/portrait%20photo.png' } });
  assert.equal(image.getAttribute('src'), 'file:///C:/Users/Example/portrait%20photo.png');
  image.dispatchEvent(new window.Event('load')); assert.equal(image.hidden, false); assert.equal(initials.hidden, true);
  for (const preview of [false, true]) {
    window.formaDesigner.preview = preview;
    window.formaDesigner.applyAppearance({ id: 'avatar', kind: 'avatar', enabled: true, visible: true, fontSize: 24 });
    assert.equal(avatar.querySelector('img'), image); assert.equal(image.hidden, false); assert.equal(initials.hidden, true);
  }
  window.forma.update({ id: 'avatar', properties: { source: '' } });
  assert.equal(image.hasAttribute('src'), false); assert.equal(image.hidden, true); assert.equal(initials.hidden, false);
  window.close();
});


test('loading overlay appears as a tray component and only creates a visual popup in preview', () => {
  const { window, document, create } = fixture();
  create('busy', 'loadingoverlay', 'root', { text: 'Please wait', targetId: 'root', isActive: true });
  const item = { id: 'busy', kind: 'loadingoverlay', component: true, name: 'loadingoverlay1', enabled: true, visible: true };
  window.formaDesigner.applyAppearance(item);
  const component = document.getElementById('busy');
  assert.equal(component.parentElement.id, 'component-tray'); assert.equal(component.textContent, 'loadingoverlay1');
  assert.equal(component.hidden, false); assert.equal(component.querySelector('.modern-spinner'), null);
  assert.equal(component.style.width, ''); assert.equal(document.querySelector('.forma-loading-popup'), null);
  const tool = document.querySelector('[data-kind="loadingoverlay"]');
  assert.equal(tool.parentElement.querySelector('summary').textContent, 'Components');
  window.formaDesigner.preview = true; window.formaDesigner.applyAppearance(item);
  assert.equal(component.hidden, true); assert.equal(document.querySelector('.forma-loading-popup .loading-message').textContent, 'Please wait');
  window.forma.update({ id: 'busy', properties: { isActive: false } });
  assert.equal(document.querySelector('.forma-loading-popup'), null);
  window.formaDesigner.preview = false; window.formaDesigner.applyAppearance(item);
  assert.equal(component.hidden, false); assert.equal(component.textContent, 'loadingoverlay1');
  window.close();
});


test('bundled icon choices render real SVG paths, follow color and validate fallback', () => {
  const { window, document, create } = fixture();
  create('symbol', 'icon', 'root', { text: 'Search', iconName: 'search', strokeWidth: 3 });
  const symbol = document.getElementById('symbol');
  assert.equal(symbol.getAttribute('role'), 'img'); assert.equal(symbol.getAttribute('aria-label'), 'Search');
  assert.equal(symbol.querySelector('svg').getAttribute('stroke'), 'currentColor');
  assert.equal(symbol.querySelector('svg').getAttribute('stroke-width'), '3');
  assert.equal(symbol.querySelector('svg').getAttribute('aria-hidden'), 'true');
  const names = ['image', 'search', 'folder-open', 'square-check', 'circle', 'house', 'settings', 'lock', 'calendar', 'file-plus', 'list', 'x'];
  for (const iconName of names) {
    window.forma.update({ id: 'symbol', properties: { iconName } });
    assert.ok(symbol.querySelector('svg > path, svg > circle, svg > rect, svg > line, svg > polyline'));
  }
  window.formaDesigner.applyAppearance({ id: 'symbol', kind: 'icon', foreColor: '#ff0000', enabled: true, visible: true });
  assert.equal(symbol.style.color, 'rgb(255, 0, 0)');
  window.forma.update({ id: 'symbol', properties: { iconName: '__proto__' } }); assert.ok(symbol.querySelector('svg'));
  window.close();
});

test('empty states update safe content and preserve their region label', () => {
  const { window, document, create } = fixture();
  create('empty', 'emptystate', 'root', { text: 'No results', description: '<script>Try a new search</script>', iconName: 'search' });
  const empty = document.getElementById('empty');
  assert.equal(empty.getAttribute('role'), 'region'); assert.equal(empty.getAttribute('aria-label'), 'No results');
  assert.equal(empty.querySelector('.empty-state-description').textContent, '<script>Try a new search</script>');
  assert.equal(empty.querySelector('script'), null); assert.ok(empty.querySelector('svg'));
  window.forma.update({ id: 'empty', properties: { text: 'All done', description: 'Nothing remains', iconName: 'square-check' } });
  assert.equal(empty.querySelector('.empty-state-title').textContent, 'All done');
  assert.equal(empty.getAttribute('aria-label'), 'All done'); assert.equal(empty.querySelector('.empty-state-description').textContent, 'Nothing remains');
  window.close();
});

test('skeletons preserve bars on appearance changes and animate only while active in preview', () => {
  const { window, document, create } = fixture();
  create('loading', 'skeleton', 'root', { text: 'Loading results', lines: 4, shape: 'text', isActive: true });
  const loading = document.getElementById('loading'); let bars = loading.querySelector('.skeleton-bars');
  assert.equal(bars.children.length, 4); assert.equal(bars.dataset.animated, 'false');
  assert.equal(bars.getAttribute('aria-hidden'), 'true'); assert.equal(loading.getAttribute('aria-label'), 'Loading results');
  window.formaDesigner.preview = true; window.formaModern.refresh(loading, { enabled: true, visible: true });
  assert.equal(loading.querySelector('.skeleton-bars'), bars); assert.equal(bars.dataset.animated, 'true');
  window.formaModern.refresh(loading, { enabled: false }); assert.equal(bars.dataset.animated, 'false');
  window.formaModern.refresh(loading, { enabled: true });
  window.forma.update({ id: 'loading', properties: { isActive: false } }); assert.equal(bars.dataset.animated, 'false');
  assert.equal(loading.getAttribute('aria-busy'), 'false');
  for (const shape of ['circle', 'rectangle']) {
    window.forma.update({ id: 'loading', properties: { shape } }); bars = loading.querySelector('.skeleton-bars');
    assert.equal(bars.dataset.shape, shape); assert.equal(bars.children.length, 1);
  }
  window.forma.update({ id: 'loading', properties: { shape: 'text', lines: 100 } });
  assert.equal(loading.querySelector('.skeleton-bars').children.length, 10);
  window.close();
});


test('unchanged state avoids modern DOM rebuilds across a 100-control form', () => {
  const { window, document, create } = fixture(); const items = [];
  for (let i = 0; i < 100; i++) {
    const id = `icon-${i}`; create(id, 'icon', 'root', { iconName: 'search', text: 'Search' });
    items.push({ id, parentId: 'root', kind: 'icon', text: 'Search', fontSize: 24, width: 32, height: 32, enabled: true, visible: true });
  }
  window.formaDesigner.state = { controls: items };
  for (const item of items) window.formaDesigner.applyAppearance(item);
  const original = document.querySelector('#icon-0 svg');
  let refreshes = 0; const refresh = window.formaModern.refresh;
  window.formaModern.refresh = function(...args) { refreshes++; return refresh.apply(this, args); };
  for (let pass = 0; pass < 20; pass++) for (const item of items) window.formaDesigner.applyAppearance({ ...item });
  assert.equal(refreshes, 0); assert.equal(document.querySelector('#icon-0 svg'), original);
  items[0].fontSize = 36; window.formaDesigner.applyAppearance(items[0]);
  assert.equal(refreshes, 1); assert.equal(document.getElementById('icon-0').style.fontSize, '36px');
  assert.equal(document.querySelector('#icon-0 svg'), original);
  window.forma.update({ id: 'icon-0', properties: { text: 'Find', iconName: 'folder-open' } });
  window.formaDesigner.applyAppearance(items[0]); assert.equal(refreshes, 2);
  assert.equal(document.getElementById('icon-0').getAttribute('aria-label'), 'Find');
  window.forma.remove({ id: 'icon-0' }); create('icon-0', 'icon', 'root', { iconName: 'search' });
  window.formaDesigner.applyAppearance(items[0]); assert.equal(refreshes, 3); // Same ID, new element must receive appearance.
  window.formaDesigner.preview = true; window.formaDesigner.applyAppearance(items[1]); assert.equal(refreshes, 4);
  window.close();
});

test('inspector selection options stay intact until names or membership change', () => {
  const { window, document, create } = fixture(); create('button', 'button', 'root');
  const controls = [{ id: 'root', name: 'Form1', kind: 'form' }, { id: 'button', name: 'button1', kind: 'button' }];
  window.formaDesigner.state = { controls, propertySchema: [] }; window.formaDesigner.selectedId = 'button';
  window.formaDesigner.inspector(); const option = document.querySelector('#selection option[value="button"]');
  for (let i = 0; i < 20; i++) window.formaDesigner.inspector();
  assert.equal(document.querySelector('#selection option[value="button"]'), option);
  controls[1].name = 'submitButton'; window.formaDesigner.inspector();
  assert.equal(document.querySelector('#selection option[value="button"]').textContent, 'submitButton');
  assert.notEqual(document.querySelector('#selection option[value="button"]'), option);
  window.close();
});


test('documented JavaScript recipes run against the current preview API', () => {
  for (const filename of ['greet-button.js', 'live-input.js', 'busy-button.js', 'grid-selection.js']) {
    const { window, document, create, messages } = fixture('preview.html'); window.formaDesigner.preview = true;
    const pending = []; window.setTimeout = callback => { pending.push(callback); return pending.length; }; window.clearTimeout = () => {};
    const kinds = { nameInput: 'textbox', greetingLabel: 'label', action: 'button', busyOverlay: 'loadingoverlay', savedToast: 'toast', grid: 'datagridview', rowLabel: 'label' };
    for (const [id, kind] of Object.entries(kinds)) create(id, kind, 'root', { text: '', rows: [['Ada']], columns: ['Name'], selectedRow: -1 });
    const source = filename === 'live-input.js' ? 'nameInput' : filename === 'grid-selection.js' ? 'grid' : 'action';
    window.formaCustomization.apply({ controls: Object.entries(kinds).map(([id, kind]) => ({
      id, name: id, kind, enabled: true, visible: true, customization: id === source ? { behavior: fs.readFileSync(path.join(base, 'docs/examples', filename), 'utf8') } : null
    })) }, true);
    document.getElementById('nameInput').value = 'Ada';
    if (filename === 'grid-selection.js') window.forma.update({ id: 'grid', properties: { selectedRow: 0 } });
    else if (filename === 'live-input.js') document.getElementById('nameInput').dispatchEvent(new window.Event('input', { bubbles: true }));
    else document.getElementById('action').click();
    if (filename === 'busy-button.js') {
      assert.equal(messages.at(-1).payload.property, 'isActive'); assert.equal(messages.at(-1).payload.value, true);
      pending[0](); assert.equal(messages.at(-1).event, 'show-toast'); assert.equal(messages.at(-2).payload.value, false);
    } else {
      assert.equal(messages.at(-1).event, 'set');
      assert.equal(messages.at(-1).payload.value, filename === 'greet-button.js' ? 'Hello, Ada!' : filename === 'live-input.js' ? 'Ada' : 'Source row: 0');
    }
    assert.equal(messages.some(message => message.type === 'custom' && message.event === 'error'), false);
    window.close();
  }
});


test('documented picker path recipe reads nested values after native selection', () => {
  const { window, document, create, messages } = fixture('preview.html'); window.formaDesigner.preview = true;
  const kinds = { inputFile: 'filepicker', outputFolder: 'folderpicker', pathLabel: 'label', readButton: 'button' };
  for (const [id, kind] of Object.entries(kinds)) create(id, kind, 'root');
  window.formaCustomization.apply({ controls: Object.entries(kinds).map(([id, kind]) => ({
    id, name: id, kind, enabled: true, visible: true,
    customization: id === 'readButton' ? { behavior: fs.readFileSync(path.join(base, 'docs/examples/read-picker-paths.js'), 'utf8') } : null
  })) }, true);
  document.getElementById('readButton').click();
  assert.equal(messages.at(-1).payload.value, 'File: None\nFolder: None');
  window.forma.update({ id: 'inputFile', properties: { selectedPath: 'C:/Pictures/avatar.png' } });
  window.forma.update({ id: 'outputFolder', properties: { selectedPath: 'C:/Output' } });
  document.getElementById('readButton').click();
  assert.equal(messages.at(-1).payload.value, 'File: C:/Pictures/avatar.png\nFolder: C:/Output');
  assert.equal(messages.at(-1).id, 'pathLabel');
  assert.equal(messages.some(m => m.type === 'custom' && m.event === 'error'), false);
  window.close();
});
