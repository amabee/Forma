import { propertiesFor, eventNames } from "./code-assistance.mjs";

const numbers = new Set("x y width height minimumWidth minimumHeight maximumWidth maximumHeight layoutSlot borderWidth borderRadius opacity fontSize lineHeight letterSpacing marginTop marginRight marginBottom marginLeft paddingTop paddingRight paddingBottom paddingLeft tabIndex interval minimum maximum increment speed lines maxLength strokeWidth thickness stars columns rowCount initialDelay showDuration totalItems pageSize page pageCount selectedIndex selectedTab selectedRow sortColumn duration gap breakpoint".split(" "));
const booleans = new Set("enabled visible focusable readOnly password checked isActive headerVisible canCancel isOpen maskCompleted isBusy workerReportsProgress workerSupportsCancellation sortingEnabled filteringEnabled dismissible removable isRemoved showText primaryEnabled expanded".split(" "));
const enums = {
  dock: ["none", "top", "bottom", "left", "right", "fill"],
  anchor: ["none", "top,left", "top,right", "bottom,left", "bottom,right", "top,left,right", "bottom,left,right", "top,bottom,left", "top,bottom,right", "top,bottom,left,right", "top", "bottom", "left", "right", "top,bottom", "left,right"],
  borderStyle: ["none", "solid", "dashed", "dotted"], fontStyle: ["normal", "italic"], textAlign: ["left", "center", "right"],
  sizeMode: ["contain", "cover", "fill"], sortDirection: ["ascending", "descending"], scrollDirection: ["both", "horizontal", "vertical"],
};
const union = values => values.map(value => JSON.stringify(value)).join(" | ");
export function propertyType(kind, key, descriptor) {
  if (descriptor?.options?.length) return union(descriptor.options);
  if (key === "value") return ["numericupdown", "slider", "progressbar", "circularprogress", "rating"].includes(kind) ? "number" : "string";
  if (key === "columns" && kind === "datagridview") return "string[]";
  if (numbers.has(key)) return "number";
  if (booleans.has(key)) return "boolean";
  if (["items", "tabs", "expandedNodes"].includes(key)) return "string[]";
  if (key === "checkedIndices") return "number[]";
  if (key === "rows") return "string[][]";
  if (key === "nodes") return "FormaTreeNode[]";
  if (key === "entries") return "FormaPropertyEntry[]";
  if (key === "document") return "FormaRichBlock[]";
  if (key === "commandItems") return "FormaCommand[]";
  if (key === "variant") return union(kind === "toast" ? ["neutral", "info", "success", "warning", "caution", "error", "danger"] : ["neutral", "info", "success", "warning", "danger"]);
  if (key === "shape") return union(kind === "skeleton" ? ["text", "rectangle", "circle"] : ["circle", "rounded", "square"]);
  if (enums[key]) return union(enums[key]);
  return "string";
}

