// Toolbox ergonomics: search with result feedback, remembered group state and a
// "Recently used" group. Adding controls (drag, double-click, Enter) stays in designer.js.
(() => {
  const toolbox = document.getElementById("toolbox"), search = document.getElementById("search");
  const list = toolbox?.querySelector(".toolbox-list");
  if (!toolbox || !search || !list) return;
  const GROUPS_KEY = "forma.toolbox.groups", RECENT_KEY = "forma.toolbox.recent", RECENT_LIMIT = 6;
  // Groups that start open on first run; the rest stay collapsed to keep the list short.
  const DEFAULT_OPEN = new Set(["Layout", "Controls"]);
  const read = (key, fallback) => { try { return JSON.parse(localStorage.getItem(key)) ?? fallback; } catch { return fallback; } };
  const write = (key, value) => { try { localStorage.setItem(key, JSON.stringify(value)); } catch {} };
  const groups = () => [...list.querySelectorAll(":scope > .tool-group:not(.recent-tools)")];
  const title = group => group.querySelector("summary").textContent.trim();

  // Restore which groups were open; remember changes made by the user (not by searching).
  const saved = read(GROUPS_KEY, null);
  for (const group of groups()) group.open = saved ? saved[title(group)] ?? false : DEFAULT_OPEN.has(title(group));
  let searching = false;
  list.addEventListener("toggle", event => {
    if (searching || !event.target.matches?.(".tool-group:not(.recent-tools)")) return;
    write(GROUPS_KEY, Object.fromEntries(groups().map(group => [title(group), group.open])));
  }, true);

  // "Recently used" shows copies of tools; designer.js handles them like any [data-kind] tool.
  const recentGroup = document.createElement("details");
  recentGroup.className = "tool-group recent-tools"; recentGroup.open = true;
  const recentSummary = document.createElement("summary"); recentSummary.textContent = "Recently used";
  recentGroup.append(recentSummary);
  list.prepend(recentGroup);
  function renderRecent() {
    const kinds = read(RECENT_KEY, []).filter(kind => groups().some(group => group.querySelector(`[data-kind="${CSS.escape(kind)}"]`)));
    recentGroup.querySelectorAll("[data-kind]").forEach(tool => tool.remove());
    for (const kind of kinds) {
      const original = list.querySelector(`.tool-group:not(.recent-tools) [data-kind="${CSS.escape(kind)}"]`);
      const copy = original.cloneNode(true); copy.hidden = false;
      recentGroup.append(copy);
    }
    recentGroup.hidden = kinds.length === 0 || searching;
  }
  function remember(kind) {
    write(RECENT_KEY, [kind, ...read(RECENT_KEY, []).filter(item => item !== kind)].slice(0, RECENT_LIMIT));
    renderRecent();
  }
  // A tool counts as used when it is dragged, double-clicked or activated from the keyboard.
  // Re-render after the event so designer.js still sees the element that was used.
  const used = event => {
    const tool = event.target.closest?.("[data-kind]");
    if (tool && (event.type !== "click" || event.detail === 0)) setTimeout(() => remember(tool.dataset.kind));
  };
  for (const type of ["dragstart", "dblclick", "click"]) list.addEventListener(type, used);

  // Search: open matching groups, hide empty ones and say when nothing matches.
  const empty = document.createElement("p");
  empty.className = "toolbox-empty"; empty.setAttribute("role", "status"); empty.hidden = true;
  list.append(empty);
  let openBeforeSearch = null;
  function filter() {
    const query = search.value.trim().toLowerCase();
    const active = query.length > 0;
    if (active && !searching) openBeforeSearch = new Map(groups().map(group => [group, group.open]));
    searching = active;
    let matches = 0;
    for (const group of groups()) {
      let visible = 0;
      group.querySelectorAll("[data-kind]").forEach(tool => {
        tool.hidden = active && !tool.textContent.toLowerCase().includes(query) && !tool.dataset.kind.includes(query);
        if (!tool.hidden) visible++;
      });
      group.classList.toggle("search-empty", active && visible === 0);
      if (active) group.open = visible > 0;
      matches += visible;
    }
    if (!active && openBeforeSearch) { for (const [group, open] of openBeforeSearch) group.open = open; openBeforeSearch = null; }
    empty.textContent = active && matches === 0 ? `No controls match "${search.value.trim()}"` : "";
    empty.hidden = !(active && matches === 0);
    renderRecent();
  }
  search.addEventListener("input", filter);
  search.addEventListener("keydown", event => {
    if (event.key === "Escape" && search.value) {
      event.preventDefault(); event.stopPropagation();
      search.value = ""; filter();
    }
    if (event.key === "Enter") {
      // Insert the first visible match, the same way double-clicking it would.
      const first = groups().flatMap(group => [...group.querySelectorAll("[data-kind]")]).find(tool => !tool.hidden && !tool.closest(".search-empty, [hidden]"));
      if (!first) return;
      event.preventDefault();
      first.dispatchEvent(new MouseEvent("dblclick", { bubbles: true }));
    }
  });

  renderRecent();
  window.formaToolbox = { filter, remember };
})();
