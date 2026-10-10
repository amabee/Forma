(() => {
  let current, popup, delay, duration, target, originalTitle;
  function close() {
    clearTimeout(delay); clearTimeout(duration);
    if (target && popup) {
      const ids = (target.getAttribute("aria-describedby") ?? "").split(/\s+/).filter(id => id && id !== popup.id);
      if (ids.length) target.setAttribute("aria-describedby", ids.join(" ")); else target.removeAttribute("aria-describedby");
    }
    if (target && originalTitle != null) target.setAttribute("title", originalTitle);
    popup?.remove(); popup = target = current = null; originalTitle = null;
  }
  function candidate(node) {
    let control = node.closest?.("[data-forma-type]");
    while (control) {
      const tooltip = [...document.querySelectorAll('[data-forma-type="tooltip"]')].find(item => item.dataset.targetId === control.id && item.dataset.disabled !== "true" && item.dataset.tooltipText);
      if (tooltip) return { tooltip, control };
      control = control.parentElement?.closest?.("[data-forma-type]");
    }
  }
  function begin(event) {
    if (window.formaDesigner?.preview === false) return;
    const found = candidate(event.target); if (!found || found.control.dataset.disabled === "true") return;
    if (current === found.tooltip && target === found.control) return;
    close(); current = found.tooltip; target = found.control;
    originalTitle = target.getAttribute("title"); target.removeAttribute("title");
    delay = setTimeout(() => {
      if (!current?.isConnected || !target?.isConnected || current.dataset.disabled === "true" || window.formaDesigner?.preview === false) { close(); return; }
      popup = document.createElement("div"); popup.id = `${current.id}-popup`;
      popup.dataset.componentSource = current.id;
      popup.className = "forma-tooltip-popup"; popup.setAttribute("role", "tooltip"); popup.textContent = current.dataset.tooltipText;
      document.body.appendChild(popup);
      target.setAttribute("aria-describedby", [target.getAttribute("aria-describedby"), popup.id].filter(Boolean).join(" "));
      const rect = target.getBoundingClientRect(), tip = popup.getBoundingClientRect(), placement = current.dataset.placement;
      let x = rect.left + (rect.width - tip.width) / 2, y = rect.top - tip.height - 8;
      if (placement === "bottom") y = rect.bottom + 8;
      if (placement === "left" || placement === "right") { x = placement === "left" ? rect.left - tip.width - 8 : rect.right + 8; y = rect.top + (rect.height - tip.height) / 2; }
      popup.style.left = `${Math.max(4, Math.min(x, window.innerWidth - tip.width - 4))}px`;
      popup.style.top = `${Math.max(4, Math.min(y, window.innerHeight - tip.height - 4))}px`;
      duration = setTimeout(close, Number(current.dataset.showDuration ?? 5000));
    }, Number(current.dataset.initialDelay ?? 500));
  }
  window.formaTooltip = {
    close,
    setTitle(element, title) {
      if (element === target) { originalTitle = title; element.removeAttribute("title"); }
      else element.title = title;
    },
    update(element, properties) {
      if (current === element) close();
      for (const name of ["targetId", "initialDelay", "showDuration", "placement"]) if (name in properties) element.dataset[name] = properties[name];
      if ("text" in properties) element.dataset.tooltipText = properties.text ?? "";
    },
    removing(element) { if (element === current || element === target || element?.contains(target)) close(); }
  };
  document.addEventListener("mouseover", begin); document.addEventListener("focusin", begin);
  const leave = event => { if (target?.contains(event.target) && !target.contains(event.relatedTarget)) close(); };
  document.addEventListener("mouseout", leave); document.addEventListener("focusout", leave);
  document.addEventListener("keydown", event => { if (event.key === "Escape") close(); });
  window.addEventListener("resize", close); window.addEventListener("blur", close);
  document.addEventListener("scroll", close, true);
})();