export function formaDeclarations(controls = [], custom = {}, shared = []) {
  const kinds = [...new Set(controls.map(control => control.kind))];
  const maps = kinds.map(kind => `${JSON.stringify(kind)}: { ${propertiesFor(kind, "get").map(key => `${JSON.stringify(key)}: ${propertyType(kind, key, controls.find(control => control.kind === kind)?.properties?.find(property => property.key === key))}`).join("; ")} }`).join(";\n");
  const writes = kinds.map(kind => `${JSON.stringify(kind)}: ${union(propertiesFor(kind, "set")) || "never"}`).join(";\n");
  const names = controls.flatMap(control => [control.name, control.id].filter(Boolean).map(name => `${JSON.stringify(name)}: ${JSON.stringify(control.kind)}`)).join(";\n");
  const customType = value => {
    if (value === null) return "null";
    if (Array.isArray(value)) return `Array<${[...new Set(value.map(customType))].join(" | ") || "unknown"}>`;
    if (typeof value === "object") return `{ ${Object.entries(value).map(([key, item]) => `${JSON.stringify(key)}: ${customType(item)}`).join("; ")} }`;
    return typeof value;
  };
  return `
interface FormaReadOnlyRef<T> { readonly value: T; subscribe(callback: (value: T, previous: T) => void, options?: { immediate?: boolean }): () => void; }
interface FormaRef<T> extends FormaReadOnlyRef<T> { value: T; }
interface FormaTreeNode { id: string; text: string; children?: FormaTreeNode[]; }
interface FormaPropertyEntry { name: string; value: string; category?: string; readOnly?: boolean; }
interface FormaRichRun { text: string; bold?: boolean; italic?: boolean; underline?: boolean; }
interface FormaRichBlock { kind: "paragraph" | "bullet" | "number"; runs: FormaRichRun[]; }
interface FormaCommand { id?: string; text?: string; separator?: boolean; enabled?: boolean; checked?: boolean; checkOnClick?: boolean; items?: FormaCommand[]; }
interface FormaProperties { ${maps} }
interface FormaWritableProperties { ${writes} }
interface FormaControls { ${names} }
type FormaModules = typeof import("./global-script").__formaModules;
type FormaEvent<E extends string> = Lowercase<E> extends keyof HTMLElementEventMap ? HTMLElementEventMap[Lowercase<E>] : Lowercase<E> extends "doubleclick" ? MouseEvent : Lowercase<E> extends "mousewheel" ? WheelEvent : CustomEvent<{ row?: number; index?: number; text?: string; itemId?: string }>;
interface FormaApi {
  /** Read a current runtime property. A plain return value is a snapshot. */
  get<N extends keyof FormaControls, P extends keyof FormaProperties[FormaControls[N]]>(name: N, property: P): FormaProperties[FormaControls[N]][P];
  get(name: string, property: string): unknown;
  /** Update the Preview model asynchronously. */
  set<N extends keyof FormaControls, P extends keyof FormaProperties[FormaControls[N]] & FormaWritableProperties[FormaControls[N]]>(name: N, property: P, value: FormaProperties[FormaControls[N]][P]): void;
  set(name: string, property: string, value: unknown): void;
  /** Live property reference. Read .value when needed. */
  bind<N extends keyof FormaControls, P extends keyof FormaProperties[FormaControls[N]]>(name: N, property: P): P extends FormaWritableProperties[FormaControls[N]] ? FormaRef<FormaProperties[FormaControls[N]][P]> : FormaReadOnlyRef<FormaProperties[FormaControls[N]][P]>;
  bind(name: string, property: string): FormaRef<unknown>;
  ref<T>(value: T): FormaRef<T>;
  reactive<T extends object>(value: T): T;
  computed<T>(getter: () => T): FormaReadOnlyRef<T>;
  effect(callback: () => void): () => void;
  watch<T>(source: FormaReadOnlyRef<T> | (() => T), callback: (value: T, previous: T) => void, options?: { immediate?: boolean }): () => void;
  on<E extends ${union(eventNames)} | Lowercase<${union(eventNames)}> | (string & {})>(event: E, callback: (event: FormaEvent<E>) => void | Promise<void>): void;
  provide<T>(name: string, value: T): T;
  use<N extends keyof FormaModules>(name: N): FormaModules[N];
  use(name: string): unknown;
  readonly shared: typeof import("./global-script").__formaShared & Record<string, any>;
  find(name: string): HTMLElement;
  cleanup(callback: () => void): void;
  validate(): boolean; submit(): boolean; reset(): boolean;
  showDialog(name: string): void;
  showToast(name: string, options?: { text?: string; variant?: "neutral" | "info" | "success" | "warning" | "caution" | "error" | "danger"; position?: string; duration?: number; dismissible?: boolean }): void;
  closeToast(name: string): void;
  addRow(name: string, cells: string[]): void;
  updateRow(name: string, rowIndex: number, cells: string[]): void;
  removeRow(name: string, rowIndex: number): void;
  clearRows(name: string): void;
  setCell(name: string, rowIndex: number, columnIndex: number, value: string): void;
}
declare const forma: FormaApi;
declare const api: FormaApi;
declare const component: { id: string; name: string; element: HTMLElement; properties: ${customType(custom)}; characteristics: ${customType(custom)} };
`;
}
