(() => {
  const kinds = new Set(["radiogroup", "checkboxgroup", "segmentedcontrol", "rating", "chipgroup", "buttongroup"]);
  const enabled = el => window.formaDesigner?.preview !== false && el.dataset.disabled !== "true" && !el._selectionProperties.readOnly;
  function notify(el, event, payload, change = true) {
    window.forma.send({ type: "event", id: el.id, event, payload });
    if (change) el.dispatchEvent(new Event("change", { bubbles: true }));
  }
  function choose(el, index, change = true) {
    if (!enabled(el)) return;
    if (el.dataset.formaType === "chipgroup") {
      const checked = !(el._selectionProperties.checkedIndices ?? []).includes(index);
      el._selectionProperties.checkedIndices = checked ? [...(el._selectionProperties.checkedIndices ?? []), index].sort((a, b) => a - b) : el._selectionProperties.checkedIndices.filter(value => value !== index);
      update(el, {}); notify(el, "item-check", { index, checked });
    } else if (el.dataset.formaType === "rating") {
      if (el._selectionProperties.number === index + 1) return;
      el._selectionProperties.number = index + 1;
      update(el, {}); notify(el, "value", { value: index + 1 });
    } else {
      if (el._selectionProperties.selectedIndex === index) return;
      el._selectionProperties.selectedIndex = index;
      update(el, {}); notify(el, "selection", { selectedIndex: index }, change);
    }
  }
  function update(el, next) {
    const kind = el.dataset.formaType;
    if (!kinds.has(kind)) return;
    const p = el._selectionProperties = { ...el._selectionProperties, ...next };
    const rating = kind === "rating", multi = ["checkboxgroup", "chipgroup"].includes(kind), radio = kind === "radiogroup";
    const items = rating ? Array.from({ length: Math.max(1, Math.min(10, p.stars ?? 5)) }, (_, i) => `${i + 1} stars`) : p.items ?? [];
    el.dataset.orientation = p.orientation ?? "horizontal";
    el.setAttribute("role", multi ? "group" : "radiogroup");
    if (!multi) el.setAttribute("aria-orientation", el.dataset.orientation);
    el.setAttribute("aria-label", p.text || ({ radiogroup: "Options", checkboxgroup: "Choices", segmentedcontrol: "Segments", rating: "Rating", chipgroup: "Tags", buttongroup: "Button choices" })[kind]);
    const schema = JSON.stringify(items);
    if (el.dataset.selectionSchema !== schema) {
      el.dataset.selectionSchema = schema;
      el.replaceChildren(...items.map((text, index) => {
        if (radio || kind === "checkboxgroup") {
          const label = document.createElement("label"), input = document.createElement("input"), caption = document.createElement("span");
          input.type = multi ? "checkbox" : "radio"; input.name = `${el.id}-choices`; input.dataset.itemIndex = index;
          caption.textContent = text; label.append(input, caption);
          input.addEventListener("change", () => {
            if (!enabled(el)) { update(el, {}); return; }
            if (multi) {
              el._selectionProperties.checkedIndices = [...el.querySelectorAll("input:checked")].map(input => Number(input.dataset.itemIndex));
              notify(el, "item-check", { index, checked: input.checked }, false);
            } else choose(el, index, false);
          });
          return label;
        }
        const button = document.createElement("button"); button.type = "button";
        button.dataset.itemIndex = index;
        button.textContent = rating ? "★" : text; button.setAttribute("role", multi ? "checkbox" : "radio"); button.setAttribute("aria-label", text);
        button.addEventListener("click", () => choose(el, index));
        button.addEventListener("keydown", event => {
          if (!enabled(el)) return;
          let target;
          if (["ArrowRight", "ArrowDown"].includes(event.key)) target = (index + 1) % items.length;
          else if (["ArrowLeft", "ArrowUp"].includes(event.key)) target = (index + items.length - 1) % items.length;
          else if (event.key === "Home") target = 0;
          else if (event.key === "End") target = items.length - 1;
          else if (rating && ["Delete", "Backspace"].includes(event.key)) {
            event.preventDefault(); el._selectionProperties.number = 0; update(el, {}); notify(el, "value", { value: 0 }); return;
          }
          if (target !== undefined) { event.preventDefault(); if (!multi) choose(el, target); el.children[target].focus(); }
        });
        return button;
      }));
    }
    const selected = rating ? (p.number ?? 0) - 1 : (p.selectedIndex ?? -1);
    if (rating) el.dataset.rating = p.number ?? 0;
    else el.selectedIndex = p.selectedIndex ?? -1;
    const disabled = el.dataset.disabled === "true" || !!p.readOnly;
    [...el.children].forEach((child, index) => {
      const input = child.querySelector("input");
      if (input) { input.checked = multi ? (p.checkedIndices ?? []).includes(index) : index === selected; input.disabled = disabled; }
      else {
        const checked = multi ? (p.checkedIndices ?? []).includes(index) : index === selected;
        child.disabled = disabled; child.setAttribute("aria-checked", String(checked));
        child.tabIndex = index === (selected >= 0 ? Math.floor(selected) : 0) ? 0 : -1;
        child.classList.toggle("selected", checked);
        if (rating) child.classList.toggle("filled", index < (p.number ?? 0));
      }
    });
  }
  window.formaSelections = { update, refresh: el => update(el, {}) };
})();
