(() => {
  const designer = window.formaDesigner;
  const panel = document.getElementById("solution-explorer"), toolbox = document.getElementById("toolbox");
  const tree = document.getElementById("explorer-tree"), search = document.getElementById("explorer-search");
  if (!panel || !tree) return;
  let root, selectedId, activeForm, signature, selectedKey;
  const expanded = new Map(), searchCollapsed = new Set(), nodes = new Map();
  const icons = { project: "panels-top-left", folder: "folder-open", "project-folder": "folder-open", form: "panel-top", component: "rectangle-horizontal", css: "palette", javascript: "file-plus", json: "list", image: "image" };
  function icon(name) {
    const image = document.createElement("img"); image.className = "icon-svg";
    image.src = `icons/${icons[name] ?? name}.svg`; image.alt = ""; return image;
  }
  function showPanel(id, persist = true) {
    panel.hidden = id !== panel.id; toolbox.hidden = id !== toolbox.id;
    document.querySelectorAll("[data-left-panel]").forEach(button => {
      const active = button.dataset.leftPanel === id;
      button.setAttribute("aria-selected", String(active)); button.tabIndex = active ? 0 : -1;
    });
    if (persist) try { localStorage.setItem("forma.workspace.leftPanel", id); } catch {}
    if (id === panel.id) render();
    window.formaWorkspace?.measure();
  }
  document.querySelectorAll("[data-left-panel]").forEach(button => button.addEventListener("click", () => showPanel(button.dataset.leftPanel)));
  document.addEventListener("click", event => {
    const toggle = event.target.closest("[data-toggle]");
    if (toggle?.dataset.toggle === panel.id) showPanel(panel.hidden ? toolbox.id : panel.id);
    if (toggle?.dataset.toggle === toolbox.id) showPanel(toolbox.hidden ? panel.id : toolbox.id);
  });
  document.querySelector(".left-panel-tabs").addEventListener("keydown", event => {
    if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
    event.preventDefault(); event.stopPropagation();
    const id = event.key === "Home" ? panel.id : event.key === "End" ? toolbox.id : panel.hidden ? panel.id : toolbox.id;
    showPanel(id); document.querySelector(`[data-left-panel="${id}"]`).focus();
  });
  function isExpanded(node) {
    if (search.value.trim()) return !searchCollapsed.has(node.key);
    return expanded.get(node.key) ?? (node.kind === "project" || node.key === "forms" || node.key === activeForm);
  }
  function selectRows() {
    tree.querySelectorAll(".explorer-row").forEach(row => {
      const node = nodes.get(row.dataset.explorerKey);
      row.setAttribute("aria-selected", String(selectedKey ? node.key === selectedKey : node.controlId === selectedId && !node.command && node.kind !== "image"));
    });
  }
  function render(focusKey) {
    if (!root) return;
    const focus = focusKey ?? document.activeElement?.closest("[data-explorer-key]")?.dataset.explorerKey;
    const needle = search.value.trim().toLowerCase(), matches = new Map();
    function scan(node) {
      const own = node.name.toLowerCase().includes(needle);
      const children = (node.children ?? []).map(scan);
      const result = own || children.some(Boolean); matches.set(node.key, result); return result;
    }
    scan(root); nodes.clear();
    function item(node, level, parentMatch = false) {
      if (needle && !parentMatch && !matches.get(node.key)) return null;
      nodes.set(node.key, node);
      const li = document.createElement("li"); li.setAttribute("role", "none");
      const row = document.createElement("div"); row.className = "explorer-row";
      row.dataset.explorerKey = node.key; row.setAttribute("role", "treeitem"); row.setAttribute("aria-level", String(level)); row.tabIndex = -1;
      if (node.command && node.locked) row.setAttribute("aria-disabled", "true");
      row.title = node.command ? `${node.name}${node.locked ? " — component is locked" : " — open source"}` : node.name;
      const chevron = document.createElement("button"); chevron.className = "explorer-chevron"; chevron.tabIndex = -1;
      const children = node.children ?? [], open = isExpanded(node);
      if (children.length) {
        row.setAttribute("aria-expanded", String(open));
        chevron.dataset.expandExplorer = node.key;
        chevron.setAttribute("aria-label", `${open ? "Collapse" : "Expand"} ${node.name}`);
        const image = document.createElement("img"); image.src = `icons/chevron-${open ? "down" : "right"}.svg`; image.alt = ""; chevron.append(image);
      } else { chevron.disabled = true; chevron.setAttribute("aria-hidden", "true"); }
      const label = document.createElement("span"); label.className = "explorer-label"; label.textContent = node.name;
      row.append(chevron, icon(node.kind), label);
      if (node.locked) { const lock = icon("lock"); lock.classList.add("explorer-lock"); row.append(lock); }
      li.append(row);
      if (children.length) {
        const group = document.createElement("ul"); group.setAttribute("role", "group"); group.hidden = !open;
        for (const child of children) { const next = item(child, level + 1, parentMatch || (!!needle && node.name.toLowerCase().includes(needle))); if (next) group.append(next); }
        li.append(group);
      }
      return li;
    }
    const content = item(root, 1); tree.replaceChildren(...(content ? [content] : []));
    document.getElementById("explorer-empty").hidden = !!content;
    selectRows();
    const rows = visibleRows();
    const target = rows.find(row => row.dataset.explorerKey === focus) ?? rows.find(row => row.getAttribute("aria-selected") === "true") ?? rows[0];
    if (target) { target.tabIndex = 0; if (focus) target.focus(); }
  }
  function visibleRows() { return [...tree.querySelectorAll(".explorer-row")].filter(row => !row.closest("[hidden]")); }
  function toggle(key) {
    const node = nodes.get(key); if (!node?.children?.length) return;
    if (search.value.trim()) { if (searchCollapsed.has(key)) searchCollapsed.delete(key); else searchCollapsed.add(key); }
    else expanded.set(key, !isExpanded(node));
    render(key);
  }
  function open(node) {
    if (node.command && node.locked) return;
    if (typeof canSaveGridDrafts === "function" && !canSaveGridDrafts()) return;
    selectedKey = node.key; selectRows();
    if (node.command === "open-project-file") { explorerAction(node, "open-project-file"); return; }
    if (node.command) window.forma.send({ type: "designer", event: "command", id: node.controlId, payload: { command: node.command } });
    else if (node.controlId) window.forma.send({ type: "designer", event: "explorer-select", id: node.controlId, payload: {} });
    else toggle(node.key);
  }
  tree.addEventListener("click", event => {
    const chevron = event.target.closest("[data-expand-explorer]");
    if (chevron) { toggle(chevron.dataset.expandExplorer); return; }
    const row = event.target.closest("[data-explorer-key]");
    if (row) { visibleRows().forEach(item => item.tabIndex = -1); row.tabIndex = 0; row.focus(); open(nodes.get(row.dataset.explorerKey)); }
  });
  tree.addEventListener("keydown", event => {
    const row = event.target.closest("[data-explorer-key]"); if (!row) return;
    if (!["ArrowDown", "ArrowUp", "ArrowLeft", "ArrowRight", "Home", "End", "Enter", " "].includes(event.key)) return;
    event.preventDefault(); event.stopPropagation();
    const node = nodes.get(row.dataset.explorerKey), rows = visibleRows(), index = rows.indexOf(row);
    let target;
    if (event.key === "Enter" || event.key === " ") { open(node); return; }
    if (event.key === "ArrowDown") target = rows[Math.min(index + 1, rows.length - 1)];
    if (event.key === "ArrowUp") target = rows[Math.max(index - 1, 0)];
    if (event.key === "Home") target = rows[0];
    if (event.key === "End") target = rows.at(-1);
    if (event.key === "ArrowRight") { if (node.children?.length && !isExpanded(node)) toggle(node.key); else target = row.parentElement.querySelector('[role="group"] .explorer-row'); }
    if (event.key === "ArrowLeft") { if (node.children?.length && isExpanded(node)) toggle(node.key); else target = row.parentElement.parentElement.closest("li")?.querySelector(".explorer-row"); }
    if (target) { rows.forEach(item => item.tabIndex = -1); target.tabIndex = 0; target.focus(); }
  });
  search.addEventListener("input", () => { searchCollapsed.clear(); render(); });
  const receive = designer.receive.bind(designer);
  designer.receive = message => {
    receive(message);
    if (message.action === "explorer-reveal") {
      function reveal(node) {
        if (node.key === message.key) return true;
        if ((node.children ?? []).some(reveal)) { expanded.set(node.key, true); return true; }
        return false;
      }
      if (root && reveal(root)) { search.value = ""; selectedKey = message.key; render(message.key); }
      return;
    }
    if (message.action !== "state" || !message.explorer) return;
    if (root && root.key !== message.explorer.key) { expanded.clear(); search.value = ""; searchCollapsed.clear(); selectedKey = null; }
    if (selectedId !== message.selectedId && nodes.get(selectedKey)?.controlId !== message.selectedId) selectedKey = null;
    selectedId = message.selectedId;
    const previousForm = activeForm; activeForm = message.id;
    const next = JSON.stringify(message.explorer); root = message.explorer;
    if (next !== signature || previousForm !== activeForm) { signature = next; render(); }
    else selectRows();
  };
  const menu = document.createElement("div"); menu.className = "explorer-context-menu"; menu.hidden = true;
  menu.setAttribute("role", "menu"); menu.setAttribute("aria-label", "Project item actions"); document.body.append(menu);
  let menuNode;
  function explorerAction(node, action) {
    window.forma.send({ type: "designer", event: "explorer-action", payload: { action, key: node.key, controlId: node.controlId } });
  }
  function closeMenu(restore = false) {
    menu.hidden = true;
    if (restore && menuNode) [...tree.querySelectorAll("[data-explorer-key]")].find(row => row.dataset.explorerKey === menuNode.key)?.focus();
  }
  function showMenu(event, node) {
    event.preventDefault(); event.stopPropagation(); menuNode = node;
    selectedKey = node.key; selectRows(); menu.replaceChildren();
    const add = (label, action, disabled = false) => {
      const button = document.createElement("button"); button.textContent = label; button.dataset.explorerAction = action;
      button.setAttribute("role", "menuitem"); button.disabled = disabled; menu.append(button);
    };
    const separator = () => menu.append(document.createElement("hr"));
    if (node.key === "assets") add("Add Image to Current Form…", "add-image", nodes.get(activeForm)?.locked);
    const files = node.key === "files" || node.kind === "project-folder" || node.kind === "project";
    if (node.kind === "project" || node.key === "forms") add("Add Form", "add-form");
    if (files) {
      add("New JavaScript File…", "new-js"); add("New CSS File…", "new-css"); add("New JSON File…", "new-json");
      add("New Folder…", "new-folder"); add("Add Existing File…", "import-file"); separator();
    }
    if (node.itemId) {
      if (node.kind !== "project-folder") add("Open", "open-project-file");
      add("Rename…", "rename-item"); add("Remove from Project…", "delete-item"); separator();
    } else if (node.kind === "form" || node.kind === "component" || node.kind === "image") {
      add("View Designer / Properties", "select");
      add("Rename…", "rename-control", node.locked); separator();
      add("View CSS", "view-css", node.locked); add("View Script", "view-script", node.locked); add("View Custom Properties", "view-custom-properties", node.locked);
      if (node.kind === "image") add("Choose Image…", "choose-image", node.locked);
      if (node.kind !== "image") { separator(); add(node.kind === "form" ? "Remove Form…" : "Delete Component…", node.kind === "form" ? "delete-form" : "delete-control", node.locked || node.kind === "form" && (root.children.find(child => child.key === "forms")?.children.length ?? 1) < 2); }
    } else if (node.command) add("Open", "open", node.locked);
    else if (node.key.startsWith("sources:")) {
      for (const source of node.children ?? []) add(`View ${source.name}`, source.command, source.locked);
    }
    if (node.kind === "project") { add("Save Project", "save"); add("Open main.js", "global"); add("Open Project Folder", "reveal-project"); }
    if (node.children?.length) { separator(); add(isExpanded(node) ? "Collapse" : "Expand", "toggle"); add("Expand All", "expand-all"); add("Collapse All", "collapse-all"); }
    menu.hidden = false;
    const row = [...tree.querySelectorAll("[data-explorer-key]")].find(row => row.dataset.explorerKey === node.key), rect = row.getBoundingClientRect();
    // The native code WebView sits above the designer browser in the center.
    // Keep this popup inside the sidebar while that editor is visible.
    const dock = document.getElementById("editor-dock");
    const rightEdge = dock && !dock.hidden ? panel.getBoundingClientRect().right - 4 : window.innerWidth;
    menu.style.left = `${Math.max(0, Math.min(event.clientX || rect.left, rightEdge - menu.offsetWidth))}px`;
    menu.style.top = `${Math.max(0, Math.min(event.clientY || rect.bottom, window.innerHeight - menu.offsetHeight))}px`;
    menu.querySelector("button:not(:disabled)")?.focus();
  }
  tree.addEventListener("contextmenu", event => {
    const row = event.target.closest("[data-explorer-key]"); if (row) showMenu(event, nodes.get(row.dataset.explorerKey));
  });
  tree.addEventListener("keydown", event => {
    const row = event.target.closest("[data-explorer-key]");
    if (event.key === "F2" && row) { event.preventDefault(); event.stopPropagation(); const node = nodes.get(row.dataset.explorerKey); if (!node.locked && (node.itemId || node.controlId && !node.command)) explorerAction(node, node.itemId ? "rename-item" : "rename-control"); }
    if (event.shiftKey && event.key === "F10" && row) showMenu(event, nodes.get(row.dataset.explorerKey));
  });
  menu.addEventListener("click", event => {
    const button = event.target.closest("[data-explorer-action]"); if (!button || button.disabled) return;
    const node = menuNode, action = button.dataset.explorerAction; closeMenu(true);
    if (["new-js", "new-css", "new-json", "new-folder", "import-file"].includes(action)) { expanded.set(node.key, true); render(); }
    if (action === "toggle") toggle(node.key);
    else if (action === "expand-all" || action === "collapse-all") {
      function expand(node) { expanded.set(node.key, action === "expand-all"); (node.children ?? []).forEach(expand); }
      expand(node); search.value = ""; render(node.key);
    } else if (action === "select") window.forma.send({ type: "designer", event: "explorer-select", id: node.controlId, payload: {} });
    else if (action === "open") open(node);
    else if (["view-css", "view-script", "view-custom-properties"].includes(action)) window.forma.send({ type: "designer", event: "command", id: node.controlId ?? node.children?.[0]?.controlId, payload: { command: action } });
    else if (["add-form", "save", "global"].includes(action)) window.forma.send({ type: "designer", event: "command", payload: { command: action === "add-form" ? "new" : action === "global" ? "edit-global-script" : "save" } });
    else explorerAction(node, action);
  });
  menu.addEventListener("keydown", event => {
    event.stopPropagation();
    if (event.key === "Escape") { event.preventDefault(); closeMenu(true); return; }
    if (event.key === "Tab") { closeMenu(); return; }
    if (!["ArrowDown", "ArrowUp", "Home", "End"].includes(event.key)) return;
    event.preventDefault(); const buttons = [...menu.querySelectorAll("button:not(:disabled)")]; const index = buttons.indexOf(document.activeElement);
    const next = event.key === "Home" ? 0 : event.key === "End" ? buttons.length - 1 : (index + (event.key === "ArrowDown" ? 1 : -1) + buttons.length) % buttons.length; buttons[next]?.focus();
  });
  document.addEventListener("pointerdown", event => { if (!menu.contains(event.target)) closeMenu(); });
  window.addEventListener("resize", () => closeMenu()); window.addEventListener("blur", () => closeMenu());
  let initialPanel = panel.id;
  try { if (localStorage.getItem("forma.workspace.leftPanel") === toolbox.id) initialPanel = toolbox.id; } catch {}
  showPanel(initialPanel, false);
  window.formaExplorer = { show: showPanel };
})();
