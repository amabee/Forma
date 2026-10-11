// Keyboard and screen-reader support for the Builder chrome:
// - the top menus behave as menus (roles, arrow keys, Left/Right between menus);
// - the design surface is focusable, and Tab / Shift+Tab move the selection through the
//   form's controls in keyboard order, leaving the surface after the last one (no trap).
(() => {
  const d = window.formaDesigner;
  if (!d || d.standalone) return;
  const send = (event, id, payload = {}) => window.forma.send({ type: "designer", id, event, payload });

  // Top menus
  const menus = [...document.querySelectorAll(".menubar details")];
  for (const menu of menus) {
    const summary = menu.querySelector("summary"), list = menu.querySelector(".menu");
    summary.setAttribute("aria-haspopup", "menu");
    summary.setAttribute("aria-expanded", String(menu.open));
    list?.setAttribute("role", "menu");
    list?.setAttribute("aria-label", summary.textContent.trim());
    list?.querySelectorAll("button").forEach(button => button.setAttribute("role", button.hasAttribute("aria-pressed") ? "menuitemcheckbox" : "menuitem"));
    list?.querySelectorAll("hr").forEach(rule => rule.setAttribute("role", "separator"));
    menu.addEventListener("toggle", () => summary.setAttribute("aria-expanded", String(menu.open)));
  }
  // Checkable View items report checked state the menu way.
  const syncChecks = () => document.querySelectorAll('.menubar [role="menuitemcheckbox"]').forEach(item => item.setAttribute("aria-checked", item.getAttribute("aria-pressed") ?? "false"));
  syncChecks();
  new MutationObserver(syncChecks).observe(document.querySelector(".menubar") ?? document.body, { subtree: true, attributes: true, attributeFilter: ["aria-pressed"] });

  const items = menu => [...menu.querySelectorAll(".menu button:not(:disabled)")];
  function openMenu(menu, focus = "first") {
    menus.forEach(other => { if (other !== menu) other.open = false; });
    menu.open = true;
    const list = items(menu);
    (focus === "last" ? list.at(-1) : list[0])?.focus();
  }
  document.querySelector(".menubar")?.addEventListener("keydown", event => {
    const menu = event.target.closest("details");
    if (!menu) return;
    const index = menus.indexOf(menu);
    if (event.target.matches("summary") && ["ArrowDown", "ArrowUp"].includes(event.key)) {
      event.preventDefault(); openMenu(menu, event.key === "ArrowUp" ? "last" : "first"); return;
    }
    if (["ArrowLeft", "ArrowRight"].includes(event.key)) {
      event.preventDefault();
      const next = menus[(index + (event.key === "ArrowRight" ? 1 : -1) + menus.length) % menus.length];
      if (menu.open) openMenu(next); else next.querySelector("summary").focus();
      return;
    }
    if (!event.target.closest(".menu")) return;
    const list = items(menu), position = list.indexOf(document.activeElement);
    const target = { ArrowDown: position + 1, ArrowUp: position - 1, Home: 0, End: list.length - 1 }[event.key];
    if (target === undefined) return;
    event.preventDefault();
    list[(target + list.length) % list.length]?.focus();
  });

  // Design surface
  const surfaceOrder = () => (d.state?.controls ?? [])
    .filter(item => item.parentId && !item.component && item.visible !== false && document.getElementById(item.id) && !document.getElementById(item.id).closest("[hidden]"))
    .map((item, position) => ({ item, position }))
    .sort((a, b) => (a.item.tabIndex ?? 0) - (b.item.tabIndex ?? 0) || a.position - b.position)
    .map(entry => entry.item.id);

  function prepareSurface() {
    const root = d.root();
    if (!root) return;
    // designer.js keeps controls out of the Tab order in design mode; the surface itself takes focus.
    root.tabIndex = d.preview ? -1 : 0;
    if (!d.preview) {
      root.setAttribute("aria-label", "Design surface. Tab and Shift+Tab move the selection between controls.");
      root.setAttribute("aria-roledescription", "design surface");
    }
  }

  window.addEventListener("keydown", event => {
    const root = d.root();
    if (event.key !== "Tab" || event.ctrlKey || event.altKey || d.preview || !root || document.activeElement !== root) return;
    const order = surfaceOrder();
    if (!order.length) return;
    const current = order.indexOf(d.selectedId);
    const next = current < 0 ? (event.shiftKey ? order.length - 1 : 0) : current + (event.shiftKey ? -1 : 1);
    // Past either end, let Tab leave the surface normally instead of trapping focus.
    if (next < 0 || next >= order.length) return;
    event.preventDefault(); event.stopImmediatePropagation();
    d.select(order[next]);
    send("select", order[next]);
    document.getElementById(order[next])?.scrollIntoView?.({ block: "nearest", inline: "nearest" });
  }, true);

  const receive = d.receive.bind(d);
  d.receive = message => { const result = receive(message); if (["state", "initialize"].includes(message?.action)) prepareSurface(); return result; };
  const updatePreview = d.updatePreview.bind(d);
  d.updatePreview = () => { updatePreview(); prepareSurface(); };
  prepareSurface();
})();
