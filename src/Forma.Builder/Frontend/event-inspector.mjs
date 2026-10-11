import { eventsFor, inspectHandlers, normalizeEvent } from "./event-handlers.mjs";
const d = window.formaDesigner;
const panel = document.getElementById("event-editors"), search = document.getElementById("event-search");
let rendered = null;
function render() {
  const item = d.state?.controls.find(control => control.id === d.selectedId);
  const source = item?.customization?.behavior ?? item?.customization?.Behavior ?? "";
  const events = item?.customization?.events ?? item?.customization?.Events ?? "";
  const signature = JSON.stringify([item?.id, item?.kind, item?.component, item?.locked, d.preview, source, events, search.value]);
  if (signature === rendered) return;
  rendered = signature;
  panel.replaceChildren();
  if (!item) return;
  const inspected = inspectHandlers(source), needle = search.value.trim().toLowerCase();
  const note = document.createElement("p"); note.className = "event-note";
  note.textContent = inspected.error || inspectHandlers(events).error ? "A source file has a syntax error. Fix it before adding handlers." : "Methods live in event.js; script.js imports and subscribes them. Save to apply.";
  panel.append(note);
  let count = 0;
  for (const [group, names] of Object.entries(eventsFor(item))) {
    const matching = names.filter(name => `${group} ${name}`.toLowerCase().includes(needle));
    if (!matching.length) continue;
    const section = document.createElement("details"); section.className = "property-group event-group"; section.open = true;
    const title = document.createElement("summary"); title.textContent = group; section.append(title);
    for (const name of matching) {
      const handlers = inspected.handlers.filter(handler => handler.key === normalizeEvent(name));
      const button = document.createElement("button"); button.className = "event-handler";
      button.disabled = d.preview || !!item.locked;
      const label = document.createElement("span"); label.textContent = name;
      const status = document.createElement("small"); status.textContent = handlers.length ? `${handlers[0].name}${handlers.length > 1 ? ` (+${handlers.length - 1})` : ""}` : inspected.error ? "Open script" : "+ Add handler";
      button.append(label, status); button.title = `${name}: ${status.textContent}`;
      button.addEventListener("click", () => window.forma.send({ type: "designer", event: "command", id: item.id, payload: { command: "edit-event", eventName: name } }));
      section.append(button); count++;
    }
    panel.append(section);
  }
  if (!count) { const empty = document.createElement("p"); empty.className = "event-note"; empty.textContent = "No matching events."; panel.append(empty); }
}
const propertiesTab = document.getElementById("inspector-properties-tab"), eventsTab = document.getElementById("inspector-events-tab");
function activate(events) {
  for (const [tab, selected] of [[propertiesTab, !events], [eventsTab, events]]) { tab.classList.toggle("active", selected); tab.setAttribute("aria-selected", String(selected)); tab.tabIndex = selected ? 0 : -1; }
  document.getElementById("property-search").closest("label").hidden = events;
  document.getElementById("custom-properties-action").hidden = events;
  document.getElementById("property-editors").hidden = events;
  document.getElementById("events-inspector").hidden = !events;
  if (events) render();
}
propertiesTab.addEventListener("click", () => activate(false));
eventsTab.addEventListener("click", () => activate(true));
for (const tab of [propertiesTab, eventsTab]) tab.addEventListener("keydown", event => {
  if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
  event.preventDefault(); const next = event.key === "Home" ? propertiesTab : event.key === "End" ? eventsTab : tab === propertiesTab ? eventsTab : propertiesTab; next.click(); next.focus();
});
search.addEventListener("input", render);
const receive = d.receive.bind(d);
d.receive = message => { receive(message); if (["state", "select", "initialize"].includes(message.action) && !document.getElementById("events-inspector").hidden) render(); };
const select = d.select.bind(d);
d.select = id => { select(id); if (!document.getElementById("events-inspector").hidden) render(); };
