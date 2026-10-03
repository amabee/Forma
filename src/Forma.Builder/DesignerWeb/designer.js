// The web workspace presents the design; C# owns controls and committed properties.
const supportedKinds = new Set(["button", "label", "textbox", "panel"]);
const byId = id => document.getElementById(id);
const send = (event, id, payload = {}) => window.forma.send({ type: "designer", id, event, payload });

function resizeBounds(item, direction, dx, dy, parentWidth, parentHeight, isRoot = false) {
  const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, Math.round(v)));
  if (isRoot) return { x: 0, y: 0,
    width: direction.includes("e") ? clamp(item.width + dx, 240, 1600) : item.width,
    height: direction.includes("s") ? clamp(item.height + dy, 160, 1200) : item.height };
  let left = item.x, top = item.y, right = item.x + item.width, bottom = item.y + item.height;
  if (direction.includes("w")) left = clamp(left + dx, 0, right - 24);
  if (direction.includes("e")) right = clamp(right + dx, left + 24, parentWidth);
  if (direction.includes("n")) top = clamp(top + dy, 0, bottom - 20);
  if (direction.includes("s")) bottom = clamp(bottom + dy, top + 20, parentHeight);
  return { x: left, y: top, width: right - left, height: bottom - top };
}

window.formaDesigner = {
  rootId: null, selectedId: null, state: null, zoom: 1, preview: false, drag: null,
  root() { return byId(this.rootId); },
  resizeBounds,
  layoutFrame() {
    const root = this.root(); if (!root) return;
    const width = parseFloat(root.style.width) || 640, height = parseFloat(root.style.height) || 440;
    if (byId("form-frame")) Object.assign(byId("form-frame").style, { width: `${width + 2}px`, transform: `scale(${this.zoom})` });
    if (byId("scaled-frame")) Object.assign(byId("scaled-frame").style, { width: `${(width + 2) * this.zoom}px`, height: `${(height + 36) * this.zoom}px` });
    const device = document.querySelector?.(".device"); if (device) device.textContent = `▰  Desktop (${width} × ${height})`;
  },
  receive(message) {
    if (message.action === "initialize") {
      this.cancelDrag();
      this.rootId = message.id;
      this.preview = false;
      document.body.classList.remove("preview-mode");
      const root = this.root();
      byId("canvas-host")?.appendChild(root);
      root?.setAttribute("aria-label", message.title ?? "Form1");
      this.updatePreview();
    }
    if (message.action === "title") this.root()?.setAttribute("aria-label", message.title);
    if (message.action === "select") this.select(message.id);
    if (message.action === "state") {
      this.state = message;
      this.selectedId = message.selectedId;
      this.root()?.setAttribute("aria-label", message.title);
      for (const name of ["canvas-title", "form-name"]) if (byId(name)) byId(name).textContent = message.title;
      if (byId("status")) byId("status").textContent = message.status;
      for (const item of message.controls) this.applyAppearance(item);
      this.select(message.selectedId);
      this.inspector();
    }
  },
  select(id) {
    this.selectedId = id;
    document.querySelectorAll(".forma-selected").forEach(el => el.classList.remove("forma-selected"));
    byId(id)?.classList.add("forma-selected");
    this.outline();
  },
  applyAppearance(item) {
    const el = byId(item.id);
    if (!el) return;
    Object.assign(el.style, { width: `${item.width}px`, height: `${item.height}px`,
      color: item.foreColor, backgroundColor: item.backColor, fontSize: `${item.fontSize}px`,
      fontFamily: item.fontFamily, fontWeight: item.fontWeight, fontStyle: item.fontStyle, textAlign: item.textAlign });
    if (item.id === this.rootId) this.layoutFrame();
    if (item.id !== this.rootId) {
      Object.assign(el.style, { borderStyle: item.borderStyle, borderColor: item.borderColor,
        borderWidth: `${item.borderWidth}px`, borderRadius: `${item.borderRadius}px`, padding: `${item.padding}px`,
        opacity: String((item.opacity ?? 100) / 100 * (!this.preview && (!item.enabled || !item.visible) ? .5 : 1)) });
      if (item.kind === "label") el.style.justifyContent = { left: "flex-start", center: "center", right: "flex-end" }[item.textAlign];
      // Hidden and disabled controls stay selectable in design mode.
      el.classList.toggle("design-muted", !item.enabled || !item.visible);
      el.hidden = this.preview && !item.visible;
      if (item.kind === "button" || item.kind === "textbox") el.disabled = this.preview && !item.enabled;
      if (item.kind === "textbox") {
        el.placeholder = item.placeholder; el.readOnly = item.readOnly;
        el.type = item.password ? "password" : "text"; el.maxLength = item.maxLength;
      }
    }
  },
  inspector() {
    const selected = this.state?.controls.find(c => c.id === this.selectedId);
    if (!selected) return;
    const selection = byId("selection");
    if (selection) {
      selection.replaceChildren(...this.state.controls.map(c => {
        const option = document.createElement("option"); option.value = c.id; option.textContent = c.name; return option;
      }));
      selection.value = selected.id;
    }
    for (const property of ["name", "text", "width", "height", "x", "y", "fontSize", "foreColor", "backColor", "enabled", "visible", "fontFamily", "fontWeight", "fontStyle", "textAlign", "borderStyle", "borderColor", "borderWidth", "borderRadius", "padding", "opacity", "placeholder", "readOnly", "password", "maxLength"]) {
      const input = byId(`prop-${property}`);
      if (!input) continue;
      if (input.type === "checkbox") input.checked = selected[property];
      else if (input.value !== String(selected[property])) input.value = selected[property];
      input.disabled = this.preview || (selected.id === this.rootId && ["name", "x", "y", "enabled", "visible", "borderStyle", "borderColor", "borderWidth", "borderRadius", "padding", "opacity"].includes(property));
      if (["x", "width"].includes(property)) input.max = selected.id === this.rootId ? 1600 : this.root().clientWidth;
      if (["y", "height"].includes(property)) input.max = selected.id === this.rootId ? 1200 : this.root().clientHeight;
    }
    if (byId("textbox-properties")) byId("textbox-properties").hidden = selected.kind !== "textbox";
    if (byId("selection-status")) byId("selection-status").textContent = selected.id === this.rootId ? "Form selected" : "1 control selected";
    this.coordinates(selected.x, selected.y);
  },
  coordinates(x, y) { if (byId("coordinates")) byId("coordinates").textContent = `X: ${x}   Y: ${y}`; },
  outline() {
    const root = this.root(), selected = byId(this.selectedId);
    let outline = byId("selection-outline");
    if (!root || !selected || this.preview) { if (outline) outline.hidden = true; return; }
    if (!outline) {
      outline = document.createElement("div"); outline.id = "selection-outline"; outline.setAttribute("aria-hidden", "true");
      for (const direction of ["nw", "n", "ne", "e", "se", "s", "sw", "w"]) {
        const handle = document.createElement("i"); handle.dataset.handle = direction;
        handle.setAttribute("title", `Resize ${direction}`); outline.appendChild(handle);
      }
      (byId("form-frame") ?? root).appendChild(outline);
    }
    outline.hidden = false;
    const isRoot = selected === root;
    Object.assign(outline.style, { left: isRoot ? "-1px" : selected.style.left,
      top: isRoot ? "-1px" : `${(parseFloat(selected.style.top) || 0) + 34}px`,
      width: `${parseFloat(selected.style.width) + (isRoot ? 2 : 0)}px`,
      height: `${parseFloat(selected.style.height) + (isRoot ? 36 : 0)}px` });
    outline.querySelectorAll?.("[data-handle]").forEach(h => { h.hidden = isRoot && !["e", "s", "se"].includes(h.dataset.handle); });
  },
  cancelDrag() {
    if (!this.drag) return;
    const drag = this.drag; this.drag = null;
    Object.assign(drag.element.style, { left: `${drag.x}px`, top: `${drag.y}px` });
    if (drag.mode === "resize") Object.assign(drag.element.style, { width: `${drag.width}px`, height: `${drag.height}px` });
    const capture = drag.capture ?? drag.element;
    if (capture.hasPointerCapture?.(drag.pointerId)) capture.releasePointerCapture(drag.pointerId);
    this.coordinates(drag.x, drag.y);
    for (const [key, value] of [["x", drag.x], ["y", drag.y]]) if (byId(`prop-${key}`)) byId(`prop-${key}`).value = value;
    this.outline();
    this.layoutFrame(); this.inspector();
  },
  updatePreview() {
    document.body.classList.toggle("preview-mode", this.preview);
    if (byId("mode-label")) byId("mode-label").textContent = this.preview ? "Preview" : "Design";
    if (byId("preview-caption")) byId("preview-caption").textContent = this.preview ? "Back to design" : "Preview";
    this.state?.controls.forEach(item => this.applyAppearance(item));
    this.inspector(); this.outline();
  },
};

