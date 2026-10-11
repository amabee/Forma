// Multi-selection on the design canvas: Ctrl/Shift-click, drag-select (marquee), Ctrl+A,
// moving several controls together, align/distribute, and clipboard shortcuts.
// Listeners run on window in the capture phase, ahead of designer.js's single-selection
// handlers, and only take over when more than one control is involved.
(() => {
  const d = window.formaDesigner;
  if (!d || d.standalone) return;
  const send = (event, id, payload = {}) => window.forma.send({ type: "designer", id, event, payload });
  const DRAG_THRESHOLD = 3;
  let ids = [];
  let gesture = null;
  // The click that follows a handled pointer gesture would make designer.js reselect a
  // single control, so it is swallowed.
  let swallowClick = false;

  const item = id => d.controlById(id);
  const element = id => document.getElementById(id);
  const editing = target => target?.closest?.("input, textarea, select, [contenteditable=true], [contenteditable=''], .cm-editor");
  // Controls the user can position freely: layouts, Dock and locks decide otherwise.
  const movable = control => control && control.id !== d.rootId && !control.locked && !control.component
    && !controlledAxes(control).position;

  function select(nextIds, primary = nextIds.at(-1)) {
    ids = nextIds.length ? [...new Set(nextIds)] : [d.rootId];
    primary = ids.includes(primary) ? primary : ids.at(-1);
    d.select(primary);
    send("select", primary, { ids });
  }

  function paint() {
    const selected = new Set(ids.length > 1 ? ids : []);
    document.querySelectorAll(".forma-multi-selected").forEach(el => { if (!selected.has(el.id)) el.classList.remove("forma-multi-selected"); });
    for (const id of selected) element(id)?.classList.add("forma-multi-selected");
    const positioned = ids.map(item).filter(movable);
    const sameParent = positioned.filter(control => control.parentId === positioned[0]?.parentId);
    document.querySelectorAll("[data-align]").forEach(button => {
      const needed = button.dataset.align.startsWith("distribute") ? 3 : 2;
      button.disabled = d.preview || sameParent.length < needed;
    });
    document.querySelector(".align-tools")?.toggleAttribute("hidden", ids.length < 2);
    document.querySelectorAll('[data-selection-command="paste"]').forEach(button => { button.disabled = d.preview || !d.state?.canPaste; });
    document.querySelectorAll('[data-selection-command]:not([data-selection-command="paste"])').forEach(button => {
      button.disabled = d.preview || !ids.some(id => id !== d.rootId && !item(id)?.locked);
    });
    const status = document.getElementById("selection-status");
    if (status && ids.length > 1) status.textContent = `${ids.length} controls selected`;
  }

  // designer.js calls these: after each state update, and when it selects a single control itself.
  function sync(message) {
    const known = new Set(message.controls.map(control => control.id));
    ids = (message.selectedIds ?? [message.selectedId]).filter(id => known.has(id));
    if (!ids.length) ids = [message.selectedId];
    paint();
  }
  function primaryChanged(id) {
    if (!ids.includes(id)) ids = [id];
    paint();
  }

  function selectAll() {
    const all = (d.state?.controls ?? []).filter(control => control.parentId === d.rootId && !control.component).map(control => control.id);
    if (all.length) select(all, all.includes(d.selectedId) ? d.selectedId : undefined);
  }

  function command(name) {
    const targets = ids.filter(id => id !== d.rootId);
    send("command", d.rootId, { command: name, ids: targets.length ? targets : [d.selectedId] });
  }

  function moveBy(dx, dy) {
    const moves = ids.map(item).filter(movable).map(control => ({ id: control.id, x: Math.max(0, control.x + dx), y: Math.max(0, control.y + dy) }));
    if (moves.length) send("move-many", d.rootId, { moves });
  }

  /** Aligns or evenly spaces the selected controls that share the primary's parent. */
  function align(kind) {
    const all = ids.map(item).filter(movable);
    const parentId = (item(d.selectedId) && movable(item(d.selectedId)) ? item(d.selectedId) : all[0])?.parentId;
    const group = all.filter(control => control.parentId === parentId);
    if (group.length < 2) return;
    const left = Math.min(...group.map(c => c.x)), right = Math.max(...group.map(c => c.x + c.width));
    const top = Math.min(...group.map(c => c.y)), bottom = Math.max(...group.map(c => c.y + c.height));
    const place = {
      left: c => ({ x: left }), right: c => ({ x: right - c.width }), center: c => ({ x: Math.round((left + right - c.width) / 2) }),
      top: c => ({ y: top }), bottom: c => ({ y: bottom - c.height }), middle: c => ({ y: Math.round((top + bottom - c.height) / 2) }),
    }[kind];
    let moves;
    if (place) moves = group.map(c => ({ id: c.id, x: c.x, y: c.y, ...place(c) }));
    else {
      if (group.length < 3) return;
      const horizontal = kind === "distribute-horizontal";
      const ordered = [...group].sort((a, b) => horizontal ? a.x - b.x : a.y - b.y);
      const size = c => horizontal ? c.width : c.height, start = c => horizontal ? c.x : c.y;
      const span = start(ordered.at(-1)) + size(ordered.at(-1)) - start(ordered[0]);
      const gap = (span - ordered.reduce((sum, c) => sum + size(c), 0)) / (ordered.length - 1);
      let position = start(ordered[0]);
      moves = ordered.map(c => {
        const move = { id: c.id, x: c.x, y: c.y, [horizontal ? "x" : "y"]: Math.round(position) };
        position += size(c) + gap;
        return move;
      });
    }
    send("move-many", d.rootId, { moves });
  }

  window.addEventListener("pointerdown", event => {
    const root = d.root();
    swallowClick = false;
    if (d.preview || event.button !== 0 || !root) return;
    const control = event.target.closest?.("[data-component]") ?? (root.contains(event.target) ? event.target.closest("[data-forma-type]") : null);
    if (!control || event.target.closest("[data-handle], [data-tab-index], [data-tab-add]")) return;
    if (control.closest('[data-forma-type="richtextbox"]')?.dataset.editing === "true") return;
    const additive = event.ctrlKey || event.shiftKey || event.metaKey;
    if (additive && control !== root) {
      event.preventDefault(); event.stopImmediatePropagation();
      swallowClick = true;
      const current = ids.filter(id => id !== d.rootId);
      select(current.includes(control.id) ? current.filter(id => id !== control.id) : [...current, control.id], current.includes(control.id) ? undefined : control.id);
      return;
    }
    if (control !== root && ids.length > 1 && ids.includes(control.id)) {
      // Pressing on part of a multi-selection drags the whole group.
      event.preventDefault(); event.stopImmediatePropagation();
      swallowClick = true;
      gesture = { type: "group", pointerId: event.pointerId, startX: event.clientX, startY: event.clientY, target: control.id,
        items: ids.map(item).filter(movable), moved: false };
      return;
    }
    if (control === root && !event.target.closest(".form-titlebar"))
      gesture = { type: "marquee", pointerId: event.pointerId, startX: event.clientX, startY: event.clientY, additive, moved: false };
  }, true);

  window.addEventListener("pointermove", event => {
    if (!gesture || gesture.pointerId !== event.pointerId) return;
    const dx = event.clientX - gesture.startX, dy = event.clientY - gesture.startY;
    if (!gesture.moved && Math.abs(dx) < DRAG_THRESHOLD && Math.abs(dy) < DRAG_THRESHOLD) return;
    gesture.moved = true;
    if (gesture.type === "group") {
      for (const control of gesture.items) element(control.id).style.transform = `translate(${dx / d.zoom}px, ${dy / d.zoom}px)`;
      d.outline();
      return;
    }
    const root = d.root(), rect = root.getBoundingClientRect();
    let box = document.getElementById("selection-marquee");
    if (!box) {
      box = document.createElement("div"); box.id = "selection-marquee"; box.setAttribute("aria-hidden", "true");
      root.append(box);
    }
    const x1 = Math.min(event.clientX, gesture.startX), y1 = Math.min(event.clientY, gesture.startY);
    gesture.rect = { left: x1, top: y1, right: Math.max(event.clientX, gesture.startX), bottom: Math.max(event.clientY, gesture.startY) };
    Object.assign(box.style, {
      left: `${(x1 - rect.left) / d.zoom - root.clientLeft}px`, top: `${(y1 - rect.top) / d.zoom - root.clientTop}px`,
      width: `${(gesture.rect.right - x1) / d.zoom}px`, height: `${(gesture.rect.bottom - y1) / d.zoom}px`,
    });
  }, true);

  function finish(event, cancelled) {
    if (!gesture || gesture.pointerId !== event.pointerId) return;
    const current = gesture; gesture = null;
    document.getElementById("selection-marquee")?.remove();
    if (current.type === "group") {
      for (const control of current.items) element(control.id).style.transform = "";
      if (cancelled) { d.outline(); return; }
      if (!current.moved) { select([current.target]); return; }
      const dx = Math.round((event.clientX - current.startX) / d.zoom), dy = Math.round((event.clientY - current.startY) / d.zoom);
      send("move-many", d.rootId, { moves: current.items.map(control => ({ id: control.id, x: Math.max(0, control.x + dx), y: Math.max(0, control.y + dy) })) });
      return;
    }
    if (cancelled || !current.moved || !current.rect) return;
    swallowClick = true;
    // Drag-select picks the form's direct children that the rectangle touches.
    const hits = (d.state?.controls ?? []).filter(control => control.parentId === d.rootId && !control.component && control.visible !== false)
      .filter(control => {
        const box = element(control.id)?.getBoundingClientRect();
        return box && box.left < current.rect.right && box.right > current.rect.left && box.top < current.rect.bottom && box.bottom > current.rect.top;
      }).map(control => control.id);
    if (hits.length) select(current.additive ? [...ids.filter(id => id !== d.rootId), ...hits] : hits);
  }
  window.addEventListener("pointerup", event => finish(event, false), true);
  window.addEventListener("click", event => {
    if (!swallowClick) return;
    swallowClick = false;
    event.preventDefault(); event.stopImmediatePropagation();
  }, true);
  window.addEventListener("pointercancel", event => finish(event, true), true);

  window.addEventListener("keydown", event => {
    if (d.preview || editing(event.target) || event.target.closest?.("aside, nav, [role=menu], [role=tree], [role=dialog]")) return;
    const key = event.key.toLowerCase(), ctrl = event.ctrlKey || event.metaKey;
    const handled = () => { event.preventDefault(); event.stopImmediatePropagation(); };
    if (ctrl && !event.shiftKey && !event.altKey && ["a", "c", "x", "v", "d"].includes(key)) {
      handled();
      if (key === "a") selectAll();
      else command({ c: "copy", x: "cut", v: "paste", d: "duplicate" }[key]);
      return;
    }
    if (ids.length < 2) return;
    if (event.key === "Delete") { handled(); command("delete"); return; }
    if (event.key === "Escape") { select([d.selectedId]); return; }
    const delta = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] }[event.key];
    if (delta) { handled(); const step = event.shiftKey ? 10 : 1; moveBy(delta[0] * step, delta[1] * step); }
  }, true);

  document.addEventListener("click", event => {
    const alignButton = event.target.closest?.("[data-align]");
    if (alignButton && !alignButton.disabled) { align(alignButton.dataset.align); return; }
    const button = event.target.closest?.("[data-selection-command]");
    if (!button || button.disabled) return;
    if (button.dataset.selectionCommand === "select-all") selectAll();
    else command(button.dataset.selectionCommand);
    document.querySelectorAll(".menubar details[open]").forEach(menu => { menu.open = false; });
  });

  window.formaSelection = { sync, primaryChanged, select, selectAll, align, get ids() { return [...ids]; } };
})();
