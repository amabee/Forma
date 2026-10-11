// Canvas navigation: zoom with Ctrl+wheel / Ctrl+= / Ctrl+- / Ctrl+0 and "Fit", a clickable
// path from the selected control up to its form, and resizing the selection from the keyboard.
(() => {
  const d = window.formaDesigner;
  const zoom = document.getElementById("zoom"), stage = document.getElementById("stage");
  if (!d || d.standalone || !zoom) return;
  const send = (event, id, payload = {}) => window.forma.send({ type: "designer", id, event, payload });
  const STEPS = [0.5, 0.67, 0.75, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2];
  const MIN = 0.25, MAX = 2;

  /** Applies a zoom level through the zoom picker, so designer.js handles it in one place. */
  function setZoom(value) {
    const level = Math.round(Math.min(MAX, Math.max(MIN, value)) * 100) / 100;
    let option = [...zoom.options].find(item => item.value !== "fit" && Number(item.value) === level);
    if (!option) {
      // Levels between the presets get one reusable custom entry.
      option = zoom.querySelector("option[data-custom]") ?? zoom.appendChild(Object.assign(document.createElement("option"), { hidden: true }));
      option.dataset.custom = "true";
      option.value = String(level);
      option.textContent = `${Math.round(level * 100)}%`;
    }
    zoom.value = option.value;
    zoom.dispatchEvent(new Event("change", { bubbles: true }));
  }

  function fitLevel() {
    const form = d.controlById(d.rootId);
    if (!stage || !form) return 1;
    // The frame adds a 2px border and a 34px title bar around the form (see designer.js layoutFrame);
    // the stage's own padding is not room for the form either.
    const style = getComputedStyle(stage), pad = side => parseFloat(style[`padding${side}`]) || 0;
    const width = stage.clientWidth - pad("Left") - pad("Right") - 48, height = stage.clientHeight - pad("Top") - pad("Bottom") - 48;
    return Math.min(width / (form.width + 2), height / (form.height + 36));
  }

  function step(direction) {
    const current = d.zoom;
    const next = direction > 0 ? STEPS.find(level => level > current + 0.001) : [...STEPS].reverse().find(level => level < current - 0.001);
    setZoom(next ?? (direction > 0 ? MAX : MIN));
  }

  // "Fit" resolves to a number before designer.js reads the picker.
  window.addEventListener("change", event => {
    if (event.target === zoom && zoom.value === "fit") setZoom(fitLevel());
  }, true);

  stage?.addEventListener("wheel", event => {
    if (!event.ctrlKey || d.preview) return;
    event.preventDefault();
    step(event.deltaY < 0 ? 1 : -1);
  }, { passive: false });

  window.addEventListener("keydown", event => {
    if (d.preview || event.target.closest?.("input, textarea, select, [contenteditable=true], .cm-editor")) return;
    if ((event.ctrlKey || event.metaKey) && !event.altKey && ["=", "+", "-", "0"].includes(event.key)) {
      event.preventDefault(); event.stopImmediatePropagation();
      if (event.key === "0") setZoom(1); else step(event.key === "-" ? -1 : 1);
      return;
    }
    // Alt+Shift+Arrow resizes the selected control by 1px, adding Ctrl resizes by 10px.
    const delta = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] }[event.key];
    if (!delta || !event.altKey || !event.shiftKey) return;
    const item = d.controlById(d.selectedId);
    if (!item || item.locked || item.component) return;
    event.preventDefault(); event.stopImmediatePropagation();
    const controlled = controlledAxes(item), amount = event.ctrlKey ? 10 : 1;
    const width = controlled.width ? item.width : Math.max(24, item.width + delta[0] * amount);
    const height = controlled.height ? item.height : Math.max(20, item.height + delta[1] * amount);
    if (width !== item.width || height !== item.height) send("resize", item.id, { width, height, x: item.x, y: item.y });
  }, true);

  // Selection path: Form › Panel › Card › Button, each segment selects that control.
  const path = document.getElementById("selection-path");
  function renderPath(id) {
    if (!path) return;
    const chain = [];
    for (let item = d.controlById(id); item; item = d.controlById(item.parentId)) chain.unshift(item);
    path.hidden = chain.length < 2;
    path.replaceChildren(...chain.flatMap((item, index) => {
      const button = document.createElement("button");
      button.type = "button"; button.textContent = item.name || item.kind; button.title = item.kind;
      if (index === chain.length - 1) button.setAttribute("aria-current", "true");
      button.addEventListener("click", () => { d.select(item.id); send("select", item.id); });
      if (index === 0) return [button];
      const separator = document.createElement("span"); separator.textContent = "›"; separator.setAttribute("aria-hidden", "true");
      return [separator, button];
    }));
  }

  // Form size presets from the toolbar; one resize edit, so a single undo step.
  const PRESETS = [
    ["Desktop", 1280, 800], ["Laptop", 1024, 640], ["Window", 800, 600], ["Small window", 640, 440],
    ["Dialog", 480, 360], ["Tablet", 768, 1024], ["Phone", 390, 844],
  ];
  const deviceName = (width, height) => PRESETS.find(([, w, h]) => w === width && h === height)?.[0] ?? "Custom";
  const deviceButton = document.getElementById("device-button"), deviceMenu = document.getElementById("device-menu");
  function setDeviceMenu(open) {
    if (!deviceMenu) return;
    deviceMenu.hidden = !open;
    deviceButton.setAttribute("aria-expanded", String(open));
    if (!open) return;
    const form = d.controlById(d.rootId);
    deviceMenu.replaceChildren(...PRESETS.map(([name, width, height]) => {
      const item = document.createElement("button");
      item.type = "button"; item.setAttribute("role", "menuitemradio");
      item.setAttribute("aria-checked", String(form?.width === width && form?.height === height));
      item.append(Object.assign(document.createElement("span"), { textContent: name }), Object.assign(document.createElement("small"), { textContent: `${width} × ${height}` }));
      item.disabled = d.preview || !!form?.locked;
      item.addEventListener("click", () => { setDeviceMenu(false); send("resize", d.rootId, { width, height }); });
      return item;
    }));
    const rect = deviceButton.getBoundingClientRect();
    Object.assign(deviceMenu.style, { left: `${Math.max(8, rect.right - 220)}px`, top: `${rect.bottom + 4}px` });
    deviceMenu.querySelector("button:not(:disabled)")?.focus();
  }
  deviceButton?.addEventListener("click", () => setDeviceMenu(deviceMenu.hidden));
  deviceMenu?.addEventListener("keydown", event => {
    const items = [...deviceMenu.querySelectorAll("button:not(:disabled)")], index = items.indexOf(document.activeElement);
    if (event.key === "Escape") { event.stopPropagation(); setDeviceMenu(false); deviceButton.focus(); }
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      items[(index + (event.key === "ArrowDown" ? 1 : -1) + items.length) % items.length]?.focus();
    }
  });
  document.addEventListener("pointerdown", event => {
    if (deviceMenu && !deviceMenu.hidden && !deviceMenu.contains(event.target) && !deviceButton.contains(event.target)) setDeviceMenu(false);
  });

  window.formaCanvas = { setZoom, fitLevel, renderPath, deviceName };
})();
