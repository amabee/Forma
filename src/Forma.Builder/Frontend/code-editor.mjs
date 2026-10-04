import { EditorView, basicSetup } from "codemirror";
import { EditorState } from "@codemirror/state";
import { keymap } from "@codemirror/view";
import { indentWithTab } from "@codemirror/commands";
import { javascript } from "@codemirror/lang-javascript";
import { css } from "@codemirror/lang-css";
import { json } from "@codemirror/lang-json";
import { HighlightStyle, syntaxHighlighting } from "@codemirror/language";
import { tags } from "@lezer/highlight";
import { formatCode } from "./code-format.mjs";

const definitions = { css: { title: "CSS", extension: css() }, behavior: { title: "JavaScript", extension: javascript() }, characteristics: { title: "JSON", extension: json() } };
const language = key => ({ css: "css", behavior: "javascript", characteristics: "json" })[key];
const send = message => window.chrome?.webview?.postMessage({ type: "editor", ...message });
const status = text => { document.getElementById("editor-message").textContent = text; };
const theme = EditorView.theme({
  "&": { height: "100%", color: "#e2e8f0", backgroundColor: "#0f172a", fontSize: "14px" },
  ".cm-scroller": { fontFamily: "Consolas, monospace", overflow: "auto", lineHeight: "1.65" },
  ".cm-content": { padding: "14px 0", caretColor: "#60a5fa" },
  ".cm-gutters": { backgroundColor: "#0f172a", color: "#64748b", border: "none", paddingRight: "12px" },
  // CodeMirror draws selections behind the text. An opaque active line hides them.
  ".cm-activeLine": { backgroundColor: "rgba(148, 163, 184, 0.08)" },
  ".cm-activeLineGutter": { backgroundColor: "#1e293b" },
  ".cm-selectionBackground": { backgroundColor: "#475569" },
  "&.cm-focused > .cm-scroller > .cm-selectionLayer .cm-selectionBackground": { backgroundColor: "#2563eb" },
  ".cm-panels, .cm-tooltip": { backgroundColor: "#1e293b", color: "#e2e8f0", borderColor: "#334155" },
}, { dark: true });
const highlighting = syntaxHighlighting(HighlightStyle.define([
  { tag: tags.keyword, color: "#c084fc" }, { tag: [tags.string, tags.special(tags.string)], color: "#86efac" },
  { tag: [tags.number, tags.bool, tags.null], color: "#fdba74" }, { tag: tags.comment, color: "#94a3b8", fontStyle: "italic" },
  { tag: [tags.function(tags.variableName), tags.propertyName], color: "#7dd3fc" },
  { tag: [tags.typeName, tags.className], color: "#facc15" }, { tag: tags.operator, color: "#f0abfc" },
]));
const views = new Map(); let active = "css", busy = false, dirty = false, populating = false;
function source() { return Object.fromEntries([...views].map(([key, view]) => [key, view.state.doc.toString()])); }
function markDirty(value) {
  if (dirty === value) return;
  dirty = value; document.getElementById("editor-dirty").textContent = dirty ? "Unsaved changes" : "Saved";
  send({ event: "dirty", dirty });
}
function cursor() {
  const view = views.get(active), pos = view.state.selection.main.head, line = view.state.doc.lineAt(pos);
  document.getElementById("editor-cursor").textContent = `${definitions[active].title} · Ln ${line.number}, Col ${pos - line.from + 1} · Spaces: 2`;
}
function extensions(key) {
  return [basicSetup, definitions[key].extension, theme, highlighting, EditorState.tabSize.of(2),
    keymap.of([indentWithTab, { key: "Mod-s", run: () => { command("save"); return true; } },
      { key: "Mod-Shift-f", run: () => { command("format"); return true; } }]),
    EditorView.updateListener.of(update => { if (update.docChanged && !populating) markDirty(true); if (update.selectionSet || update.docChanged) cursor(); })];
}
for (const key of Object.keys(definitions)) views.set(key, new EditorView({ state: EditorState.create({ extensions: extensions(key) }), parent: document.getElementById(`editor-${key}`) }));
function activate(key) {
  active = key;
  for (const [name, view] of views) {
    view.dom.parentElement.hidden = name !== key;
    const tab = document.querySelector(`[data-tab="${name}"]`); tab.setAttribute("aria-selected", String(name === key));
    if (name === key) { view.requestMeasure(); view.focus(); }
  }
  cursor();
}
async function formatKeys(keys) {
  // Parse every requested document before replacing any, so one invalid file cannot partly format a save.
  const originals = new Map(keys.map(key => [key, views.get(key).state.doc.toString()]));
  const changes = await Promise.all(keys.map(async key => [key, await formatCode(language(key), originals.get(key))]));
  if (keys.some(key => views.get(key).state.doc.toString() !== originals.get(key)))
    throw new Error("Document changed while formatting; try again");
  for (const [key, text] of changes) {
    const view = views.get(key);
    if (text !== view.state.doc.toString()) view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: text } });
  }
}
function setBusy(value) { busy = value; document.querySelectorAll('[data-command="save"],[data-command="format"],[data-command="external"]').forEach(button => button.disabled = value); }
async function command(event) {
  if (busy) return;
  if (["choose-editor", "close"].includes(event)) { send({ event }); return; }
  setBusy(true);
  try {
    if (event === "format") { await formatKeys([active]); status("Document formatted. Ctrl+S saves and applies."); setBusy(false); return; }
    if (document.getElementById("format-on-save").checked) await formatKeys(Object.keys(definitions));
    send({ event, source: source() }); // Host acknowledges save/external or returns an error.
  } catch (error) { status(`Format failed: ${error.message}. Fix syntax or turn off Format on save.`); setBusy(false); }
}
function receive(message) {
  if (message.action === "source") {
    populating = true;
    try { for (const [key, view] of views) view.setState(EditorState.create({ doc: message.source[key] ?? "", extensions: extensions(key) })); }
    finally { populating = false; }
    markDirty(false); activate(active);
  }
  if (message.action === "saved") { const current = source(); markDirty(Object.keys(definitions).some(key => current[key] !== message.source[key])); setBusy(false); }
  if (message.action === "error") setBusy(false);
  if (message.status) status(message.status);
}
document.querySelectorAll("[data-tab]").forEach(tab => tab.addEventListener("click", () => activate(tab.dataset.tab)));
document.querySelectorAll("[data-command]").forEach(button => button.addEventListener("click", () => command(button.dataset.command)));
window.chrome?.webview?.addEventListener("message", event => receive(event.data));
window.addEventListener("keydown", event => {
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "s") { event.preventDefault(); command("save"); }
});
window.addEventListener("pagehide", () => { for (const view of views.values()) view.destroy(); });
window.formaCodeEditor = { command, receive, source, views, activate };
activate("css"); send({ event: "ready" });
