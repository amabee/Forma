(() => {
  const d = window.formaDesigner, dock = document.getElementById("editor-dock"), mount = document.getElementById("editor-mount");
  if (!dock || !mount) return;
  let frame = 0, editorOpen = false, activeEditorKey = null;
  const designTab = document.getElementById("workspace-design-tab"), codeTab = document.getElementById("workspace-code-tab");
  function activateWorkspace(tab) {
    const code = tab === "code" && editorOpen;
    dock.hidden = !code;
    document.getElementById("stage").hidden = code;
    const tray = document.getElementById("component-tray");
    tray.classList.toggle("workspace-inactive", code);
    designTab.setAttribute("aria-selected", String(!code));
    designTab.tabIndex = code ? -1 : 0;
    document.querySelectorAll('[data-workspace-tab="code"]').forEach(tab => {
      const selected = code && (tab.dataset.editorKey ?? null) === activeEditorKey;
      tab.setAttribute("aria-selected", String(selected)); tab.tabIndex = selected ? 0 : -1;
    });
    window.forma.send({ type: "designer", event: "editor-visibility", payload: { visible: code } });
    if (code) measure();
  }
  const tabBar = document.querySelector(".workspace-tabs");
  tabBar.addEventListener("click", event => {
    const close = event.target.closest("[data-close-editor]");
    if (close) {
      window.forma.send({ type: "designer", event: "editor-close-tab", payload: { key: close.dataset.closeEditor } });
      return;
    }
    const tab = event.target.closest("[data-workspace-tab]");
    if (!tab) return;
    if (tab.dataset.workspaceTab === "code" && tab.dataset.editorKey && tab.dataset.editorKey !== activeEditorKey)
      window.forma.send({ type: "designer", event: "editor-activate", payload: { key: tab.dataset.editorKey } });
    else activateWorkspace(tab.dataset.workspaceTab);
  });
  tabBar.addEventListener("keydown", event => {
    if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
    event.preventDefault(); event.stopPropagation();
    const tabs = [...tabBar.querySelectorAll("[data-workspace-tab]")].filter(tab => !tab.hidden);
    const index = tabs.indexOf(event.target);
    const next = event.key === "Home" ? 0 : event.key === "End" ? tabs.length - 1 : (index + (event.key === "ArrowRight" ? 1 : -1) + tabs.length) % tabs.length;
    tabs[next].click(); tabs[next].focus();
  });
  function renderEditorTabs(message) {
    const wasCode = !dock.hidden;
    activeEditorKey = message.activeKey ?? null;
    editorOpen = message.editors.length > 0;
    codeTab.hidden = true;
    tabBar.querySelectorAll(".workspace-document").forEach(tab => tab.remove());
    for (const editor of message.editors) {
      const group = document.createElement("div"); group.className = "workspace-document";
      const tab = document.createElement("button");
      tab.dataset.workspaceTab = "code"; tab.dataset.editorKey = editor.key;
      tab.setAttribute("role", "tab"); tab.setAttribute("aria-controls", "editor-dock");
      tab.textContent = `${editor.name} · Code${editor.dirty ? " ●" : ""}`;
      tab.title = `${editor.name}${editor.dirty ? " — unsaved changes" : ""}`;
      const close = document.createElement("button"); close.dataset.closeEditor = editor.key;
      close.className = "workspace-tab-close"; close.textContent = "×";
      close.setAttribute("aria-label", `Close ${editor.name} code`);
      group.append(tab, close); tabBar.append(group);
    }
    activateWorkspace(editorOpen && (message.show || wasCode) ? "code" : "design");
  }
  const bounds = () => {
    frame = 0; if (dock.hidden) return;
    const r = mount.getBoundingClientRect();
    window.forma.send({ type: "designer", event: "editor-bounds", payload: { x: r.x, y: r.y, width: r.width, height: r.height } });
  };
  const measure = () => { if (!frame) frame = requestAnimationFrame(bounds); };
  const themeToggle = document.getElementById("theme-toggle");
  let theme = "dark", hostThemeAnnounced = false;
  try { if (localStorage.getItem("forma.workspace.theme") === "light") theme = "light"; } catch {}
  function applyTheme(value, persist = false) {
    theme = value === "light" ? "light" : "dark";
    document.documentElement.dataset.theme = theme;
    const label = theme === "dark" ? "Switch to light mode" : "Switch to dark mode";
    themeToggle.setAttribute("aria-pressed", String(theme === "dark"));
    themeToggle.setAttribute("aria-label", label); themeToggle.title = label;
    themeToggle.textContent = theme === "dark" ? "☀ Light mode" : "☾ Dark mode";
    if (persist) try { localStorage.setItem("forma.workspace.theme", theme); } catch {}
    window.forma.send({ type: "designer", event: "theme", payload: { theme } });
    measure();
  }
  applyTheme(theme);
  themeToggle.addEventListener("click", () => applyTheme(theme === "dark" ? "light" : "dark", true));
  const receive = d.receive.bind(d);
  d.receive = message => {
    if (message.action === "editor-tabs") { renderEditorTabs(message); return; }
    if (message.action === "editor-open") { editorOpen = true; codeTab.hidden = false; codeTab.textContent = `${message.name ?? "Component"} · Code`; activateWorkspace("code"); return; }
    if (message.action === "editor-close") { editorOpen = false; codeTab.hidden = true; activateWorkspace("design"); return; }
    receive(message);
    if (message.action === "state") {
      // Repeat after the host's initial state: the page can load before its
      // native message bridge subscribes to WebMessageReceived.
      if (!hostThemeAnnounced) {
        window.forma.send({ type: "designer", event: "theme", payload: { theme } });
        hostThemeAnnounced = true;
      }
      const search = document.getElementById("property-search"); if (search?.value) filterProperties(search.value);
    }
  };
  if (window.ResizeObserver) new ResizeObserver(measure).observe(mount);
  window.addEventListener("resize", measure);
  document.addEventListener("click", measure);
  const menu = document.getElementById("component-source-menu");
  let menuTarget;
  const closeMenu = () => { menu.hidden = true; };
  function openSourceMenu(event, control) {
    const item = d.state?.controls.find(c => c.id === control.id);
    if (!item || d.preview) return;
    event.preventDefault(); d.cancelDrag?.(); menuTarget = item.id;
    d.select?.(item.id);
    window.forma.send({ type: "designer", event: "select", id: item.id, payload: {} });
    document.getElementById("component-source-name").textContent = item.name || item.kind;
    menu.querySelectorAll("button").forEach(button => { button.disabled = !!item.locked; });
    menu.hidden = false;
    const r = control.getBoundingClientRect();
    menu.style.left = `${Math.max(0, Math.min(event.clientX || r.left, window.innerWidth - menu.offsetWidth))}px`;
    menu.style.top = `${Math.max(0, Math.min(event.clientY || r.bottom, window.innerHeight - menu.offsetHeight))}px`;
    menu.querySelector("button:not(:disabled)")?.focus();
  }
  document.addEventListener("contextmenu", event => {
    const control = event.target.closest("[data-component], [data-forma-type]");
    if (control) openSourceMenu(event, control);
  });
  menu.addEventListener("click", event => {
    const button = event.target.closest("[data-source-command]");
    if (!button || button.disabled) return;
    window.forma.send({ type: "designer", event: "command", id: menuTarget, payload: { command: button.dataset.sourceCommand } });
    closeMenu();
  });
  document.addEventListener("pointerdown", event => { if (!menu.contains(event.target)) closeMenu(); });
  window.addEventListener("blur", closeMenu);
  window.addEventListener("resize", closeMenu);
  document.addEventListener("keydown", event => {
    if (event.key === "Escape" && !menu.hidden) { closeMenu(); document.getElementById(menuTarget)?.focus(); }
    if (event.shiftKey && event.key === "F10") {
      const control = document.getElementById(d.selectedId); if (control) openSourceMenu(event, control);
    }
  });
  menu.addEventListener("keydown", event => {
    if (!["ArrowDown", "ArrowUp", "Home", "End", "Tab"].includes(event.key)) return;
    event.stopPropagation();
    if (event.key === "Tab") { closeMenu(); return; }
    event.preventDefault();
    const buttons = [...menu.querySelectorAll("button:not(:disabled)")];
    if (!buttons.length) return;
    const index = buttons.indexOf(document.activeElement);
    buttons[event.key === "Home" ? 0 : event.key === "End" ? buttons.length - 1 : (index + (event.key === "ArrowDown" ? 1 : -1) + buttons.length) % buttons.length].focus();
  });
  document.querySelectorAll("[data-library]").forEach(button => button.addEventListener("click", () => {
    document.getElementById("toolbox").hidden = false;
    document.querySelectorAll("[data-library]").forEach(item => item.setAttribute("aria-pressed", String(item === button)));
    document.querySelectorAll(".tool-group").forEach(group => {
      const title = group.querySelector("summary").textContent.trim().toLowerCase();
      group.hidden = button.dataset.library !== "all" && (button.dataset.library === "layouts" ? !title.includes("layout") : button.dataset.library === "components" ? !title.includes("component") : title.includes("layout") || title.includes("component"));
    });
    measure();
  }));
  function filterProperties(query) {
    const needle = query.toLowerCase();
    document.querySelectorAll(".property-group").forEach(group => {
      let count = 0;
      group.querySelectorAll("label").forEach(label => { label.hidden = !label.textContent.toLowerCase().includes(needle); if (!label.hidden) count++; });
      group.hidden = !!needle && !count && !group.textContent.toLowerCase().includes(needle);
      if (needle && !group.hidden) group.open = true;
    });
  }
  document.getElementById("property-search").addEventListener("input", e => filterProperties(e.target.value));
  window.formaWorkspace = { measure, activate: activateWorkspace };
})();
