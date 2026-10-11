// Shared by the designer and runtime preview. Source is stored in the .forma file.
(() => {
  const styles = new Map(),
    behaviors = new Map(),
    failedStyles = new Map(),
    failedBehaviors = new Map();
  let currentState;
  const modules = new Map();
  let moduleSession;
  let sharedState, globalSignature, globalKey, globalFailed = false;
  let runtimeActive = false, runtimeEpoch = 0, eventSequence = 0;
  let controlsById = new Map();
  // forma.showForm promises waiting for their window to close, by request ID.
  const pendingForms = new Map();
  let formRequests = 0;
  const eventMetadata = new WeakMap(), propertyCache = new Map();
  const eventSession = Math.random().toString(36).slice(2);
  const eventLabels = Object.fromEntries(("Click DoubleClick MouseDown MouseUp MouseMove MouseEnter MouseLeave MouseOver MouseOut MouseWheel KeyDown KeyUp KeyPress Input Change BeforeInput Focus Blur FocusIn FocusOut Load Ready Created Mounted Updated Destroyed Resize Move Layout DragStart Drag DragEnd DragEnter DragOver DragLeave Drop Copy Cut Paste Validating Validated Submit Reset PropertyChanged Tick").split(" ").map(name => [({ doubleclick: "dblclick", mousewheel: "wheel" })[name.toLowerCase()] ?? name.toLowerCase(), name]));
  const textInputs = new Set(["textbox", "searchbox", "passwordbox", "textarea", "maskedtextbox"]);
  const numericInputs = new Set(["numericupdown", "slider", "progressbar", "circularprogress", "rating"]);
  function canonicalProperty(item, property) {
    if (textInputs.has(item.kind) && property === "text") return "value";
    if (numericInputs.has(item.kind) && property === "number") return "value";
    if (["datepicker", "timepicker", "datetimepicker"].includes(item.kind) && property === "dateValue") return "value";
    if (item.kind === "colorpicker" && property === "color") return "value";
    return property;
  }
  const cloneValue = value => value && typeof value === "object" ? JSON.parse(JSON.stringify(value)) : value;
  const equalValue = (a, b) => Object.is(a, b) || a && b && typeof a === "object" && typeof b === "object" && JSON.stringify(a) === JSON.stringify(b);
  function portableValue(value, ancestors = new Set()) {
    if (value === null || ["string", "boolean"].includes(typeof value)) return value;
    if (typeof value === "number") return Number.isFinite(value) ? value : null;
    if (typeof value === "bigint") return String(value);
    if (typeof value !== "object" || value instanceof Node || value instanceof Event || ancestors.has(value)) return undefined;
    ancestors.add(value);
    const result = Array.isArray(value) ? value.map(entry => portableValue(entry, ancestors) ?? null) : Object.fromEntries(Object.keys(value).map(key => [key, portableValue(value[key], ancestors)]).filter(([, entry]) => entry !== undefined));
    ancestors.delete(value); return result;
  }
  function eventSource(event, fallbackId) {
    const data = eventMetadata.get(event);
    if (data?.sourceId) return controlsById.get(data.sourceId) ?? data.sourceIdentity;
    for (const node of event.composedPath?.() ?? [event.target]) {
      const id = node?.dataset?.componentSource ?? node?.dataset?.commandSource ?? node?.id;
      if (controlsById.has(id)) return controlsById.get(id);
    }
    return controlsById.get(fallbackId);
  }
  function metadata(event, origin) {
    if (!eventMetadata.has(event)) eventMetadata.set(event, { eventId: `evt_${eventSession}_${++eventSequence}`, timestamp: Date.now(), origin: origin ?? (event.isTrusted ? "user" : "programmatic"), handled: false, propagationStopped: false });
    return eventMetadata.get(event);
  }
  function notifyProperty(id, property, value, previousValue, origin) {
    const element = document.getElementById(id);
    if (!runtimeActive || !element || !controlsById.has(id)) return;
    const event = new CustomEvent("propertychanged", { bubbles: true, detail: { property, value: cloneValue(value), previousValue: cloneValue(previousValue) } });
    Object.assign(metadata(event, origin), { sourceId: id, property, value: cloneValue(event.detail.value), previousValue: cloneValue(event.detail.previousValue) });
    dispatchComponent(id, event);
  }
  function dispatchComponent(id, event) {
    const element = document.getElementById(id);
    if (!element) return;
    metadata(event).sourceId = id;
    element.dispatchEvent(event);
    const rootId = currentState?.id ?? currentState?.controls.find(control => control.kind === "form")?.id;
    const root = document.getElementById(rootId);
    // Tray components belong to the form in the model, outside its DOM subtree.
    if (event.bubbles && root && root !== element && !root.contains(element) && !metadata(event).propagationStopped && !event.cancelBubble) root.dispatchEvent(event);
  }
  function inputProperty(item, event) {
    if (["checkbox", "radiobutton", "switch", "togglebutton", "chip"].includes(item.kind)) return "checked";
    if (["checkboxgroup", "chipgroup", "checkedlistbox"].includes(item.kind)) return "checkedIndices";
    if (["combobox", "listbox", "listview", "radiogroup", "segmentedcontrol", "buttongroup", "breadcrumb", "sidenavigation"].includes(item.kind)) return "selectedIndex";
    if (["filepicker", "folderpicker"].includes(item.kind)) return "selectedPath";
    if (["tabcontrol", "accordion"].includes(item.kind)) return "selectedTab";
    if (textInputs.has(item.kind) || numericInputs.has(item.kind) || ["datepicker", "timepicker", "datetimepicker", "colorpicker"].includes(item.kind)) return "value";
    if (event.type === "row-selection") return "selectedRow";
    return undefined;
  }
  function liveInputValue(item, property, event) {
    if (property === "selectedRow" && typeof event.detail?.row === "number") return event.detail.row;
    const element = document.getElementById(item.id);
    if (element?._selectionProperties) {
      const key = property === "value" ? "number" : property;
      if (key in element._selectionProperties) return cloneValue(element._selectionProperties[key]);
    }
    const reader = [...behaviors.values()].find(record => record.api)?.api;
    if (reader) return reader.get(item.id, property);
    return undefined;
  }
  function captureInput(event) {
    if (!runtimeActive) return;
    const item = eventSource(event);
    if (!item) return;
    const property = inputProperty(item, event);
    if (!property) return;
    try {
      const rawValue = liveInputValue(item, property, event), value = typeof rawValue === "number" && !Number.isFinite(rawValue) ? null : rawValue, cache = propertyCache.get(item.id);
      const previousValue = cache?.values.get(property);
      Object.assign(metadata(event), { property, value: cloneValue(value), previousValue: cloneValue(previousValue) });
      if (cache && !equalValue(value, previousValue)) {
        cache.pendingNative.set(property, { value: cloneValue(value), previousValue: cache.pendingNative.get(property)?.previousValue ?? cloneValue(previousValue) });
        cache.values.set(property, cloneValue(value));
        const epoch = runtimeEpoch;
        queueMicrotask(() => { if (epoch === runtimeEpoch) notifyProperty(item.id, property, value, previousValue, metadata(event).origin); });
      }
    } catch (error) { report(item.id, error); }
  }
  function refreshProperties(state, pending) {
    for (const id of propertyCache.keys()) if (!controlsById.has(id)) propertyCache.delete(id);
    for (const item of state.controls) {
      const signature = behaviors.get(item.id)?.snapshot ?? snapshot(item), previous = propertyCache.get(item.id);
      if (previous?.signature === signature && !(state.propertyOrigins ?? []).some(entry => entry.id === item.id)) continue;
      const values = new Map();
      const excluded = new Set(["id", "kind", "parentId", "component", "customization", "globalScript", "scriptProperties"]);
      for (const key of new Set([...(item.scriptProperties ?? Object.keys(item)), "text", "enabled", "visible", ...(textInputs.has(item.kind) ? ["text"] : []), ...(numericInputs.has(item.kind) ? ["number"] : []), ...(["datepicker", "timepicker", "datetimepicker"].includes(item.kind) ? ["dateValue"] : []), ...(item.kind === "colorpicker" ? ["color"] : [])])) {
        if (!excluded.has(key) && Object.hasOwn(item, key)) values.set(canonicalProperty(item, key), cloneValue(item[key]));
      }
      const pendingNative = previous?.pendingNative ?? new Map();
      for (const [property, change] of pendingNative) {
        const acknowledged = (state.propertyOrigins ?? []).some(entry => entry.id === item.id && canonicalProperty(item, entry.property) === property);
        if (!acknowledged && equalValue(values.get(property), change.previousValue)) values.set(property, change.value);
        else pendingNative.delete(property);
      }
      propertyCache.set(item.id, { signature, values, pendingNative });
      if (!previous) continue;
      for (const [property, value] of values) {
        if (!previous.values.has(property) || equalValue(value, previous.values.get(property))) continue;
        const origin = (state.propertyOrigins ?? []).find(change => change.id === item.id && canonicalProperty(item, change.property) === property)?.origin ?? "native";
        const oldValue = previous.values.get(property);
        pending.push(() => notifyProperty(item.id, property, value, oldValue, origin));
      }
    }
  }
  const aliases = { doubleclick: "dblclick", mousewheel: "wheel" };
  const lifecycle = new Set([
    "created",
    "mounted",
    "ready",
    "load",
    "updated",
    "destroyed",
    "resize",
    "move",
    "layout",
    "validating",
    "validated",
    "propertychanged",
  ]);
  const eventName = (name) =>
    aliases[String(name).toLowerCase()] ?? String(name).toLowerCase();
  function emit(record, name, detail = {}, cancelable = false) {
    const event = new CustomEvent(name, { detail, cancelable });
    const owner = controlsById.get(record.item.ownerId ?? record.item.id) ?? record.sourceIdentity ?? record.item;
    Object.assign(metadata(event, "system"), { sourceId: owner.id, sourceIdentity: { id: owner.id, name: owner.name, kind: owner.kind, parentId: owner.parentId } });
    return record.element.dispatchEvent(event);
  }
  function validate(record) {
    if (!emit(record, "validating", {}, true)) return false;
    const element = record.validationElement ?? record.element;
    const inputs = element.matches("input,select,textarea")
      ? [element]
      : [...element.querySelectorAll("input,select,textarea")];
    if (!inputs.every((input) => input.checkValidity())) return false;
    emit(record, "validated");
    return true;
  }
  function snapshot(item) {
    return JSON.stringify(item);
  }
  const report = (id, error) =>
    window.forma.send({
      type: "custom",
      id,
      event: "error",
      payload: { message: String(error.message ?? error) },
    });
  const field = (object, name) =>
    object?.[name] ?? object?.[name[0].toUpperCase() + name.slice(1)];

  function selectorsOf(source) {
    const selectors = [];
    let start = 0,
      depth = 0,
      quote = "";
    for (let index = 0; index < source.length; index++) {
      const char = source[index];
      if (char === "\\") {
        index++;
        continue;
      }
      if (quote) {
        if (char === quote) quote = "";
        continue;
      }
      if (char === '"' || char === "'") {
        quote = char;
        continue;
      }
      if (char === "(" || char === "[") depth++;
      if (char === ")" || char === "]") depth--;
      if (char === "," && depth === 0) {
        selectors.push(source.slice(start, index).trim());
        start = index + 1;
      }
    }
    selectors.push(source.slice(start).trim());
    return selectors;
  }

  function scopedCss(id, source, popupOnly = false, appearance = {}) {
    const scratch = document.createElement("style");
    scratch.media = "not all";
    scratch.textContent = source;
    document.head.appendChild(scratch);
    const oldToastDefaults = appearance.kind === "toast" && source.includes("These are your current appearance defaults; edit them as needed.");
    const color = value => { const probe = document.createElement("span"); probe.style.color = value ?? ""; return probe.style.color; };
    try {
      const compile = (rules) =>
        Array.from(rules)
          .map((rule) => {
            if (rule.type === 1) {
              const selectors = selectorsOf(rule.selectorText);
              if (
                selectors.some((selector) => !/^:host(?![\w-(])/.test(selector))
              )
                throw new Error(
                  "Every selector must start with :host, such as :host:hover or :host input.",
                );
              // Nonvisual component IDs belong to designer tray entries, not their visible runtime UI.
              const hosts = popupOnly
                ? [`[data-component-source="${id}"]`]
                : [`[id="${id}"]`, `[data-component-source="${id}"]`];
              const selector = selectors
                .flatMap((selector) =>
                  hosts.map((host) => selector.replace(/:host\b/g, host)),
                )
                .join(", ");
              // Overrides must take priority over the inspector's inline appearance.
              const declarations = Array.from(
                { length: rule.style.length },
                (_, index) => {
                  const name = rule.style[index];
                  // Older generated toast templates copied blue button colors. Keep genuine custom overrides.
                  if (oldToastDefaults && selectors.every(selector => selector === ":host")) {
                    const value = rule.style.getPropertyValue(name);
                    const expected = name === "color" ? appearance.foreColor : name === "background-color" ? appearance.backColor : null;
                    if (expected && color(value) === color(expected)) return "";
                    if (name === "border" && appearance.borderColor && color(rule.style.borderColor) === color(appearance.borderColor))
                      return `border-width: ${rule.style.borderWidth} !important; border-style: ${rule.style.borderStyle} !important;`;
                    if (name === "border-color" && appearance.borderColor && color(value) === color(appearance.borderColor)) return "";
                  }
                  return `${name}: ${rule.style.getPropertyValue(name)} !important;`;
                },
              ).join("\n");
              return `${selector} { ${declarations} }`;
            }
            if (rule.type === 4)
              return `@media ${rule.conditionText} { ${compile(rule.cssRules)} }`;
            if (rule.type === 12)
              return `@supports ${rule.conditionText} { ${compile(rule.cssRules)} }`;
            throw new Error(
              "Component styles support :host rules, @media and @supports.",
            );
          })
          .join("\n");
      return compile(scratch.sheet.cssRules);
    } finally {
      scratch.remove();
    }
  }

  function target(state, name) {
    const matches = state.controls.filter(
      (item) => item.id === name || item.name === name,
    );
    if (matches.length !== 1)
      throw new Error(`Control '${name}' must match one Name or ID.`);
    return matches[0];
  }

  function installBehavior(item, state, source, characteristics, events = "") {
    const element = item.globalElement ?? document.getElementById(item.id),
      cleanups = [];
    const values = JSON.parse(characteristics || "{}");
    let scopeActive = true;
    cleanups.push(() => { scopeActive = false; });
    const handles = new Map(), identities = new Map();
    function active() { if (!scopeActive) throw new Error("This component script has been destroyed."); }
    function handle(id, identity = controlsById.get(id)) {
      if (identity) identities.set(id, { name: identity.name, kind: identity.kind });
      if (handles.has(id)) return handles.get(id);
      const reference = Object.freeze({
        get id() { return id; },
        get name() { return (controlsById.get(id) ?? identities.get(id))?.name; },
        get kind() { return (controlsById.get(id) ?? identities.get(id))?.kind; },
        get element() { return document.getElementById(id); },
        get(property) { active(); return api.get(id, property); },
        set(property, value) { active(); return api.set(id, property, value); },
        bind(property) { active(); return api.bind(id, property); },
        on(event, handler) { active(); return listen(event, handler, document.getElementById(id), () => controlsById.get(id)); },
      });
      handles.set(id, reference); return reference;
    }
    function eventPayload(event, subscriberId) {
      const data = metadata(event), source = eventSource(event, subscriberId);
      const path = [], visited = new Set();
      let ancestor = source;
      while (ancestor && !visited.has(ancestor.id)) { visited.add(ancestor.id); path.push(handle(ancestor.id, ancestor)); ancestor = controlsById.get(ancestor.parentId); }
      const overlay = {
        eventId: data.eventId, timestamp: data.timestamp,
        eventName: eventLabels[event.type] ?? event.type,
        id: source?.id ?? subscriberId,
        source: source ? handle(source.id, source) : handle(subscriberId),
        subscriber: handle(subscriberId, controlsById.get(subscriberId) ?? (item.id === subscriberId ? item : undefined)), path,
        originalEvent: event,
        target: event.target,
        currentTarget: event.currentTarget,
        phase: event.eventPhase === Event.CAPTURING_PHASE ? "capture" : source?.id === subscriberId ? "target" : "bubble",
        origin: data.origin, property: data.property, value: cloneValue(data.value), previousValue: cloneValue(data.previousValue),
        modifiers: Object.freeze({ ctrl: !!event.ctrlKey, shift: !!event.shiftKey, alt: !!event.altKey, meta: !!event.metaKey }),
        mouse: typeof event.clientX === "number" ? Object.freeze({ x: event.clientX, y: event.clientY, screenX: event.screenX, screenY: event.screenY, button: event.button, buttons: event.buttons, deltaX: event.deltaX, deltaY: event.deltaY, deltaZ: event.deltaZ, deltaMode: event.deltaMode }) : undefined,
        pointer: typeof event.pointerId === "number" ? Object.freeze({ id: event.pointerId, type: event.pointerType, pressure: event.pressure, isPrimary: event.isPrimary, width: event.width, height: event.height }) : undefined,
        keyboard: typeof event.key === "string" ? Object.freeze({ key: event.key, code: event.code, repeat: event.repeat, location: event.location, isComposing: event.isComposing }) : undefined,
        preventDefault() { event.preventDefault(); },
        stopPropagation() { data.propagationStopped = true; event.stopPropagation(); },
        stopImmediatePropagation() { data.propagationStopped = true; event.stopImmediatePropagation(); },
        toJSON() {
          const identity = reference => ({ id: reference.id, name: reference.name, kind: reference.kind });
          return {
            eventId: data.eventId, eventName: overlay.eventName, type: event.type, timestamp: data.timestamp,
            id: overlay.id, source: identity(overlay.source), subscriber: identity(overlay.subscriber), path: path.map(reference => reference.id),
            property: data.property, value: portableValue(data.value), previousValue: portableValue(data.previousValue), detail: portableValue(event.detail),
            phase: overlay.phase, origin: data.origin, handled: data.handled, defaultPrevented: event.defaultPrevented,
            propagationStopped: data.propagationStopped || event.cancelBubble, cancelable: event.cancelable, bubbles: event.bubbles, isTrusted: event.isTrusted,
            mouse: overlay.mouse, keyboard: overlay.keyboard, pointer: overlay.pointer, modifiers: overlay.modifiers,
          };
        },
      };
      return new Proxy(event, {
        get(target, key) {
          if (key === "handled") return data.handled;
          if (key === "propagationStopped") return data.propagationStopped || event.cancelBubble;
          if (Object.hasOwn(overlay, key)) return overlay[key];
          const value = Reflect.get(target, key, target);
          return typeof value === "function" && key !== "constructor" ? value.bind(target) : value;
        },
        set(target, key, value) {
          if (key === "handled") { if (typeof value !== "boolean") throw new TypeError("handled must be a boolean."); data.handled = value; return true; }
          if (Object.hasOwn(overlay, key)) return false;
          if (key === "cancelBubble" && value) data.propagationStopped = true;
          return Reflect.set(target, key, value, target);
        },
      });
    }
    const rootId = state.id ?? state.controls.find(control => control.kind === "form")?.id ?? item.ownerId ?? state.controls[0]?.id;
    function listen(event, handler, eventTarget, readItem) {
      event = eventName(event);
      if (typeof handler !== "function") throw new Error("Event handler must be a function.");
      if (!eventTarget) throw new Error("Event target is unavailable.");
      const listener = e => {
        if (lifecycle.has(event) && !(e instanceof CustomEvent)) return;
        const owner = readItem();
        if (!lifecycle.has(event) && (!owner || owner.enabled === false || owner.visible === false)) return;
        try { Promise.resolve(handler(eventPayload(e, eventTarget.id || item.ownerId || item.id))).catch(error => report(item.id, error)); }
        catch (error) { report(item.id, error); }
      };
      const options = { capture: ["focus", "blur"].includes(event), passive: false };
      eventTarget.addEventListener(event, listener, options);
      let attached = true;
      const unsubscribe = () => {
        if (!attached) return;
        attached = false; eventTarget.removeEventListener(event, listener, options);
      };
      cleanups.push(unsubscribe);
      return unsubscribe;
    }
    const root = handle(rootId);
    function gridCommand(name, operation, args = {}) {
      const destination = target(currentState, name);
      if (destination.kind !== "datagridview")
        throw new Error("The target must be a DataGridView.");
      window.forma.send({
        type: "custom",
        id: destination.id,
        event: "grid",
        payload: { sourceId: item.ownerId ?? item.id, operation, ...args },
      });
    }
    function cells(value) {
      if (
        !Array.isArray(value) ||
        value.some((cell) => typeof cell !== "string")
      )
        throw new Error("Grid columns and cells must be arrays of strings.");
      return [...value];
    }
    function index(value) {
      if (!Number.isInteger(value) || value < 0)
        throw new Error("Grid indices must be non-negative integers.");
      return value;
    }
    const api = {
      get root() { return root; },
      provide(name, value) {
        if (!item.globalElement) throw new Error("Define shared modules in main.js using forma.provide.");
        if (typeof name !== "string" || !/^[A-Za-z][\w.-]{0,63}$/.test(name)) throw new Error("Use a nonempty module name, e.g. app.");
        if (modules.has(name)) throw new Error(`Module '${name}' is already provided.`);
        if (value === undefined) throw new Error("Provide a defined module value.");
        modules.set(name, value);
        return value;
      },
      use(name) {
        if (!modules.has(name)) throw new Error(`Shared module '${name}' was not provided by main.js.`);
        return modules.get(name);
      },
      get shared() { return sharedState; },
      addRow(name, row) {
        gridCommand(name, "addRow", { row: cells(row) });
      },
      updateRow(name, rowIndex, row) {
        gridCommand(name, "updateRow", {
          index: index(rowIndex),
          row: cells(row),
        });
      },
      removeRow(name, rowIndex) {
        gridCommand(name, "removeRow", { index: index(rowIndex) });
      },
      clearRows(name) {
        gridCommand(name, "clearRows");
      },
      setCell(name, rowIndex, columnIndex, value) {
        if (typeof value !== "string")
          throw new Error("Grid cells must be strings.");
        gridCommand(name, "setCell", {
          rowIndex: index(rowIndex),
          columnIndex: index(columnIndex),
          value,
        });
      },
      on(event, handler) {
        const eventTarget = item.globalElement && !lifecycle.has(eventName(event)) ? document.getElementById(item.ownerId) ?? element : element;
        return listen(event, handler, eventTarget, () => item);
      },
      validate() {
        return validate(behaviors.get(item.id));
      },
      submit() {
        const record = behaviors.get(item.id);
        return validate(record) && emit(record, "submit", {}, true);
      },
      reset() {
        return emit(behaviors.get(item.id), "reset", {}, true);
      },
      cleanup(callback) {
        cleanups.push(callback);
      },
      find(name) {
        return document.getElementById(target(currentState, name).id);
      },
      get(name, property) {
        const item = target(currentState, name),
          el = document.getElementById(item.id);
        if (item.kind === "timer" && property === "interval") return item.interval;
        if (["numericupdown", "slider", "progressbar", "circularprogress", "rating"].includes(item.kind) && ["minimum", "maximum", "increment"].includes(property)) return item[property];
        if (item.kind === "spinner" && property === "speed") return item.speed;
        if (item.kind === "skeleton" && ["shape", "lines", "isActive"].includes(property)) return item[property];
        if (["dock", "anchor"].includes(property) && item.kind !== "form" && !item.component) return item[property] ?? (property === "dock" ? "none" : "top,left");
        if (property === "items" && ["combobox", "listbox", "listview", "checkedlistbox", "radiogroup", "checkboxgroup", "segmentedcontrol", "chipgroup", "buttongroup", "breadcrumb", "sidenavigation"].includes(item.kind)) return [...(item.items ?? [])];
        if (property === "checkedIndices" && item.kind === "chipgroup") return [...el.querySelectorAll('[aria-checked="true"]')].map(button => Number(button.dataset.itemIndex));
        if (item.kind === "chip" && property === "checked") return el.querySelector(".chip-toggle").getAttribute("aria-pressed") === "true";
        if (item.kind === "chip" && ["variant", "removable", "isRemoved"].includes(property)) return property === "isRemoved" ? !!el._modernProperties.isRemoved : item[property];
        if (["iconbutton", "floatingactionbutton"].includes(item.kind) && ["iconName", "showText"].includes(property)) return item[property];
        if (property === "commandItems" && ["menustrip", "toolbar", "toolstrip", "contextmenu", "contextmenustrip", "dropdownbutton", "splitbutton"].includes(item.kind)) return JSON.parse(JSON.stringify(item.commandItems ?? []));
        if (property === "orientation" && ["accordion", "sidebar", "appshell", "responsivepanel", "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel", "flowlayoutpanel", "tablelayoutpanel", "tabcontrol", "splitcontainer", "scrollablepanel", "sidenavigation", "radiogroup", "segmentedcontrol", "buttongroup"].includes(item.kind)) return item.orientation;
        if (property === "gap" && ["accordion", "sidebar", "appshell", "responsivepanel", "stackpanel", "hstack", "vstack", "wrappanel", "centerpanel", "flowlayoutpanel", "tablelayoutpanel", "tabcontrol", "splitcontainer", "scrollablepanel"].includes(item.kind)) return item.gap;
        if (item.kind === "accordion" && property === "expanded") return el.dataset.expanded !== "false";
        if (["appshell", "responsivepanel"].includes(item.kind) && property === "breakpoint") return item.breakpoint;
        if (["accordion", "tabcontrol"].includes(item.kind) && property === "tabs") return [...(item.tabs ?? [])];
        if (item.kind === "scrollablepanel" && property === "scrollDirection") return item.scrollDirection;
        if (item.kind === "splitbutton" && property === "primaryEnabled") return item.primaryEnabled;
        if (item.kind === "commandbutton" && ["description", "iconName", "showText"].includes(property)) return item[property];
        if (property === "checkedIndices" && ["checkboxgroup", "checkedlistbox"].includes(item.kind)) return [...el.querySelectorAll("input:checked")].map(input => Number(input.dataset.itemIndex));
        if (property === "selectedIndex" && item.kind === "radiogroup") return Number(el.querySelector("input:checked")?.dataset.itemIndex ?? -1);
        if (item.kind === "rating" && property === "value") return Number(el.dataset.rating ?? 0);
        if (item.kind === "rating" && property === "readOnly") return item.readOnly;
        if (item.kind === "toast" && ["variant", "position", "duration", "dismissible", "isOpen"].includes(property)) return item[property];
        if (item.kind === "datagridview") {
          if (property === "columns") return [...(item.columns ?? [])];
          if (property === "rows")
            return (item.rows ?? []).map((row) => [...row]);
          if (
            ["readOnly", "sortingEnabled", "filteringEnabled"].includes(
              property,
            )
          )
            return item[property];
        }
        if (
          property === "source" &&
          ["image", "picturebox", "avatar"].includes(item.kind)
        )
          return item.source ?? "";
        if (
          property === "selectedPath" &&
          ["filepicker", "folderpicker"].includes(item.kind)
        )
          return el.querySelector("input").value;
        if (property === "text")
          return (
            item.text ??
            ("value" in el && el.tagName !== "BUTTON"
              ? el.value
              : el.textContent)
          );
        if (
          property === "isActive" &&
          ["spinner", "loadingoverlay"].includes(item.kind)
        )
          return item.isActive;
        if (property === "enabled" || property === "visible")
          return item[property];
        if (property === "selectedTab")
          return Number(el.dataset.activeTab ?? 0);
        if (property === "checked")
          return item.kind === "togglebutton"
            ? el.getAttribute("aria-pressed") === "true"
            : (
                el.querySelector(
                  'input[type="checkbox"],input[type="radio"]',
                ) ?? el
              ).checked;
        if (property === "value" && ["progressbar", "circularprogress"].includes(item.kind)) return item.number;
        if (property === "value")
          return ["numericupdown", "slider"].includes(item.kind)
            ? el.valueAsNumber
            : el.value;
        if (property === "selectedIndex") return el.selectedIndex;
        if (
          ["selectedRow", "filterText", "sortColumn", "sortDirection"].includes(
            property,
          ) &&
          item.kind === "datagridview"
        )
          return item[property];
        if (item.scriptProperties?.includes(property)) return item[property] && typeof item[property] === "object" ? JSON.parse(JSON.stringify(item[property])) : item[property];
        throw new Error(`Unsupported property '${property}'.`);
      },
      set(name, property, value, origin = "programmatic") {
        const destination = target(currentState, name);
        if (property === "commandItems" && !Array.isArray(value)) throw new Error("Commands must be an array.");
        if (destination.kind === "datagridview") {
          if (property === "columns") value = cells(value);
          if (property === "rows") {
            if (!Array.isArray(value))
              throw new Error("Grid rows must be an array of string arrays.");
            value = value.map(cells);
          }
        }
        window.forma.send({
          type: "custom",
          id: destination.id,
          event: "set",
          payload: { sourceId: item.ownerId ?? item.id, property, value, origin },
        });
      },
      showToast(name, options = {}) {
        const destination = target(currentState, name);
        if (destination.kind !== "toast") throw new Error("The target must be a Toast.");
        if (!options || Array.isArray(options) || typeof options !== "object") throw new Error("Toast options must be an object.");
        for (const [property, value] of Object.entries(options)) {
          const valid = property === "text" ? typeof value === "string" && value.length <= 32767
            : property === "variant" ? ["neutral", "info", "success", "warning", "caution", "error", "danger"].includes(value)
            : property === "position" ? ["top-right", "top-left", "bottom-right", "bottom-left"].includes(value)
            : property === "duration" ? Number.isInteger(value)
            : property === "dismissible" ? typeof value === "boolean" : false;
          if (!valid) throw new Error(`Invalid toast option '${property}'.`);
        }
        window.forma.send({
          type: "custom",
          id: destination.id,
          event: "show-toast",
          payload: { sourceId: item.ownerId ?? item.id, options },
        });
      },
      closeToast(name) {
        window.forma.send({
          type: "custom",
          id: target(currentState, name).id,
          event: "close-toast",
          payload: { sourceId: item.ownerId ?? item.id },
        });
      },
      showDialog(name) {
        window.forma.send({
          type: "custom",
          id: target(currentState, name).id,
          event: "show-dialog",
          payload: { sourceId: item.ownerId ?? item.id },
        });
      },
      /**
       * Opens another form of the project in its own Preview window. With { modal: true } it
       * blocks this window like ShowDialog. Resolves with the value passed to closeForm.
       */
      showForm(name, options = {}) {
        if (typeof name !== "string" || !name.trim()) throw new Error("showForm needs a form name.");
        const requestId = `form-${eventSession}-${++formRequests}`;
        return new Promise((resolve, reject) => {
          pendingForms.set(requestId, { resolve, reject });
          window.forma.send({
            type: "custom",
            id: item.ownerId ?? item.id,
            event: "show-form",
            payload: { sourceId: item.ownerId ?? item.id, form: name, modal: options?.modal === true, requestId },
          });
        });
      },
      /** Closes this form's window; the opener's showForm promise receives the result. */
      closeForm(result = null) {
        window.forma.send({
          type: "custom",
          id: item.ownerId ?? item.id,
          event: "close-form",
          payload: { sourceId: item.ownerId ?? item.id, result: result === undefined ? null : JSON.parse(JSON.stringify(result)) },
        });
      },
    };
    const component = {
      id: item.ownerId ?? item.id,
      name: item.name,
      element: item.globalElement ? document.getElementById(item.ownerId) ?? element : element,
      properties: values,
      characteristics: values,
    };
    const sourceOwner = item.ownerId ? controlsById.get(item.ownerId) : undefined;
    const record = {
      source,
      characteristics,
      events,
      element,
      item,
      cleanups,
      snapshot: snapshot(item),
      api,
      sourceIdentity: sourceOwner && { id: sourceOwner.id, name: sourceOwner.name, kind: sourceOwner.kind, parentId: sourceOwner.parentId },
      validationElement: item.globalElement ? document.getElementById(item.ownerId) : undefined,
    };
    behaviors.set(item.id, record);
    const reactiveScope = window.formaReactivity.createScope({
      error: (error) => report(item.id, error),
      resolve: (name) => target(currentState, name).id,
      read: (id, property) =>
        currentState.controls.some((control) => control.id === id)
          ? api.get(id, property)
          : undefined,
      write: (id, property, value) => {
        const destination = target(currentState, id);
        if (property === "selectedPath")
          throw new Error(
            "selectedPath is read-only. Use the picker to select a path.",
          );
        const textInput = [
          "textbox",
          "searchbox",
          "passwordbox",
          "textarea",
          "maskedtextbox",
        ].includes(destination.kind);
        api.set(
          id,
          property === "value" && textInput ? "text" : property,
          value,
          "binding",
        );
      },
    });
    for (const name of [
      "ref",
      "reactive",
      "computed",
      "effect",
      "watch",
      "bind",
    ])
      api[name] = reactiveScope[name];
    if (item.globalElement) sharedState = reactiveScope.reactive({});
    cleanups.push(() => reactiveScope.dispose());
    if (window.ResizeObserver && !item.globalElement) {
      let lastSize;
      const observer = new ResizeObserver((entries) => {
        const rect = entries[0]?.contentRect;
        if (!rect || behaviors.get(item.id) !== record) return;
        const size = { width: rect.width, height: rect.height };
        if (
          lastSize &&
          (lastSize.width !== size.width || lastSize.height !== size.height)
        ) {
          emit(record, "resize", { previous: lastSize, current: size });
          emit(record, "layout");
        }
        lastSize = size;
      });
      observer.observe(element);
      cleanups.push(() => observer.disconnect());
    }
    const onBlur = () => validate(record);
    element.addEventListener("blur", onBlur, true);
    cleanups.push(() => element.removeEventListener("blur", onBlur, true));
    // The script-local forma API is separate from the internal window.forma bridge.
    // api remains an alias for existing saved projects.
    try {
      if (moduleSession) moduleSession.execute(source, api, component, item.globalElement ? "main.js" : "script.js", item.globalElement || !events ? [] : [{ path: "event.js", content: events }]);
      else new Function("forma", "component", "api", '"use strict";\n' + source)(api, component, api);
    } catch (error) {
      cleanupBehavior(item.id);
      throw error;
    }
    return record;
  }

  function cleanupBehavior(id) {
    const record = behaviors.get(id);
    if (record?.destroying) return;
    if (record) record.destroying = true;
    if (record) emit(record, "destroyed");
    behaviors.delete(id);
    for (const cleanup of record?.cleanups ?? []) {
      try {
        cleanup();
      } catch (error) {
        report(id, error);
      }
    }
  }

  /** The host reports a window opened by showForm has closed (or could not open). */
  function formClosed(requestId, result, error) {
    const pending = pendingForms.get(requestId);
    if (!pending) return;
    pendingForms.delete(requestId);
    if (error) pending.reject(new Error(error));
    else pending.resolve(result ?? null);
  }
  window.formaCustomization = {
    formClosed,
    apply(state, runtime = false) {
      currentState = state;
      controlsById = new Map(state.controls.map(control => [control.id, control]));
      for (const record of behaviors.values()) {
        const owner = record.item.ownerId && controlsById.get(record.item.ownerId);
        if (owner) record.sourceIdentity = { id: owner.id, name: owner.name, kind: owner.kind, parentId: owner.parentId };
      }
      runtimeActive = runtime;
      if (runtime && !state.controls.length) { this.clear(); window.formaReactivity.refresh(); return; }
      const pending = [];
      const live = new Set(state.controls.map((item) => item.id));
      if (runtime) {
        const root = { id: state.id ?? state.controls.find(item => item.kind === "form")?.id ?? document.querySelector('[data-forma-type="form"]')?.id ?? state.controls[0].id };
        const source = state.globalScript ?? "";
        const signature = JSON.stringify([root?.id, source, state.projectFiles ?? []]);
        if (signature !== globalSignature) {
          for (const id of [...behaviors.keys()].filter(id => id !== globalKey)) cleanupBehavior(id);
          if (globalKey) cleanupBehavior(globalKey);
          runtimeEpoch++; propertyCache.clear();
          modules.clear(); sharedState = undefined; globalFailed = false; globalSignature = signature;
          globalKey = "__global__" + root.id;
          moduleSession = window.formaModuleRuntime?.createModuleSession(state.projectFiles ?? []);
          try {
            const record = installBehavior({ id: globalKey, ownerId: root.id, name: "main.js", kind: "form", enabled: true, visible: true, globalElement: document.createElement("div") }, state, source, "{}");
            pending.push(() => { for (const event of ["created", "mounted", "ready", "load"]) emit(record, event); });
          } catch (error) { globalFailed = true; modules.clear(); sharedState = undefined; report(root.id, error); }
        }
        live.add(globalKey);
      } else {
        moduleSession = undefined; modules.clear(); sharedState = undefined; globalSignature = undefined; globalFailed = false;
        runtimeEpoch++; propertyCache.clear();
      }
      for (const map of [failedStyles, failedBehaviors])
        for (const id of map.keys()) if (!live.has(id)) map.delete(id);
      for (const [id, record] of styles)
        if (!live.has(id)) {
          record.element.remove();
          styles.delete(id);
        }
      for (const id of behaviors.keys())
        if (!runtime || !live.has(id)) cleanupBehavior(id);
      for (const item of state.controls) {
        const customization = item.customization;
        const css = field(customization, "css") ?? "";
        if (failedStyles.get(item.id) !== css) failedStyles.delete(item.id);
        if (!css && styles.has(item.id)) {
          styles.get(item.id).element.remove();
          styles.delete(item.id);
        }
        if (
          css &&
          styles.get(item.id)?.source !== css &&
          failedStyles.get(item.id) !== css
        ) {
          try {
            const nonvisual =
              item.component ||
              [
                "toast",
                "loadingoverlay",
                "tooltip",
                "dialog",
                "confirmationdialog",
                "contextmenu",
                "contextmenustrip",
                "timer",
                "backgroundworker",
              ].includes(item.kind);
            const compiled = scopedCss(item.id, css, nonvisual, item);
            const element =
              styles.get(item.id)?.element ?? document.createElement("style");
            element.textContent = compiled;
            document.head.appendChild(element);
            styles.set(item.id, { source: css, element });
            failedStyles.delete(item.id);
          } catch (error) {
            failedStyles.set(item.id, css);
            report(item.id, error);
          }
        }
        if (!runtime || globalFailed) continue;
        const source = field(customization, "behavior") ?? "",
          events = field(customization, "events") ?? "",
          characteristics = field(customization, "characteristics") ?? "{}";
        const existing = behaviors.get(item.id);
        if (
          existing &&
          existing.source === source &&
          existing.events === events &&
          existing.characteristics === characteristics &&
          existing.element === document.getElementById(item.id)
        ) {
          const previous = JSON.parse(existing.snapshot);
          const changed = existing.snapshot !== snapshot(item);
          Object.assign(existing.item, item);
          existing.snapshot = snapshot(item);
          if (changed)
            pending.push(() => {
              emit(existing, "updated", { previous, current: { ...item } });
              const moved = previous.x !== item.x || previous.y !== item.y;
              const resized =
                previous.width !== item.width ||
                previous.height !== item.height;
              if (moved)
                emit(existing, "move", { previous, current: { ...item } });
              if (resized && !window.ResizeObserver)
                emit(existing, "resize", { previous, current: { ...item } });
              if (
                moved ||
                (resized && !window.ResizeObserver) ||
                previous.parentId !== item.parentId
              )
                emit(existing, "layout");
            });
          continue;
        }
        cleanupBehavior(item.id);
        const signature = JSON.stringify([source, characteristics, events]);
        if (failedBehaviors.get(item.id) !== signature)
          failedBehaviors.delete(item.id);
        if ((source || item.kind === "form" || item.id === state.id) && failedBehaviors.get(item.id) !== signature)
          try {
            const record = installBehavior(
              item,
              state,
              source,
              characteristics,
              events,
            );
            pending.push(() => {
              for (const event of ["created", "mounted", "ready", "load"])
                emit(record, event);
            });
            failedBehaviors.delete(item.id);
          } catch (error) {
            failedBehaviors.set(item.id, signature);
            report(item.id, error);
          }
      }
      window.formaReactivity.refresh();
      if (runtime && !globalFailed) refreshProperties(state, pending);
      // All components and their handlers exist before startup callbacks run.
      for (const dispatch of pending) dispatch();
    },
    event(id, event, detail = {}) {
      const native = new CustomEvent(eventName(event), { detail, bubbles: true });
      metadata(native, "native"); dispatchComponent(id, native);
    },
    clear() {
      for (const id of [...behaviors.keys()].filter(id => id !== globalKey)) cleanupBehavior(id);
      if (globalKey) cleanupBehavior(globalKey);
      moduleSession = undefined; modules.clear(); sharedState = undefined; globalSignature = undefined; globalKey = undefined; globalFailed = false;
      for (const record of styles.values()) record.element.remove();
      styles.clear();
      failedStyles.clear();
      runtimeActive = false; runtimeEpoch++; propertyCache.clear(); controlsById.clear();
      failedBehaviors.clear();
    },
  };
  for (const name of ["input", "change", "row-selection"]) document.addEventListener(name, captureInput, true);
  window.addEventListener("pagehide", () => window.formaCustomization.clear());
})();
