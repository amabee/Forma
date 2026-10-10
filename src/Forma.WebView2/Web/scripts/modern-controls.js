(() => {
  const toasts = new Map(), overlays = new Map();
  const runtime = () => window.formaDesigner?.preview !== false;
  const allowed = el => runtime() && el._modernAppearance?.enabled !== false && el._modernAppearance?.visible !== false;
  const emit = (el, event, payload) => window.forma.send({ type: "event", id: el.id, event, payload });
  const span = (className, text = "") => { const el = document.createElement("span"); el.className = className; el.textContent = text; return el; };

  function removeToast(id) { const entry = toasts.get(id); if (entry) { clearTimeout(entry.timer); entry.popup.remove(); toasts.delete(id); arrangeToasts(); } }
  function toast(el, p) {
    if (!p.isOpen || !allowed(el)) { removeToast(el.id); return; }
    const signature = JSON.stringify([p.text, p.variant, p.position, p.duration, p.dismissible]);
    if (toasts.get(el.id)?.signature === signature) return;
    removeToast(el.id);
    const popup = document.createElement("div"); popup.className = "forma-toast-popup";
    popup.dataset.componentSource = el.id; popup.dataset.variant = p.variant ?? "info"; popup.dataset.position = p.position ?? "bottom-right";
    popup.setAttribute("role", ["danger", "error", "warning", "caution"].includes(p.variant) ? "alert" : "status"); popup.appendChild(span("toast-message", p.text ?? "Notification"));
    if (p.dismissible !== false) {
      const close = document.createElement("button"); close.type = "button"; close.textContent = "×"; close.setAttribute("aria-label", "Dismiss notification");
      close.addEventListener("click", () => { removeToast(el.id); emit(el, "toast-close", { reason: "dismiss" }); }); popup.appendChild(close);
    }
    document.body.appendChild(popup);
    const entry = { popup, signature, timer: setTimeout(() => { removeToast(el.id); emit(el, "toast-close", { reason: "timeout" }); }, p.duration ?? 4000) };
    toasts.set(el.id, entry); arrangeToasts();
  }
  function arrangeToasts() {
    const offsets = new Map();
    for (const { popup } of toasts.values()) {
      const position = popup.dataset.position, offset = offsets.get(position) ?? 16;
      popup.style[position.startsWith("top") ? "top" : "bottom"] = `${offset}px`;
      offsets.set(position, offset + popup.getBoundingClientRect().height + 12);
    }
  }
  function removeOverlay(id) {
    const entry = overlays.get(id); if (!entry) return;
    entry.observer?.disconnect(); entry.popup.remove(); overlays.delete(id);
    if (![...overlays.values()].some(other => other.target === entry.target)) {
      if (entry.busy == null) entry.target.removeAttribute("aria-busy"); else entry.target.setAttribute("aria-busy", entry.busy);
    }
  }
  function placeOverlay(entry) {
    const rect = entry.target.getBoundingClientRect();
    Object.assign(entry.popup.style, { left: `${rect.left}px`, top: `${rect.top}px`, width: `${rect.width}px`, height: `${rect.height}px` });
  }
  function overlay(el, p) {
    el.hidden = runtime();
    const target = document.getElementById(p.targetId);
    if (!p.isActive || !allowed(el) || !target || target.hidden || target === el) { removeOverlay(el.id); return; }
    let entry = overlays.get(el.id);
    if (entry && entry.target !== target) { removeOverlay(el.id); entry = null; }
    if (!entry) {
      const popup = document.createElement("div"); popup.className = "forma-loading-popup"; popup.dataset.componentSource = el.id;
      popup.setAttribute("role", "status"); popup.setAttribute("aria-live", "polite");
      popup.append(span("modern-spinner"), span("loading-message"));
      const existing = [...overlays.values()].find(other => other.target === target);
      entry = { popup, target, busy: existing ? existing.busy : target.getAttribute("aria-busy") };
      target.setAttribute("aria-busy", "true"); document.body.appendChild(popup); overlays.set(el.id, entry);
      if (window.ResizeObserver) { entry.observer = new ResizeObserver(() => placeOverlay(entry)); entry.observer.observe(target); }
    }
    entry.popup.querySelector(".loading-message").textContent = p.text ?? "Please wait…"; placeOverlay(entry);
  }
  window.formaModern = {
    update(el, update) {
      const p = el._modernProperties = { ...el._modernProperties, ...update }, kind = el.dataset.formaType;
      if (!["toast", "loadingoverlay"].includes(kind)) {
        const signature = JSON.stringify([p, runtime(), el._modernAppearance?.enabled, el._modernAppearance?.visible]);
        if (el._modernSignature === signature) return;
        el._modernSignature = signature;
      }
      if (kind === "card") {
        const header = el.querySelector(":scope > .layout-header"), host = el.querySelector(":scope > .layout-content");
        header.hidden = p.headerVisible === false; host.style.height = p.headerVisible === false ? "100%" : "calc(100% - 56px)";
        header.replaceChildren(span("card-title", p.text ?? "Card"), span("card-description", p.description ?? ""));
      }
      if (["iconbutton", "floatingactionbutton", "commandbutton"].includes(kind)) {
        el.setAttribute("aria-label", p.text || "Action");
        el.dataset.showText = String(!!p.showText);
        const icon = window.formaIcons.create(p.iconName ?? "search"); icon.setAttribute("aria-hidden", "true");
        if (kind === "commandbutton") {
          const caption = span("command-button-content"); caption.append(span("command-button-title", p.text ?? "Command"), span("command-button-description", p.description ?? ""));
          el.replaceChildren(icon, caption);
        } else el.replaceChildren(icon, span("icon-button-label", p.text ?? "Action"));
      }
      if (kind === "chip") {
        el.dataset.variant = p.variant ?? "neutral";
        el.hidden = runtime() && !!p.isRemoved;
        if (!el.querySelector(".chip-toggle")) {
          const toggle = document.createElement("button"), remove = document.createElement("button");
          toggle.type = remove.type = "button"; toggle.className = "chip-toggle"; remove.className = "chip-remove"; remove.textContent = "×";
          toggle.addEventListener("click", () => {
            if (!allowed(el)) return;
            el._modernProperties.checked = !el._modernProperties.checked;
            window.formaModern.update(el, {});
            emit(el, "checked", { checked: el._modernProperties.checked });
            el.dispatchEvent(new Event("change", { bubbles: true }));
          });
          remove.addEventListener("click", event => {
            event.stopPropagation();
            if (!allowed(el) || !el._modernProperties.removable) return;
            el._modernProperties.isRemoved = true; window.formaModern.update(el, {});
            emit(el, "chip-remove", {}); el.dispatchEvent(new CustomEvent("chip-remove", { bubbles: true }));
          });
          el.replaceChildren(toggle, remove);
        }
        const toggle = el.querySelector(".chip-toggle"), remove = el.querySelector(".chip-remove");
        toggle.textContent = p.text ?? "Chip"; toggle.setAttribute("aria-pressed", String(!!p.checked));
        toggle.disabled = remove.disabled = el._modernAppearance?.enabled === false;
        el.dataset.checked = String(!!p.checked);
        remove.hidden = !p.removable; remove.setAttribute("aria-label", `Remove ${p.text ?? "chip"}`);
      }
      if (kind === "icon") {
        el.setAttribute("role", "img"); el.setAttribute("aria-label", p.text || "Icon");
        el.replaceChildren(window.formaIcons.create(p.iconName, p.strokeWidth ?? 2));
      }
      if (kind === "emptystate") {
        el.setAttribute("role", "region"); el.setAttribute("aria-label", p.text || "Empty state");
        const graphic = span("empty-state-icon"); graphic.appendChild(window.formaIcons.create(p.iconName));
        el.replaceChildren(graphic, span("empty-state-title", p.text ?? "Nothing here yet"), span("empty-state-description", p.description ?? ""));
      }
      if (kind === "skeleton") {
        el.setAttribute("role", "status"); el.setAttribute("aria-label", p.text || "Loading content");
        el.setAttribute("aria-busy", String(p.isActive !== false));
        const shape = ["text", "rectangle", "circle"].includes(p.shape) ? p.shape : "text";
        const count = shape === "text" ? Math.max(1, Math.min(10, p.lines ?? 3)) : 1;
        let bars = el.querySelector(".skeleton-bars");
        if (!bars || bars.dataset.shape !== shape || bars.children.length !== count) {
          bars = span("skeleton-bars"); bars.dataset.shape = shape; bars.setAttribute("aria-hidden", "true");
          for (let i = 0; i < count; i++) bars.appendChild(span("skeleton-bar"));
          el.replaceChildren(bars);
        }
        bars.dataset.animated = String(allowed(el) && p.isActive !== false);
      }
      if (kind === "badge") {
        el.dataset.variant = p.variant ?? "info"; el.replaceChildren(span("badge-content", p.text ?? "Badge"));
      }
      if (kind === "avatar") {
        el.dataset.shape = p.shape ?? "circle"; el.setAttribute("role", "img"); el.setAttribute("aria-label", p.text || "Avatar");
        let fallback = el.querySelector(".avatar-initials"), image = el.querySelector("img");
        if (!fallback) {
          fallback = span("avatar-initials"); image = document.createElement("img"); image.alt = "";
          image.addEventListener("error", () => { image.hidden = true; fallback.hidden = false; });
          image.addEventListener("load", () => { image.hidden = false; fallback.hidden = true; });
          el.append(fallback, image);
        }
        fallback.textContent = p.initials || (p.text || "User").trim().split(/\s+/).filter(Boolean).slice(0, 2).map(part => Array.from(part)[0]).join("").toUpperCase();
        image.style.objectFit = p.sizeMode ?? "cover";
        if (p.source && image.getAttribute("src") !== p.source) { image.src = p.source; image.hidden = true; fallback.hidden = false; }
        if (!p.source) { image.removeAttribute("src"); image.hidden = true; fallback.hidden = false; }
      }
      if (kind === "divider") {
        el.dataset.orientation = p.orientation ?? "horizontal"; el.setAttribute("role", "separator"); el.setAttribute("aria-orientation", p.orientation ?? "horizontal");
        const line = () => { const node = span("divider-line"); node.style[p.orientation === "vertical" ? "borderLeft" : "borderTop"] = `${p.thickness ?? 1}px ${p.lineStyle ?? "solid"} currentColor`; return node; };
        el.replaceChildren(...(p.text ? [line(), span("divider-caption", p.text), line()] : [line()]));
      }
      if (kind === "spinner") {
        if (!el.querySelector(".modern-spinner")) el.append(span("modern-spinner"), span("spinner-label"));
        el.querySelector(".spinner-label").textContent = p.text ?? "Loading…";
        const spinner = el.querySelector(".modern-spinner"); spinner.style.animationDuration = `${p.speed ?? 800}ms`;
        spinner.style.animationPlayState = runtime() && p.isActive !== false ? "running" : "paused";
        el.setAttribute("role", "status"); el.setAttribute("aria-busy", String(!!p.isActive));
      }
      if (kind === "toast") toast(el, p);
      if (kind === "loadingoverlay") overlay(el, p);
    },
    refresh(el, item) { el._modernAppearance = item; if (el._modernProperties) this.update(el, {}); },
    removing(el) {
      for (const [id] of toasts) if (id === el?.id || el?.contains(document.getElementById(id))) removeToast(id);
      for (const [id, entry] of overlays) if (id === el?.id || el?.contains(entry.target) || el?.contains(document.getElementById(id))) removeOverlay(id);
    },
    clear() { for (const id of toasts.keys()) removeToast(id); for (const id of overlays.keys()) removeOverlay(id); }
  };
  const reposition = () => { arrangeToasts(); for (const entry of overlays.values()) placeOverlay(entry); };
  window.addEventListener("resize", reposition); document.addEventListener("scroll", reposition, true);
  window.addEventListener("pagehide", () => window.formaModern.clear());
})();
