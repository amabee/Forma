import { EditorView, basicSetup } from "codemirror";
import { EditorState, Compartment } from "@codemirror/state";
import { keymap } from "@codemirror/view";
import { indentWithTab } from "@codemirror/commands";
import { javascript, localCompletionSource } from "@codemirror/lang-javascript";
import { css } from "@codemirror/lang-css";
import { json } from "@codemirror/lang-json";
import { HighlightStyle, syntaxHighlighting } from "@codemirror/language";
import { tags } from "@lezer/highlight";
import { formatCode } from "./code-format.mjs";
import { autocompletion } from "@codemirror/autocomplete";
import { linter, lintGutter, openLintPanel, forEachDiagnostic, forceLinting } from "@codemirror/lint";
import { syntaxTree } from "@codemirror/language";
import { diagnose, formaCompletions } from "./code-assistance.mjs";
let completionContext = { controls: [] };
document.body.classList.toggle("embedded-editor", new URLSearchParams(window.location.search).has("embedded"));

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
const lightTheme = EditorView.theme({
  "&": { height: "100%", color: "#1e293b", backgroundColor: "#ffffff", fontSize: "14px" },
  ".cm-scroller": { fontFamily: "Consolas, monospace", overflow: "auto", lineHeight: "1.65" },
  ".cm-content": { padding: "14px 0", caretColor: "#2563eb" },
  ".cm-gutters": { backgroundColor: "#f8fafc", color: "#64748b", border: "none", paddingRight: "12px" },
  ".cm-activeLine": { backgroundColor: "rgba(59, 130, 246, 0.06)" },
  ".cm-activeLineGutter": { backgroundColor: "#eff6ff" },
  ".cm-selectionBackground": { backgroundColor: "#e2e8f0" },
  "&.cm-focused > .cm-scroller > .cm-selectionLayer .cm-selectionBackground": { backgroundColor: "#bfdbfe" },
  ".cm-panels, .cm-tooltip": { backgroundColor: "#f8fafc", color: "#1e293b", borderColor: "#cbd5e1" },
}, { dark: false });
const lightHighlighting = syntaxHighlighting(HighlightStyle.define([
  { tag: tags.keyword, color: "#7e22ce" }, { tag: tags.string, color: "#15803d" },
  { tag: [tags.number, tags.bool, tags.null], color: "#b45309" }, { tag: tags.comment, color: "#64748b", fontStyle: "italic" },
  { tag: [tags.function(tags.variableName), tags.propertyName], color: "#0369a1" },
  { tag: [tags.typeName, tags.className], color: "#854d0e" }, { tag: tags.operator, color: "#be185d" },
]));
const themeSlot = new Compartment(); let editorTheme = "dark";
const themeExtensions = () => editorTheme === "light" ? [lightTheme, lightHighlighting] : [theme, highlighting];
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
  return [basicSetup, definitions[key].extension, themeSlot.of(themeExtensions()), EditorState.tabSize.of(2),
    lintGutter(), linter(view => diagnose(language(key), view.state.doc.toString(), completionContext.controls), { delay: 400 }),
    ...(key === "behavior" ? [autocompletion({ override: [context => {
      if (/Comment/.test(syntaxTree(context.state).resolveInner(context.pos, -1).name)) return null;
      let values = {}; try { values = JSON.parse(views.get("characteristics")?.state.doc.toString() ?? "{}"); } catch {}
      return formaCompletions(context, completionContext.controls, values);
    }, localCompletionSource] })] : []),
    keymap.of([indentWithTab, { key: "Mod-s", run: () => { command("save"); return true; } },
      { key: "Mod-Shift-f", run: () => { command("format"); return true; } }]),
    EditorView.updateListener.of(update => {
      if (update.docChanged && !populating) markDirty(true);
      if (update.selectionSet || update.docChanged) cursor();
      let count = 0; for (const [name, view] of views) {
        let fileCount = 0; forEachDiagnostic(view.state, () => { count++; fileCount++; });
        const tab = document.querySelector(`[data-tab="${name}"]`);
        if (fileCount) tab.dataset.problems = String(fileCount); else delete tab.dataset.problems;
      }
      const indicator = document.getElementById("editor-problems"); if (indicator) indicator.textContent = `${count} problem${count === 1 ? "" : "s"}`;
    })];
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
  if (event === "problems") {
    let activeCount = 0; forEachDiagnostic(views.get(active).state, () => activeCount++);
    if (!activeCount) for (const [key, view] of views) {
      let count = 0; forEachDiagnostic(view.state, () => count++);
      if (count) { activate(key); break; }
    }
    openLintPanel(views.get(active)); return;
  }
  if (["choose-editor", "close"].includes(event)) { send({ event }); return; }
  setBusy(true);
  try {
    if (event === "format") { await formatKeys([active]); status("Document formatted. Ctrl+S saves and applies."); setBusy(false); return; }
    if (document.getElementById("format-on-save").checked) await formatKeys(Object.keys(definitions));
    send({ event, source: source() }); // Host acknowledges save/external or returns an error.
  } catch (error) { status(`Format failed: ${error.message}. Fix syntax or turn off Format on save.`); setBusy(false); }
}
function receive(message) {
  if (message.theme) {
    editorTheme = message.theme === "light" ? "light" : "dark";
    document.documentElement.dataset.theme = editorTheme;
    for (const view of views.values()) view.dispatch({ effects: themeSlot.reconfigure(themeExtensions()) });
  }
  if (message.context && JSON.stringify(completionContext) !== JSON.stringify(message.context)) {
    completionContext = message.context;
    if (message.action === "context") for (const view of views.values()) forceLinting(view);
  }
  if (message.name) document.getElementById("editor-component-name").textContent = `${message.name} · Custom Properties`;
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