function canvasPoint(event) {
  const d = window.formaDesigner, root = d.root(), rect = root.getBoundingClientRect();
  return { x: Math.round((event.clientX - rect.left) / d.zoom - root.clientLeft),
    y: Math.round((event.clientY - rect.top) / d.zoom - root.clientTop) };
}

document.addEventListener("dragstart", event => {
  const tool = event.target.closest("[data-kind]");
  if (!tool || !supportedKinds.has(tool.dataset.kind) || window.formaDesigner.preview) return;
  event.dataTransfer.setData("text/plain", `forma:${tool.dataset.kind}`);
  event.dataTransfer.effectAllowed = "copy";
});
document.addEventListener("dragover", event => {
  const d = window.formaDesigner, root = d.root();
  if (d.preview || !root || !root.contains(event.target)) return;
  if (!Array.from(event.dataTransfer.types).includes("text/plain")) return;
  event.preventDefault(); event.dataTransfer.dropEffect = "copy"; root.classList.add("drop-target");
});
document.addEventListener("dragleave", event => {
  const root = window.formaDesigner.root();
  if (root && !root.contains(event.relatedTarget)) root.classList.remove("drop-target");
});
document.addEventListener("drop", event => {
  const d = window.formaDesigner, root = d.root(); root?.classList.remove("drop-target");
  if (d.preview || !root || !root.contains(event.target)) return;
  event.preventDefault();
  const text = event.dataTransfer.getData("text/plain");
  if (!text.startsWith("forma:")) return;
  const kind = text.slice(6);
  if (supportedKinds.has(kind)) send("drop", root.id, { control: kind, ...canvasPoint(event) });
});

