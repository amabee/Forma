// Status bar feedback: problems are shown in an error style and kept in a short
// history the user can open, so a message that scrolls past is not lost.
(() => {
  const status = document.getElementById("status"), bar = document.querySelector(".statusbar");
  if (!status || !bar) return;
  const LIMIT = 20;
  const problem = /\b(error|could not|cannot|failed|invalid)\b|^fix the/i;
  const problems = [];

  const toggle = document.createElement("button");
  toggle.type = "button"; toggle.className = "status-problems"; toggle.hidden = true;
  toggle.setAttribute("aria-haspopup", "true"); toggle.setAttribute("aria-expanded", "false");
  const list = document.createElement("div");
  list.className = "status-problem-list"; list.setAttribute("role", "dialog"); list.setAttribute("aria-label", "Recent problems"); list.hidden = true;
  const heading = document.createElement("div"); heading.className = "status-problem-heading";
  const title = document.createElement("span"); title.textContent = "Recent problems";
  const clear = document.createElement("button"); clear.type = "button"; clear.textContent = "Clear";
  heading.append(title, clear);
  const items = document.createElement("ol");
  list.append(heading, items);
  status.parentElement.after(toggle);
  document.body.append(list);

  function render() {
    toggle.hidden = problems.length === 0;
    toggle.textContent = `⚠ ${problems.length}`;
    toggle.setAttribute("aria-label", `${problems.length} recent problem${problems.length === 1 ? "" : "s"}`);
    items.replaceChildren(...problems.map(entry => {
      const item = document.createElement("li");
      const time = document.createElement("time"); time.textContent = entry.time;
      const text = document.createElement("span"); text.textContent = entry.text;
      item.append(time, text);
      return item;
    }).reverse());
    if (!problems.length) setOpen(false);
  }
  function setOpen(open) {
    list.hidden = !open; toggle.setAttribute("aria-expanded", String(open));
    if (open) {
      const rect = toggle.getBoundingClientRect();
      list.style.left = `${Math.max(8, Math.min(rect.left, window.innerWidth - 368))}px`;
      list.style.bottom = `${Math.max(8, window.innerHeight - rect.top + 6)}px`;
      clear.focus();
    }
  }

  function report(text) {
    const isProblem = problem.test(text);
    bar.dataset.tone = isProblem ? "error" : "";
    if (!isProblem) return;
    // A refresh can repeat the same status; only record it once.
    if (problems.at(-1)?.text === text) return;
    problems.push({ text, time: new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit", second: "2-digit" }) });
    if (problems.length > LIMIT) problems.shift();
    render();
  }

  toggle.addEventListener("click", () => setOpen(list.hidden));
  clear.addEventListener("click", () => { problems.length = 0; bar.dataset.tone = ""; render(); toggle.focus(); });
  document.addEventListener("pointerdown", event => { if (!list.hidden && !list.contains(event.target) && event.target !== toggle) setOpen(false); });
  list.addEventListener("keydown", event => { if (event.key === "Escape") { event.stopPropagation(); setOpen(false); toggle.focus(); } });

  report(status.textContent);
  window.formaStatus = { report, problems };
})();
