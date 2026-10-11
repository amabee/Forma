// Start screen: a full-window view with project actions, recent projects, templates and a few
// shortcuts. The host sends { action: "start-page", recent, templates } on launch and from
// File > Start page; every item here maps to a real host command.
(() => {
  const d = window.formaDesigner;
  if (!d || d.standalone) return;
  const command = (name, payload = {}) => window.forma.send({ type: "designer", id: d.rootId, event: "command", payload: { command: name, ...payload } });
  let page = null, restoreFocus = null;

  function element(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  }
  function icon(name) {
    const image = element("img", "icon-svg");
    image.src = `icons/${name}.svg`; image.alt = "";
    return image;
  }
  function button(className, label, run, iconName) {
    const node = element("button", className);
    node.type = "button";
    if (iconName) node.append(icon(iconName));
    node.append(element("span", "", label));
    node.addEventListener("click", run);
    return node;
  }

  /** "Edited 2 hours ago" from an ISO timestamp; falls back to the date. */
  function edited(iso) {
    const time = Date.parse(iso ?? "");
    if (Number.isNaN(time)) return "";
    const minutes = Math.round((Date.now() - time) / 60000);
    const unit = (value, name) => `Edited ${value} ${name}${value === 1 ? "" : "s"} ago`;
    if (minutes < 1) return "Edited just now";
    if (minutes < 60) return unit(minutes, "minute");
    if (minutes < 60 * 24) return unit(Math.round(minutes / 60), "hour");
    if (minutes < 60 * 24 * 14) return unit(Math.round(minutes / 1440), "day");
    return `Edited ${new Date(time).toLocaleDateString()}`;
  }

  // Small wireframes of each template's real layout (see ProjectTemplates.cs).
  const SVG = "http://www.w3.org/2000/svg";
  const wireframes = {
    login: '<rect x="18" y="7" width="28" height="4" rx="1"/><rect class="field" x="14" y="15" width="36" height="5" rx="1"/><rect class="field" x="14" y="23" width="36" height="5" rx="1"/><rect class="fill" x="14" y="32" width="36" height="5" rx="1"/>',
    settings: '<rect class="field" x="6" y="6" width="52" height="22" rx="2"/><rect class="fill" x="10" y="10" width="7" height="4" rx="2"/><rect class="fill" x="10" y="17" width="7" height="4" rx="2"/><rect x="20" y="11" width="18" height="2" rx="1"/><rect x="20" y="18" width="14" height="2" rx="1"/><rect class="field" x="30" y="33" width="12" height="5" rx="1"/><rect class="fill" x="45" y="33" width="13" height="5" rx="1"/>',
    dashboard: '<rect x="0" y="0" width="64" height="8"/><rect class="field" x="0" y="8" width="14" height="36"/><rect class="fill" x="2" y="11" width="10" height="3" rx="1"/><rect class="field" x="18" y="12" width="13" height="10" rx="1.5"/><rect class="field" x="34" y="12" width="13" height="10" rx="1.5"/><rect class="field" x="50" y="12" width="12" height="10" rx="1.5"/>',
  };
  function thumbnail(id) {
    const svg = document.createElementNS(SVG, "svg");
    svg.setAttribute("viewBox", "0 0 64 44"); svg.setAttribute("aria-hidden", "true");
    svg.classList.add("start-wireframe");
    svg.innerHTML = wireframes[id] ?? "";
    return svg;
  }

  function close() {
    if (!page) return;
    page.remove(); page = null;
    restoreFocus?.focus?.();
  }

  function show(message) {
    const reopening = !!page;
    if (!reopening) restoreFocus = document.activeElement;
    page?.remove();
    page = element("section", "start-page");
    page.setAttribute("role", "dialog"); page.setAttribute("aria-modal", "true"); page.setAttribute("aria-labelledby", "start-title");
    const leave = run => () => { close(); run(); };
    const newProject = leave(() => command("new-project"));
    const openProject = leave(() => command("open"));

    // Rail: brand, the same actions as the hero, and a way back to the designer.
    const rail = element("nav", "start-rail");
    rail.setAttribute("aria-label", "Start");
    const brand = element("div", "start-brand");
    const mark = element("img"); mark.src = "forma-mark.svg"; mark.alt = "";
    brand.append(mark, element("span", "", "Forma"));
    const jump = target => () => page.querySelector(target)?.querySelector("button:not(:disabled)")?.focus();
    rail.append(brand,
      button("start-nav", "New project", newProject, "file-plus"),
      button("start-nav", "Open project…", openProject, "folder-open"),
      button("start-nav", "Templates", jump(".start-templates"), "panels-top-left"),
      button("start-nav", "Recent projects", jump(".start-recent"), "list"));
    const foot = element("div", "start-rail-foot");
    const resume = button("start-resume", "Go to the designer", close);
    resume.title = "Close the start screen (Esc)";
    foot.append(resume, element("span", "start-version", "Forma 0.1.0"));
    rail.append(foot);

    // Hero: the tagline beside the mark, selected on a design canvas like a control.
    const main = element("main", "start-main");
    const hero = element("header", "start-hero");
    const intro = element("div", "start-intro");
    const title = element("h1", "", "Drag it. Style it. Code it. Ship it.");
    title.id = "start-title";
    intro.append(title,
      element("p", "start-lead", "Lay out Windows forms visually, style them like the web, and wire them up in C# or JavaScript."));
    const actions = element("div", "start-cta");
    actions.append(button("start-primary", "New project", newProject, "file-plus"), button("start-secondary", "Open project…", openProject, "folder-open"));
    intro.append(actions);
    const canvas = element("figure", "start-canvas");
    canvas.setAttribute("aria-hidden", "true");
    const specimen = element("div", "start-specimen");
    const art = element("img"); art.src = "forma-mark.svg"; art.alt = "";
    specimen.append(art);
    for (const handle of ["nw", "n", "ne", "e", "se", "s", "sw", "w"]) specimen.append(element("i", `start-handle ${handle}`));
    canvas.append(specimen, element("figcaption", "start-readout", "X 48   Y 40   128 × 144"));
    hero.append(intro, canvas);

    // Recent projects: a plain list, newest first.
    const recent = element("section", "start-recent");
    recent.setAttribute("aria-labelledby", "start-recent-title");
    const recentTitle = element("h2", "", "Recent projects"); recentTitle.id = "start-recent-title";
    const list = element("ul", "start-recent-list");
    for (const project of message.recent ?? []) {
      const item = element("li", "start-recent-row");
      const open = element("button", "start-recent-item");
      open.type = "button"; open.title = project.path; open.disabled = !project.exists;
      const text = element("span", "start-recent-text");
      text.append(element("strong", "", project.name), element("span", "start-recent-folder", project.exists ? project.folder : `Not found: ${project.folder}`));
      open.append(icon("folder-open"), text, element("span", "start-recent-time", project.exists ? edited(project.edited) : ""));
      open.addEventListener("click", leave(() => command("open-recent", { path: project.path })));
      const remove = element("button", "start-recent-remove");
      remove.type = "button"; remove.title = "Remove from recent projects";
      remove.setAttribute("aria-label", `Remove ${project.name} from recent projects`);
      remove.append(icon("x"));
      remove.addEventListener("click", () => command("remove-recent", { path: project.path }));
      item.append(open, remove);
      list.append(item);
    }
    if (!list.children.length) {
      const empty = element("li", "start-empty");
      empty.append(element("p", "", "Projects you open or save will show up here."), button("start-secondary", "Open project…", openProject, "folder-open"));
      list.append(empty);
    }
    recent.append(recentTitle, list);

    // Templates with wireframes of their layouts, then a few shortcuts worth knowing.
    const side = element("div", "start-side");
    const templates = element("section", "start-templates");
    templates.setAttribute("aria-labelledby", "start-templates-title");
    const templatesTitle = element("h2", "", "Start from a template"); templatesTitle.id = "start-templates-title";
    const templateList = element("div", "start-template-list");
    for (const template of message.templates ?? []) {
      const choice = element("button", "start-template");
      choice.type = "button";
      const text = element("span", "start-template-text");
      text.append(element("strong", "", template.name), element("span", "", template.description));
      choice.append(thumbnail(template.id), text);
      choice.addEventListener("click", leave(() => command("new-from-template", { template: template.id })));
      templateList.append(choice);
    }
    templates.append(templatesTitle, templateList);

    const keys = element("section", "start-keys");
    keys.setAttribute("aria-labelledby", "start-keys-title");
    const keysTitle = element("h2", "", "Good to know"); keysTitle.id = "start-keys-title";
    const table = element("dl");
    for (const [combo, meaning] of [[["Ctrl", "K"], "Find any command, control or form"], [["Double-click"], "Open a control's code at its main event"], [["Ctrl", "Click"], "Select several controls at once"], [["F1"], "See every keyboard shortcut"]]) {
      const term = element("dt");
      combo.forEach(key => term.append(element("kbd", "", key)));
      table.append(term, element("dd", "", meaning));
    }
    keys.append(keysTitle, table);
    side.append(templates, keys);

    const grid = element("div", "start-grid");
    grid.append(recent, side);
    main.append(hero, grid);
    page.append(rail, main);
    document.body.append(page);
    (reopening ? page.querySelector(".start-recent-item:not(:disabled), .start-primary") : page.querySelector(".start-primary"))?.focus();
  }

  document.addEventListener("keydown", event => { if (event.key === "Escape" && page) { event.stopPropagation(); close(); } }, true);
  // Using the toolbox means the user is designing; get out of the way.
  for (const type of ["dragstart", "dblclick"])
    document.addEventListener(type, event => { if (event.target.closest?.("[data-kind]")) close(); });

  const receive = d.receive.bind(d);
  d.receive = message => {
    if (message?.action === "start-page") { show(message); return; }
    if (message?.action === "preview") close();
    return receive(message);
  };
  window.formaStartPage = { show, close, get open() { return !!page; } };
})();