document.addEventListener("pointerdown", event => {
  const d = window.formaDesigner, root = d.root();
  const handle = event.target.closest("[data-handle]");
  if (handle && !d.preview && event.button === 0) {
    const item = d.state?.controls.find(c => c.id === d.selectedId);
    if (!item) return;
    event.preventDefault(); event.stopImmediatePropagation();
    d.drag = { element: byId(item.id), capture: handle, mode: "resize", direction: handle.dataset.handle,
      pointerId: event.pointerId, startX: event.clientX, startY: event.clientY,
      x: item.x, y: item.y, width: item.width, height: item.height, moved: false };
    handle.setPointerCapture(event.pointerId); return;
  }
  if (!d.preview && event.button === 0 && event.target.closest(".form-titlebar")) {
    d.select(d.rootId); send("select", d.rootId); return;
  }
  if (d.preview || event.button !== 0 || !root?.contains(event.target)) return;
  const control = event.target.closest("[data-forma-type]");
  if (!control) return;
  event.preventDefault(); event.stopImmediatePropagation();
  d.select(control.id); send("select", control.id);
  if (control === root) return;
  const item = d.state?.controls.find(c => c.id === control.id);
  if (!item) return;
  d.drag = { element: control, pointerId: event.pointerId, startX: event.clientX, startY: event.clientY,
    x: item.x, y: item.y, width: item.width, height: item.height, moved: false, left: item.x, top: item.y };
  control.setPointerCapture(event.pointerId);
}, true);
document.addEventListener("pointermove", event => {
  const d = window.formaDesigner, drag = d.drag;
  if (!drag || drag.pointerId !== event.pointerId) return;
  const dx = (event.clientX - drag.startX) / d.zoom, dy = (event.clientY - drag.startY) / d.zoom;
  if (!drag.moved && Math.abs(dx) < 3 && Math.abs(dy) < 3) return;
  drag.moved = true;
  if (drag.mode === "resize") {
    drag.bounds = resizeBounds(drag, drag.direction, dx, dy, d.root().clientWidth, d.root().clientHeight, drag.element === d.root());
    Object.assign(drag.element.style, { left: `${drag.bounds.x}px`, top: `${drag.bounds.y}px`,
      width: `${drag.bounds.width}px`, height: `${drag.bounds.height}px` });
    for (const key of ["x", "y", "width", "height"]) if (byId(`prop-${key}`)) byId(`prop-${key}`).value = drag.bounds[key];
    d.layoutFrame(); d.outline(); d.coordinates(drag.bounds.x, drag.bounds.y); return;
  }
  drag.left = Math.max(0, Math.min(d.root().clientWidth - drag.width, Math.round(drag.x + dx)));
  drag.top = Math.max(0, Math.min(d.root().clientHeight - drag.height, Math.round(drag.y + dy)));
  Object.assign(drag.element.style, { left: `${drag.left}px`, top: `${drag.top}px` });
  d.coordinates(drag.left, drag.top); d.outline();
  if (byId("prop-x")) byId("prop-x").value = drag.left;
  if (byId("prop-y")) byId("prop-y").value = drag.top;
});
document.addEventListener("pointerup", event => {
  const d = window.formaDesigner, drag = d.drag;
  if (!drag || drag.pointerId !== event.pointerId) return;
  d.drag = null;
  const capture = drag.capture ?? drag.element;
  if (capture.hasPointerCapture(event.pointerId)) capture.releasePointerCapture(event.pointerId);
  if (drag.moved) send(drag.mode === "resize" ? "resize" : "move", drag.element.id,
    drag.mode === "resize" ? drag.bounds : { x: drag.left, y: drag.top });
});
document.addEventListener("pointercancel", () => window.formaDesigner.cancelDrag());
document.addEventListener("lostpointercapture", () => window.formaDesigner.cancelDrag());

