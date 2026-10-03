const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function fixture() {
  const messages = [];
  const listeners = {};
  function element(id, type) {
    const classes = new Set();
    return {
      id, dataset: { formaType: type }, style: {}, attributes: {}, clientLeft: 1, clientTop: 1,
      classList: { add: x => classes.add(x), remove: x => classes.delete(x), contains: x => classes.has(x),
        toggle: (x, on) => on ? classes.add(x) : classes.delete(x) },
      appendChild(node) { nodes.push(node); },
      setPointerCapture(id) { this.capture = id; },
      hasPointerCapture(id) { return this.capture === id; },
      releasePointerCapture() { this.capture = null; },
      setAttribute(key, value) { this.attributes[key] = value; },
      closest(selector) {
        if (selector === '[data-handle]') return this.dataset.handle ? this : null;
        if (selector === '[data-forma-type]') return this.dataset.formaType ? this : null;
        return null;
      },
    };
  }
  const root = element('form', 'form');
  const button = element('button', 'button');
  const nodes = [root, button];
  root.contains = target => nodes.includes(target);
  root.clientWidth = 640; root.clientHeight = 440;
  root.getBoundingClientRect = () => ({ left: 120, top: 80 });
  const document = {
    body: { classList: { remove() {}, toggle() {} } },
    createElement: tag => element('', tag),
    getElementById: id => nodes.find(node => node.id === id),
    querySelectorAll: () => nodes.filter(node => node.classList.contains('forma-selected')),
    addEventListener: (name, handler) => { listeners[name] = handler; },
  };
  const window = { chrome: { webview: { postMessage: message => messages.push(message), addEventListener() {} } } };
  const context = vm.createContext({ window, document, console });
  for (const relative of ['src/Forma.WebView2/Web/scripts/forma.js', 'src/Forma.Builder/DesignerWeb/designer.js']) {
    vm.runInContext(fs.readFileSync(path.join(__dirname, '../..', relative), 'utf8'), context);
  }
  window.forma.receive({ type: 'designer', action: 'initialize', id: 'form', title: 'My form' });
  return { window, root, button, listeners, messages, nodes };
}

test('drop uses form coordinates, including its border, and carries the current form identity', () => {
  const { listeners, root, messages } = fixture();
  let prevented = false;
  listeners.drop({ target: root, clientX: 161, clientY: 141,
    preventDefault: () => { prevented = true; },
    dataTransfer: { getData: () => 'forma:button' },
  });
  assert.equal(prevented, true);
  assert.deepEqual(JSON.parse(JSON.stringify(messages)), [{
    type: 'designer', id: 'form', event: 'drop', payload: { control: 'button', x: 40, y: 60 },
  }]);
});

test('existing control drag commits position once and respects zoom', () => {
  const { window, root, button, listeners, messages } = fixture();
  const d = window.formaDesigner;
  d.state = { controls: [{ id: button.id, x: 40, y: 60, width: 120, height: 36 }] };
  d.zoom = 1.25;
  listeners.pointerdown({ target: button, button: 0, pointerId: 1, clientX: 200, clientY: 200,
    preventDefault() {}, stopImmediatePropagation() {} });
  listeners.pointermove({ pointerId: 1, clientX: 250, clientY: 225 });
  assert.equal(button.style.left, '80px');
  assert.equal(button.style.top, '80px');
  listeners.pointerup({ pointerId: 1 });
  assert.equal(messages.filter(m => m.event === 'move').length, 1);
  assert.deepEqual(JSON.parse(JSON.stringify(messages.at(-1).payload)), { x: 80, y: 80 });
  assert.equal(button.capture, null);
});

test('existing control stays within the form and cancelled movement restores position', () => {
  const { window, button, listeners, messages } = fixture();
  window.formaDesigner.state = { controls: [{ id: button.id, x: 40, y: 60, width: 120, height: 36 }] };
  listeners.pointerdown({ target: button, button: 0, pointerId: 2, clientX: 200, clientY: 200,
    preventDefault() {}, stopImmediatePropagation() {} });
  listeners.pointermove({ pointerId: 2, clientX: 2000, clientY: 2000 });
  assert.equal(button.style.left, '520px');
  assert.equal(button.style.top, '404px');
  listeners.pointercancel();
  assert.equal(button.style.left, '40px');
  assert.equal(button.style.top, '60px');
  assert.equal(messages.filter(m => m.event === 'move').length, 0);
});

test('preview mode prevents designer movement', () => {
  const { window, button, listeners, messages } = fixture();
  window.formaDesigner.preview = true;
  listeners.pointerdown({ target: button, button: 0 });
  assert.equal(window.formaDesigner.drag, null);
  assert.equal(messages.length, 0);
});

test('unrelated drag data and drops outside the form create no controls', () => {
  const { listeners, root, messages } = fixture();
  listeners.drop({ target: root, preventDefault() {}, dataTransfer: { getData: () => 'other text' } });
  listeners.drop({ target: {}, dataTransfer: { getData: () => 'forma:button' } });
  assert.equal(messages.length, 0);
});

