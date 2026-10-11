// View > Show grid (with snap to grid) and View > Show tab order. Both are remembered.
(() => {
  const d = window.formaDesigner;
  if (!d || d.standalone) return;
  const GRID = 8;
  // Containers and decorations never take keyboard focus, so they get no tab-order badge.
  const UNFOCUSABLE = new Set(["form", "panel", "groupbox", "card", "splitcontainer", "tabcontrol", "flowlayoutpanel", "tablelayoutpanel",
    "accordion", "sidebar", "appshell", "responsivepanel", "responsivegrid", "responsivestack", "breakpointcontainer", "aspectratiocontainer",
    "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel", "scrollablepanel", "label", "image", "picturebox", "icon", "divider",
    "skeleton", "spinner", "badge", "avatar", "emptystate", "progressbar", "circularprogress", "statusbar"]);
  const read = key => { try { return localStorage.getItem(key) === "true"; } catch { return false; } };
  const write = (key, value) => { try { localStorage.setItem(key, String(value)); } catch {} };
  const options = { grid: read("forma.view.grid"), tabOrder: read("forma.view.tabOrder"), gridSize: GRID };

  function paintButtons() {
    document.querySelectorAll("[data-view-option]").forEach(button => {
      const on = options[button.dataset.viewOption];
      button.setAttribute("aria-pressed", String(on));
      button.querySelector(".view-check")?.replaceChildren(document.createTextNode(on ? "✓" : ""));
    });
  }

  function renderGrid() {
    d.root()?.classList.toggle("show-grid", options.grid && !d.preview);
  }

  /** Numbered badges in keyboard order: tabIndex first, then document order. */
  function renderTabOrder() {
    const root = d.root();
    root?.querySelector(".tab-order-layer")?.remove();
    if (!root || !options.tabOrder || d.preview) return;
    const layer = document.createElement("div");
    layer.className = "tab-order-layer"; layer.setAttribute("aria-hidden", "true");
    const order = [...root.querySelectorAll("[data-forma-type]")].map((element, position) => ({ element, position, item: d.controlById(element.id) }))
      .filter(({ item, element }) => item && !UNFOCUSABLE.has(item.kind) && !item.component && item.visible !== false && item.focusable !== false && item.enabled !== false && !element.closest("[hidden]"))
      .sort((a, b) => (a.item.tabIndex ?? 0) - (b.item.tabIndex ?? 0) || a.position - b.position);
    const origin = root.getBoundingClientRect();
    order.forEach(({ element }, index) => {
      const box = element.getBoundingClientRect(), badge = document.createElement("span");
      badge.className = "tab-order-badge"; badge.textContent = String(index + 1);
      badge.style.left = `${(box.left - origin.left) / d.zoom - root.clientLeft}px`;
      badge.style.top = `${(box.top - origin.top) / d.zoom - root.clientTop}px`;
      layer.append(badge);
    });
    root.append(layer);
  }

  function refresh() { renderGrid(); renderTabOrder(); paintButtons(); }

  document.addEventListener("click", event => {
    const button = event.target.closest?.("[data-view-option]");
    if (!button) return;
    const key = button.dataset.viewOption;
    options[key] = !options[key];
    write(key === "grid" ? "forma.view.grid" : "forma.view.tabOrder", options[key]);
    document.querySelectorAll(".menubar details[open]").forEach(menu => { menu.open = false; });
    refresh();
  });

  // Badges follow every design change (moves, resizes, new controls, zoom, preview).
  const receive = d.receive.bind(d);
  const later = callback => (window.requestAnimationFrame ? requestAnimationFrame(callback) : callback());
  d.receive = message => { const result = receive(message); if (["state", "initialize"].includes(message?.action)) later(refresh); return result; };
  const updatePreview = d.updatePreview.bind(d);
  d.updatePreview = () => { updatePreview(); refresh(); };
  document.getElementById("zoom")?.addEventListener("change", () => later(refresh));

  /** Rounds a dragged position to the grid on each axis an alignment guide did not claim. */
  function snap(x, y, guides) {
    if (!options.grid) return { x, y };
    const claimed = new Set(guides.map(guide => guide.axis));
    return { x: claimed.has("x") ? x : Math.round(x / GRID) * GRID, y: claimed.has("y") ? y : Math.round(y / GRID) * GRID };
  }

  window.formaView = Object.assign(options, { snap, refresh });
  paintButtons();
})();
