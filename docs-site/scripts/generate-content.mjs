import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { descriptions, recipe } from '../content/components.mjs';

const root = fileURLToPath(new URL('../../', import.meta.url));
const site = path.join(root, 'docs-site');
const read = file => fs.readFileSync(path.join(root, file), 'utf8').replace(/^\uFEFF/, '').replaceAll('\r\n', '\n');
const catalog = JSON.parse(read('docs-site/content/catalog.json'));
const catalogSources = JSON.parse(read('docs-site/content/catalog-sources.json'));
for (const [file, hash] of Object.entries(catalogSources)) {
  if (createHash('sha256').update(read(file)).digest('hex') !== hash)
    throw new Error(`Property catalog is stale after ${file} changed. Run npm run catalog from docs-site with .NET 10, then commit the refreshed catalog.`);
}
const runtime = JSON.parse(read('src/Forma.Builder/DesignerWeb/runtime-script-properties.json'));
const assistance = read('src/Forma.Builder/Frontend/code-assistance.mjs');
const { propertiesFor } = new Function('runtimeCatalog', assistance.slice(assistance.indexOf('const common'), assistance.indexOf('const methods')).replaceAll('export ', '') + '; return { propertiesFor };')(runtime);
const types = read('src/Forma.Builder/Frontend/javascript-types.mjs');
const { propertyType } = new Function(types.slice(types.indexOf('const numbers'), types.indexOf('export function formaDeclarations')).replaceAll('export ', '') + '; return { propertyType };')();
const reference = read('docs/component-reference.md');
const base = ('/' + (process.env.DOCS_BASE || '').replace(/^\/+|\/+$/g, '')).replace(/\/$/, '');
const url = slug => `${base}/${slug}/`;
const fence = String.fromCharCode(96).repeat(3);
const escape = value => String(value ?? '—').replaceAll('|', '\\|').replaceAll('\n', ' ');
const code = value => '`' + escape(value) + '`';
const guides = [
  ['builder-first-interaction', 'Start here', 'Your first form, from blank canvas to working app.', 'Getting started'],
  ['solution-explorer', 'Solution Explorer', 'Find forms, edit sources, and organize project files.', 'Getting started'],
  ['project-files', 'Projects & saving', 'Multiple forms, embedded assets, .forma files, and history.', 'Getting started'],
  ['components-and-scripting', 'Using components', 'Build, style, and script the current component library.', 'Getting started'],
  ['component-customization', 'Scripts & Custom Properties', 'CSS, JavaScript, JSON, code completion, events, and cleanup.', 'Scripting'],
  ['global-scripts', 'main.js & providers', 'Split shared state into modules. Keep your entry file civilized.', 'Scripting'],
  ['runtime-properties', 'Runtime properties', 'Read, write, and bind supported properties with exact types.', 'Scripting'],
  ['properties-implementation', 'Inspector properties', 'Geometry, styles, docking, anchoring, and editing rules.', 'Reference'],
  ['component-reference', 'Complete reference', 'The original consolidated model and property reference.', 'Reference'],
  ['toolbox-implementation', 'Implementation status', 'What works today and which features are still pending.', 'Reference'],
  ['architecture', 'Architecture', 'MVVM, project structure, and runtime boundaries.', 'Reference'],
  ['roadmap-stages', 'Roadmap', 'The planned batches and the current implementation status.', 'Reference'],
];
const guideNames = new Set(guides.map(([id]) => id));
function links(source) {
  return source.replace(/\]\(([^)]+)\)/g, (match, target) => {
    if (/^(https?:|#|mailto:)/.test(target)) return match;
    const [filename, fragment] = target.split('#');
    if (filename.startsWith('../src/')) return `](https://github.com/amabee/Forma/blob/master/${filename.slice(3)}${fragment ? '#' + fragment : ''})`;
    const id = path.basename(filename, '.md');
    if (guideNames.has(id)) return `](${url('guides/' + id)}${fragment ? '#' + fragment : ''})`;
    if (filename.startsWith('examples')) return `](${url('guides/recipes')}${fragment ? '#' + fragment : ''})`;
    if (filename.endsWith('.md')) return `](${url('guides/roadmap-stages')})`;
    return match;
  });
}
const generated = path.join(site, 'src/generated');
fs.mkdirSync(generated, { recursive: true });
// Remove stale generated documents only; handwritten content is kept elsewhere.
for (const entry of fs.readdirSync(generated)) if (entry.endsWith('.md') || entry === 'navigation.json') fs.unlinkSync(path.join(generated, entry));
const nav = [], search = [];
function write(slug, title, description, group, body, extra = {}) {
  const metadata = { slug, title, description, group, ...extra };
  const content = links(body);
  fs.writeFileSync(path.join(generated, slug.replaceAll('/', '--') + '.md'), `---\n${Object.entries(metadata).map(([key, value]) => `${key}: ${JSON.stringify(value)}`).join('\n')}\n---\n\n${content}\n`);
  nav.push(metadata);
  search.push({ ...metadata, url: url(slug), text: content.replace(/\x60{3}[\s\S]*?\x60{3}/g, match => match.replace(/\x60{3}\w*/g, '')).replace(/[#*`|]/g, '').replace(/\s+/g, ' ') });
}
for (const [id, title, description, group] of guides) write('guides/' + id, title, description, group, read('docs/' + id + '.md').replace(/^# .*\n/, ''));

const toolbox = read('src/Forma.Builder/DesignerWeb/index.html');
const kinds = [...new Set([...toolbox.matchAll(/data-kind="([^"]+)"/g)].map(match => match[1]))];
const implemented = ['form', ...kinds];
const groups = new Map([['form', 'Layouts']]);
for (const section of toolbox.matchAll(/<details open class="tool-group">([\s\S]*?)<\/details>/g)) {
  const label = /<summary>([^<]+)/.exec(section[1])?.[1];
  const group = label === 'Layout' ? 'Layouts' : label === 'Components' ? 'Services' : 'Controls';
  for (const match of section[1].matchAll(/data-kind="([^"]+)"/g)) groups.set(match[1], group);
}
const alias = { number: 'value', dateValue: 'value', color: 'value', gridColumns: 'columns', gridRows: 'rows' };
const notes = {
  text: 'Display text; use value to read live text input.', value: 'Typed live value. Text inputs are written with text; a value binding writes through that alias.',
  source: 'Image source. Choose an image in the inspector; use source rather than text in scripts.', interval: 'Milliseconds; integer 10–3,600,000. Only fires while Timer is enabled.',
  selectedPath: 'Native picker result; read-only. Choose a path through the picker.', selectedIndex: 'Zero-based; -1 clears selection.', selectedTab: 'Zero-based active tab/section.',
  layoutSlot: 'One-based parent pane, page, or table cell.', rows: 'Array of string arrays. Each cell must be a string.', columns: 'DataGridView: string array; TableLayoutPanel: column count.',
  items: 'String array; inspector uses one item per line.', tabs: 'String array of page/section captions.', checkedIndices: 'Array of zero-based checked item indices.',
  commandItems: 'Command object array; use camelCase keys. Inspector uses Commands JSON.', targetId: 'Visual control Name or ID; reads return its stable ID.',
  dock: 'Parent-managed edge/fill layout. Takes priority over Anchor.', anchor: 'Comma-separated edges; opposite edges stretch. Use exact option strings.',
  width: 'Configured model width; managed layouts/custom CSS can change rendered size.', height: 'Configured model height; managed layouts/custom CSS can change rendered size.',
  x: 'Parent-content X coordinate. Writes are rejected for managed/docked children.', y: 'Parent-content Y coordinate. Writes are rejected for managed/docked children.',
  page: 'One-based current page.', pageCount: 'Calculated from totalItems and pageSize; read-only.', result: 'Last dialog result; read-only.', isBusy: 'Worker status; read-only.', isOpen: 'Current open status; read-only.',
  gap: 'Spacing between managed children in pixels.', breakpoint: 'Container width below which responsive children stack.', speed: 'Spinner animation duration, in milliseconds.',
  document: 'Structured blocks/runs, not HTML. See Runtime properties.', nodes: 'Tree node objects with unique IDs and optional children.', entries: 'Property entry objects with string names/values.',
};
for (const kind of implemented) {
  const item = catalog.find(item => item.kind === kind);
  if (!item || !descriptions[kind]) throw new Error(`Missing catalog/editorial entry: ${kind}`);
  const usage = recipe(kind);
  const readable = propertiesFor(kind, 'get'), writable = new Set(propertiesFor(kind, 'set'));
  const bindingWritable = key => writable.has(key) || key === 'value' && ['textbox','searchbox','passwordbox','textarea','maskedtextbox'].includes(kind);
  const descriptors = item.inspector.filter((property, index, list) => list.findIndex(other => other.id === property.id) === index);
  const specific = descriptors.filter(property => property.kind);
  const shared = descriptors.filter(property => !property.kind);
  const inspectorTable = properties => '| Field | Inspector key | Editor | Accepted values / range | Runtime key |\n| --- | --- | --- | --- | --- |\n' + properties.map(property => {
    const key = alias[property.id] ?? property.id;
    const values = property.options?.map(code).join(', ') || (property.min != null || property.max != null ? `${property.min ?? '…'}–${property.max ?? '…'}` : '—');
    return `| ${escape(property.label)}${property.readOnly ? ' (read-only)' : ''} | ${code(property.id)} | ${property.editor} | ${values} | ${readable.includes(key) ? code(key) : 'Designer only'} |`;
  }).join('\n');
  const runtimeTable = '| Key | Value type | get | set | Binding | Meaning / restrictions |\n| --- | --- | --- | --- | --- | --- |\n' + readable.map(key => {
    const descriptor = descriptors.find(property => (alias[property.id] ?? property.id) === key);
    const type = propertyType(kind, key, descriptor);
    return `| ${code(key)} | ${code(type)} | Yes | ${writable.has(key) ? 'Yes' : '—'} | ${bindingWritable(key) ? 'Read/write' : 'Read-only'} | ${escape(notes[key] ?? (descriptor?.min != null ? `Range ${descriptor.min}–${descriptor.max}. Invalid types are rejected; numeric values clamp.` : descriptor?.label ?? 'Supported runtime property.'))} |`;
  }).join('\n');
  let section = reference.split(/^### /m).slice(1).find(block => block.split('\n')[0].split(/, | and /).includes(item.name));
  // Detailed tables already come from the current catalogs; keep editorial prose only.
  if (section?.includes('**Kind:**')) section = undefined;
  const model = '| Property | C# type | Constructor default | Writable |\n| --- | --- | --- | --- |\n' + item.model.map(property => `| ${code(property.name)} | ${code(property.type)} | ${code(JSON.stringify(property.value))} | ${property.writable ? 'Yes' : 'No'} |`).join('\n');
  const events = ({ timer: 'Tick', datagridview: 'row-selection', chip: 'chip-remove', splitbutton: 'primary-click, command-item', dropdownbutton: 'command-item', breadcrumb: 'navigate', sidenavigation: 'navigate' })[kind] ?? 'Relevant DOM events and shared lifecycle events';
  const body = `## What it does\n\n${descriptions[kind]}\n\n${item.tray ? 'This is a **component tray service**. It appears below the form and does not take a canvas rectangle.' : kind === 'form' ? 'Each project can contain multiple forms. Add a form with Ctrl+N; create a separate project with Ctrl+Shift+N.' : 'This is a **visual component**. Drag it from Toolbox, or double-click to insert it.'}\n\n## Set it up\n\n1. ${kind === 'form' ? 'Select your form in the designer or Solution Explorer.' : `Add **${item.name}** from Toolbox.`}\n2. Set its **Name** to ${code('sample')} for the examples below. Names are case-sensitive.\n3. Configure the component-specific properties below.\n4. Right-click the component and choose **View Script** to add behavior. Use **View CSS** for scoped styles and **View Custom Properties** for JSON values.\n5. Save sources, save the project, and open a fresh Preview. Scripts execute in Preview.\n\n## JavaScript example\n\n${usage.notes}\n\n${fence}js\n${usage.code}\n${fence}\n\n## Component properties\n\n${specific.length ? inspectorTable(specific) : 'This component uses the shared inspector fields below; it has no additional component-specific inspector fields.'}\n\n## Runtime property API\n\nUse ${code('forma.get("sample", key)')}, ${code('forma.set("sample", key, value)')}, or ${code('forma.bind("sample", key)')}. Read a binding with ${code('.value')}; write only when the Binding column permits it. Numbers and booleans must keep their types. Arrays are passed directly, not as JSON strings.\n\n${runtimeTable}\n\nSet commands cross the native bridge asynchronously. An immediate get after set may return the previous value. Observe a binding or use the assigned value locally. Getter arrays are copies. Geometry is configured model geometry; layout and CSS may override rendered bounds.\n\n## Shared inspector fields\n\n${inspectorTable(shared)}\n\n## Events and lifecycle\n\n**Component hooks:** ${events}. ${kind === 'backgroundworker' ? 'Core worker events are not automatic JavaScript callbacks.' : ''}\n\n${item.tray ? 'Tray services have lifecycle events even without a visible control.' : 'Only applicable browser events fire: a control without text input will not produce native input events just because a handler is registered.'} Event names are case-insensitive. Put handlers on the component producing the event; a Timer Tick handler belongs to the Timer. See [the event guide](${url('guides/events')}) for payloads, validation, cleanup, and startup order.\n\n## C# example\n\nUse this in a configured Forma C# host, not in script.js. The renderer is your host's WebView2Renderer instance.\n\n${fence}csharp\n${['timer', 'backgroundworker'].includes(kind) ? 'using ' : ''}var sample = new ${kind === 'form' ? 'Forma.Core.Form' : 'Forma.Core.Controls.' + item.name}\n{\n    Name = "sample"\n};\nawait renderer.RenderAsync(sample);\n${fence}\n\n## C# model reference\n\nThese are **Core constructor defaults**, not necessarily the Builder’s drop-time styles or size. Appearance fields belong to Builder Appearance. C# events are not all forwarded to JavaScript.\n\n${model}\n\n**Core methods:** ${(item.methods ?? []).map(method => code(method.name + "(" + method.parameters.join(", ") + ")")).join(", ")}.\n\n**Core events:** ${item.events.map(code).join(', ')}.\n\n${section ? '## Additional behavior notes\n\n' + section.slice(section.indexOf('\n') + 1).split(/^## /m)[0].trim() : ''}\n\n## Related guides\n\n- [Runtime properties](${url('guides/runtime-properties')})\n- [Scripts, styles & custom JSON](${url('guides/component-customization')})\n- [main.js, imports & providers](${url('guides/global-scripts')})\n- [Implementation status](${url('guides/toolbox-implementation')})\n`;
  write('components/' + kind, item.name, descriptions[kind], item.tray ? 'Services' : groups.get(kind) === 'Services' ? 'Controls' : groups.get(kind) ?? 'Controls', body, { kind, tray: item.tray });
}

const customization = read('docs/component-customization.md');
write('guides/events', 'Events & lifecycle', 'Listen where the event happens. Know when your code runs.', 'Scripting', customization.slice(customization.indexOf('## JavaScript events') + '## JavaScript events'.length));
const examples = fs.readdirSync(path.join(root, 'docs/examples')).filter(file => /\.(js|cs)$/.test(file));
write('guides/recipes', 'Copyable recipes', 'Small scripts for real tasks. Name your controls, then make them work.', 'Scripting', 'These examples come from the tested repository recipes. Read the source comments for required component names and where each script belongs.\n\n' + examples.map(file => `## ${file.replace(/\.(js|cs)$/, '').replaceAll('-', ' ')}\n\n${fence}${file.endsWith('.cs') ? 'csharp' : 'js'}\n${read('docs/examples/' + file)}\n${fence}`).join('\n\n'));
write('guides/troubleshooting', 'Troubleshooting', 'Empty input? NaN? Your script deserves answers.', 'Getting started', `## A value read at startup stays empty\n\n${fence}js\nconst name = forma.bind("firstName", "value");\nforma.on("Click", () => alert(name.value));\n${fence}\n\nA get returns a snapshot. A binding keeps a live reference. Read the ref’s value inside the event.\n\n## parseInt returns NaN\n\nParse the current input, not the ref itself and not an empty startup value:\n\n${fence}js\nconst age = forma.bind("ageInput", "value");\nforma.on("Click", () => {\n  const number = Number.parseInt(age.value, 10);\n  if (!Number.isFinite(number)) return;\n  console.log(number);\n});\n${fence}\n\n## Timer never fires\n\nPut Tick in the Timer’s script. Enable that Timer by its exact name. interval is a number, such as 1000, not "1000". Bind enabled with .value; do not replace a ref object with a boolean.\n\n## Avatar image does not change\n\nSet source, not text. Use a FilePicker’s selectedPath or Choose Image in the inspector. Verify the path exists.\n\n## My provider cannot be found\n\nImport the provider file in main.js before component scripts consume it. Module names are exact. Do not call use at the top of an imported helper when main.js has not registered the provider yet. See [providers](${url('guides/global-scripts')}).\n\n## I cannot freely move a child\n\nFlow, stack, table, responsive, and shell layouts manage child positions. Dragging reorders or assigns cells. Free-position Panel and GroupBox support X/Y; Dock and Anchor may still constrain the child.\n\n## Preview keeps the old script\n\nSave the script and start a fresh Preview. A running Preview owns an isolated snapshot. Saving the .forma project stores applied sources, not unsaved editor drafts or runtime values.\n\n## DataGrid rows keep disappearing\n\nUse inspector Columns/Rows for saved starting data. Runtime setters edit only Preview. Arrays returned by get are copies; use set or the grid’s row/cell methods to apply edits.\n\n## A property is rejected\n\nCheck the component’s Runtime property API table. Inspector fields can use different keys or formats. Numeric strings are not numbers; enums require exact supported values.\n\n## Styles ignore inspector changes\n\nCustom CSS overrides inspector appearance. Remove copied declarations from component.css if you want the inspector to control them again. Every selector starts with :host.\n`);

nav.sort((a, b) => a.group.localeCompare(b.group) || a.title.localeCompare(b.title));
fs.writeFileSync(path.join(generated, 'navigation.json'), JSON.stringify(nav, null, 2));
fs.writeFileSync(path.join(site, 'public/search-index.json'), JSON.stringify(search));
console.log(`Generated ${implemented.length} component pages and ${nav.length - implemented.length} guides.`);