test('designer click selects a control and suppresses its application click', () => {
  const { listeners, button, messages } = fixture();
  let stopped = false;
  listeners.click({ target: button, preventDefault() {}, stopImmediatePropagation: () => { stopped = true; } });
  assert.equal(stopped, true);
  assert.equal(messages[0].event, 'select');
  assert.equal(messages[0].id, 'button');
});

test('selection replaces the old outline and title updates the form caption', () => {
  const { window, root, button } = fixture();
  window.forma.receive({ type: 'designer', action: 'select', id: root.id });
  window.forma.receive({ type: 'designer', action: 'select', id: button.id });
  assert.equal(root.classList.contains('forma-selected'), false);
  assert.equal(button.classList.contains('forma-selected'), true);
  window.forma.receive({ type: 'designer', action: 'title', title: 'Renamed' });
  assert.equal(root.attributes['aria-label'], 'Renamed');
});

test('runtime position can switch back to normal layout', () => {
  const { window, button } = fixture();
  window.forma.applyPosition(button, { x: 40, y: 60 });
  assert.deepEqual(button.style, { position: 'absolute', left: '40px', top: '60px' });
  window.forma.applyPosition(button, { x: null, y: null });
  assert.deepEqual(button.style, { position: '', left: '', top: '' });
});

test('north-west resizing keeps the opposite corner fixed and enforces minimum size', () => {
  const { window } = fixture();
  const item = { x: 40, y: 60, width: 120, height: 36 };
  const result = window.formaDesigner.resizeBounds(item, 'nw', 20, 10, 640, 440);
  assert.deepEqual(JSON.parse(JSON.stringify(result)), { x: 60, y: 70, width: 100, height: 26 });
  const minimum = window.formaDesigner.resizeBounds(item, 'nw', 900, 900, 640, 440);
  assert.equal(minimum.width, 24); assert.equal(minimum.height, 20);
  assert.equal(minimum.x + minimum.width, 160); assert.equal(minimum.y + minimum.height, 96);
});

test('south-east resizing clamps to the parent boundary', () => {
  const { window } = fixture();
  const result = window.formaDesigner.resizeBounds({ x: 40, y: 60, width: 120, height: 36 }, 'se', 900, 900, 640, 440);
  assert.equal(result.width, 600); assert.equal(result.height, 380);
});

test('resize handle uses zoom and commits all four bounds once', () => {
  const { window, button, listeners, messages, nodes } = fixture();
  const d = window.formaDesigner;
  d.state = { controls: [{ id: button.id, x: 40, y: 60, width: 120, height: 36 }] };
  Object.assign(button.style, { left: '40px', top: '60px', width: '120px', height: '36px' });
  d.select(button.id); d.zoom = 1.25;
  const handle = nodes.find(n => n.dataset.handle === 'se');
  listeners.pointerdown({ target: handle, button: 0, pointerId: 4, clientX: 200, clientY: 200,
    preventDefault() {}, stopImmediatePropagation() {} });
  listeners.pointermove({ pointerId: 4, clientX: 250, clientY: 225 });
  assert.equal(button.style.width, '160px'); assert.equal(button.style.height, '56px');
  listeners.pointerup({ pointerId: 4 });
  assert.equal(messages.filter(m => m.event === 'resize').length, 1);
  assert.deepEqual(JSON.parse(JSON.stringify(messages.at(-1).payload)), { x: 40, y: 60, width: 160, height: 56 });
  assert.equal(handle.capture, null);
});

test('cancelling corner resize restores dimensions and sends no commit', () => {
  const { window, button, listeners, messages, nodes } = fixture();
  const d = window.formaDesigner;
  d.state = { controls: [{ id: button.id, x: 40, y: 60, width: 120, height: 36 }] };
  Object.assign(button.style, { left: '40px', top: '60px', width: '120px', height: '36px' });
  d.select(button.id);
  const handle = nodes.find(n => n.dataset.handle === 'nw');
  listeners.pointerdown({ target: handle, button: 0, pointerId: 5, clientX: 200, clientY: 200,
    preventDefault() {}, stopImmediatePropagation() {} });
  listeners.pointermove({ pointerId: 5, clientX: 180, clientY: 180 });
  listeners.pointercancel();
  assert.equal(button.style.width, '120px'); assert.equal(button.style.height, '36px');
  assert.equal(button.style.left, '40px'); assert.equal(button.style.top, '60px');
  assert.equal(messages.length, 0);
});

test('form resize stays anchored and supports a larger canvas', () => {
  const { window } = fixture();
  const result = window.formaDesigner.resizeBounds({ x: 0, y: 0, width: 640, height: 440 }, 'se', 200, 100, 640, 440, true);
  assert.deepEqual(JSON.parse(JSON.stringify(result)), { x: 0, y: 0, width: 840, height: 540 });
});
