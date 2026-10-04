(() => {
  const d = window.formaDesigner, dock = document.getElementById("editor-dock"), mount = document.getElementById("editor-mount");
  if (!dock || !mount) return;
  let frame = 0;
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
    if (message.action === "editor-open") { dock.hidden = false; measure(); return; }
    if (message.action === "editor-close") { dock.hidden = true; return; }
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
  const splitter = document.getElementById("editor-splitter");
  const size = height => {
    const max = Math.max(180, document.querySelector(".designer-area").clientHeight - 100);
    const value = Math.max(180, Math.min(max, height));
    dock.style.height = `${value}px`; splitter.setAttribute("aria-valuenow", value); splitter.setAttribute("aria-valuemax", max); measure();
  };
  let drag;
  splitter.addEventListener("pointerdown", e => { drag = { y: e.clientY, height: dock.clientHeight }; splitter.setPointerCapture(e.pointerId); e.preventDefault(); });
  splitter.addEventListener("pointermove", e => { if (drag) size(drag.height + drag.y - e.clientY); });
  splitter.addEventListener("pointerup", () => { drag = null; });
  splitter.addEventListener("pointercancel", () => { drag = null; });
  splitter.addEventListener("keydown", e => { if (["ArrowUp", "ArrowDown"].includes(e.key)) { e.preventDefault(); size(dock.clientHeight + (e.key === "ArrowUp" ? 20 : -20)); } });
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
  window.formaWorkspace = { measure, size };
})();
