// Shared by the designer and runtime preview. Source is stored in the .forma file.
(() => {
  const styles = new Map(), behaviors = new Map(), failedStyles = new Map(), failedBehaviors = new Map();
  let currentState;
  const report = (id, error) => window.forma.send({ type: "custom", id, event: "error", payload: { message: String(error.message ?? error) } });
  const field = (object, name) => object?.[name] ?? object?.[name[0].toUpperCase() + name.slice(1)];

  function selectorsOf(source) {
    const selectors = []; let start = 0, depth = 0, quote = "";
    for (let index = 0; index < source.length; index++) {
      const char = source[index];
      if (char === "\\") { index++; continue; }
      if (quote) { if (char === quote) quote = ""; continue; }
      if (char === '"' || char === "'") { quote = char; continue; }
      if (char === "(" || char === "[") depth++;
      if (char === ")" || char === "]") depth--;
      if (char === "," && depth === 0) { selectors.push(source.slice(start, index).trim()); start = index + 1; }
    }
    selectors.push(source.slice(start).trim()); return selectors;
  }

  function scopedCss(id, source) {
    const scratch = document.createElement("style"); scratch.media = "not all";
    scratch.textContent = source; document.head.appendChild(scratch);
    try {
      const compile = rules => Array.from(rules).map(rule => {
        if (rule.type === 1) {
          const selectors = selectorsOf(rule.selectorText);
          if (selectors.some(selector => !/^:host(?![\w-(])/.test(selector)))
            throw new Error("Every selector must start with :host, such as :host:hover or :host input.");
          const selector = selectors.flatMap(selector => [selector.replace(/:host\b/g, `[id="${id}"]`), selector.replace(/:host\b/g, `[data-component-source="${id}"]`)]).join(", ");
          // Overrides must take priority over the inspector's inline appearance.
          const declarations = Array.from({ length: rule.style.length }, (_, index) => {
            const name = rule.style[index];
            return `${name}: ${rule.style.getPropertyValue(name)} !important;`;
          }).join("\n");
          return `${selector} { ${declarations} }`;
        }
        if (rule.type === 4) return `@media ${rule.conditionText} { ${compile(rule.cssRules)} }`;
        if (rule.type === 12) return `@supports ${rule.conditionText} { ${compile(rule.cssRules)} }`;
        throw new Error("Component styles support :host rules, @media and @supports.");
      }).join("\n");
      return compile(scratch.sheet.cssRules);
    } finally { scratch.remove(); }
  }

  function target(state, name) {
    const matches = state.controls.filter(item => item.id === name || item.name === name);
    if (matches.length !== 1) throw new Error(`Control '${name}' must match one Name or ID.`);
    return matches[0];
  }

  function installBehavior(item, state, source, characteristics) {
    const element = document.getElementById(item.id), cleanups = [];
    const values = JSON.parse(characteristics || "{}");
    const api = {
      on(event, handler) {
        const listener = e => {
          if (!item.enabled || !item.visible) return;
          try { Promise.resolve(handler(e)).catch(error => report(item.id, error)); }
          catch (error) { report(item.id, error); }
        };
        element.addEventListener(event, listener);
        cleanups.push(() => element.removeEventListener(event, listener));
      },
      cleanup(callback) { cleanups.push(callback); },
      find(name) { return document.getElementById(target(currentState, name).id); },
      get(name, property) {
        const item = target(currentState, name), el = document.getElementById(item.id);
        if (property === "source" && ["image", "picturebox", "avatar"].includes(item.kind)) return item.source ?? "";
        if (property === "selectedPath" && ["filepicker", "folderpicker"].includes(item.kind)) return el.querySelector("input").value;
        if (property === "text") return item.text ?? ("value" in el && el.tagName !== "BUTTON" ? el.value : el.textContent);
        if (property === "isActive" && ["spinner", "loadingoverlay"].includes(item.kind)) return item.isActive;
        if (property === "enabled" || property === "visible") return item[property];
        if (property === "selectedTab") return Number(el.dataset.activeTab ?? 0);
        if (property === "checked") return item.kind === "togglebutton" ? el.getAttribute("aria-pressed") === "true" : (el.querySelector('input[type="checkbox"],input[type="radio"]') ?? el).checked;
        if (property === "value") return ["numericupdown", "slider"].includes(item.kind) ? el.valueAsNumber : el.value;
        if (property === "selectedIndex") return el.selectedIndex;
        if (["selectedRow", "filterText", "sortColumn", "sortDirection"].includes(property) && item.kind === "datagridview") return item[property];
        throw new Error(`Unsupported property '${property}'.`);
      },
      set(name, property, value) {
        const destination = target(currentState, name);
        window.forma.send({ type: "custom", id: destination.id, event: "set", payload: { sourceId: item.id, property, value } });
      },
      showToast(name) { window.forma.send({ type: "custom", id: target(currentState, name).id, event: "show-toast", payload: { sourceId: item.id } }); },
      closeToast(name) { window.forma.send({ type: "custom", id: target(currentState, name).id, event: "close-toast", payload: { sourceId: item.id } }); },
      showDialog(name) { window.forma.send({ type: "custom", id: target(currentState, name).id, event: "show-dialog", payload: { sourceId: item.id } }); }
    };
    const component = { id: item.id, name: item.name, element, properties: values, characteristics: values };
    const record = { source, characteristics, element, item, cleanups };
    behaviors.set(item.id, record);
    try { new Function("api", "component", '"use strict";\n' + source)(api, component); }
    catch (error) { cleanupBehavior(item.id); throw error; }
  }

  function cleanupBehavior(id) {
    const record = behaviors.get(id); behaviors.delete(id);
    for (const cleanup of record?.cleanups ?? []) {
      try { cleanup(); } catch (error) { report(id, error); }
    }
  }

  window.formaCustomization = {
    apply(state, runtime = false) {
      currentState = state;
      const live = new Set(state.controls.map(item => item.id));
      for (const map of [failedStyles, failedBehaviors]) for (const id of map.keys()) if (!live.has(id)) map.delete(id);
      for (const [id, record] of styles) if (!live.has(id)) { record.element.remove(); styles.delete(id); }
      for (const id of behaviors.keys()) if (!runtime || !live.has(id)) cleanupBehavior(id);
      for (const item of state.controls) {
        const customization = item.customization;
        const css = field(customization, "css") ?? "";
        if (failedStyles.get(item.id) !== css) failedStyles.delete(item.id);
        if (!css && styles.has(item.id)) { styles.get(item.id).element.remove(); styles.delete(item.id); }
        if (css && styles.get(item.id)?.source !== css && failedStyles.get(item.id) !== css) {
          try {
            const compiled = scopedCss(item.id, css);
            const element = styles.get(item.id)?.element ?? document.createElement("style");
            element.textContent = compiled; document.head.appendChild(element);
            styles.set(item.id, { source: css, element });
            failedStyles.delete(item.id);
          } catch (error) { failedStyles.set(item.id, css); report(item.id, error); }
        }
        if (!runtime) continue;
        const source = field(customization, "behavior") ?? "", characteristics = field(customization, "characteristics") ?? "{}";
        const existing = behaviors.get(item.id);
        if (existing && existing.source === source && existing.characteristics === characteristics && existing.element === document.getElementById(item.id)) {
          Object.assign(existing.item, item);
          continue;
        }
        cleanupBehavior(item.id);
        const signature = source + "\n" + characteristics;
        if (failedBehaviors.get(item.id) !== signature) failedBehaviors.delete(item.id);
        if (source && failedBehaviors.get(item.id) !== signature) try {
          installBehavior(item, state, source, characteristics); failedBehaviors.delete(item.id);
        } catch (error) { failedBehaviors.set(item.id, signature); report(item.id, error); }
      }
    },
    event(id, event) { document.getElementById(id)?.dispatchEvent(new CustomEvent(event)); },
    clear() {
      for (const id of behaviors.keys()) cleanupBehavior(id);
      for (const record of styles.values()) record.element.remove();
      styles.clear();
      failedStyles.clear(); failedBehaviors.clear();
    }
  };
  window.addEventListener("pagehide", () => window.formaCustomization.clear());
})();