document.addEventListener("click", event => {
  const d = window.formaDesigner, root = d.root();
  if (event.target.closest("[data-handle]")) { event.preventDefault(); event.stopImmediatePropagation(); return; }
  if (root?.contains(event.target)) {
    if (d.preview) return;
    const control = event.target.closest("[data-forma-type]");
    if (control) { event.preventDefault(); event.stopImmediatePropagation(); send("select", control.id); }
    return;
  }
  const command = event.target.closest("[data-command]");
  if (command && !command.disabled) send("command", d.rootId, { command: command.dataset.command });
  const toggle = event.target.closest("[data-toggle]");
  if (toggle) { const panel = byId(toggle.dataset.toggle); panel.hidden = !panel.hidden; }
  if (event.target.closest("[data-preview]")) { d.cancelDrag(); d.preview = !d.preview; d.updatePreview(); }
  document.querySelectorAll(".menubar details").forEach(menu => { if (!menu.contains(event.target) || command || toggle) menu.open = false; });
}, true);
document.addEventListener("dblclick", event => {
  const d = window.formaDesigner, tool = event.target.closest("[data-kind]");
  if (tool && !d.preview) send("drop", d.rootId, { control: tool.dataset.kind, x: 32, y: 32 });
});
document.addEventListener("change", event => {
  const d = window.formaDesigner, input = event.target;
  if (input.id === "zoom") {
    d.cancelDrag(); d.zoom = Number(input.value);
    d.layoutFrame();
  } else if (input.id === "selection") send("select", input.value);
  else if (input.dataset.property && !d.preview) {
    const value = input.type === "checkbox" ? input.checked : input.type === "number" ? Number(input.value) : input.value;
    send("property", d.selectedId, { property: input.dataset.property, value });
  }
});
document.addEventListener("input", event => {
  const d = window.formaDesigner;
  if (event.target.id === "prop-text" && !d.preview) {
    send("property", d.selectedId, { property: "text", value: event.target.value }); return;
  }
  if (event.target.id !== "search") return;
  const query = event.target.value.trim().toLowerCase();
  document.querySelectorAll(".tool-group button").forEach(tool => { tool.hidden = !tool.textContent.toLowerCase().includes(query); });
});
document.addEventListener("keydown", event => {
  const d = window.formaDesigner;
  if (event.key === "Escape") { d.cancelDrag(); if (d.preview) { d.preview = false; d.updatePreview(); } return; }
  if (event.target.closest("input,select,textarea")) return;
  if (event.ctrlKey && event.key.toLowerCase() === "n") { event.preventDefault(); send("command", d.rootId, { command: "new" }); }
  if (d.preview) return;
  if (event.key === "Delete") { event.preventDefault(); send("command", d.rootId, { command: "delete" }); }
  const delta = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] }[event.key];
  const item = d.state?.controls.find(c => c.id === d.selectedId);
  if (delta && item && item.id !== d.rootId) {
    event.preventDefault(); const step = event.shiftKey ? 10 : 1;
    send("move", item.id, { x: item.x + delta[0] * step, y: item.y + delta[1] * step });
  }
});
