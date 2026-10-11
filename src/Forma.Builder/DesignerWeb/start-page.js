// Start page over the canvas: new blank form, open, starter templates and recent projects.
// The host sends { action: "start-page", recent, templates } on launch and from File > Start page.
(() => {
  const d = window.formaDesigner, stage = document.getElementById("stage");
  if (!d || d.standalone || !stage) return;
  const command = (name, payload = {}) => window.forma.send({ type: "designer", id: d.rootId, event: "command", payload: { command: name, ...payload } });
  let page = null;

  function element(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;
    return node;
  }

  function close() {
    if (!page) return;
    page.remove(); page = null;
  }

  function show(message) {
    close();
    page = element("section", "start-page");
    page.setAttribute("role", "dialog");
    page.setAttribute("aria-label", "Start page");
    const panel = element("div", "start-panel");
    const header = element("div", "start-header");
    header.append(element("h2", "", "Start"));
    const dismiss = element("button", "start-close", "×");
    dismiss.type = "button"; dismiss.setAttribute("aria-label", "Close start page"); dismiss.title = "Close (Esc)";
    dismiss.addEventListener("click", close);
    header.append(dismiss);

    const actions = element("div", "start-actions");
    const action = (label, detail, run) => {
      const button = element("button", "start-action");
      button.type = "button";
      button.append(element("strong", "", label), element("span", "", detail));
      button.addEventListener("click", () => { close(); run(); });
      return button;
    };
    actions.append(
      action("Blank form", "Start from an empty form", () => {}),
      action("Open…", "Open a .forma project", () => command("open")),
    );

    const templates = element("div", "start-section");
    templates.append(element("h3", "", "Templates"));
    const templateList = element("div", "start-templates");
    for (const template of message.templates ?? [])
      templateList.append(action(template.name, template.description, () => command("new-from-template", { template: template.id })));
    templates.append(templateList);

    const recent = element("div", "start-section");
    recent.append(element("h3", "", "Recent projects"));
    const list = element("ul", "start-recent");
    for (const project of message.recent ?? []) {
      const item = element("li");
      const button = element("button", "start-recent-item");
      button.type = "button"; button.title = project.path;
      button.append(element("strong", "", project.name), element("span", "", project.exists ? project.folder : "File not found"));
      button.disabled = !project.exists;
      button.addEventListener("click", () => { close(); command("open-recent", { path: project.path }); });
      item.append(button); list.append(item);
    }
    if (!list.children.length) list.append(element("li", "start-empty", "Projects you open or save appear here."));
    recent.append(list);

    panel.append(header, actions, templates, recent);
    page.append(panel);
    stage.append(page);
    page.querySelector(".start-action")?.focus();
  }

  // Using the toolbox or Escape means the user is designing; get out of the way.
  document.addEventListener("keydown", event => { if (event.key === "Escape" && page) { event.stopPropagation(); close(); } }, true);
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
