// Dock/Anchor are view geometry. The Builder model owns saved bounds and layout settings.
(() => {
  const managed = new Set(['flowlayoutpanel', 'stackpanel', 'hstack', 'vstack', 'wrappanel', 'centerpanel', 'sidebar', 'appshell', 'responsivepanel', 'tablelayoutpanel']);
  const groups = new Map();
  const number = value => Number.isFinite(Number(value)) ? Number(value) : 0;
  const limit = (value, minimum, maximum) => Math.max(minimum, Math.min(Math.max(minimum, maximum), value));
  let observer;
  function size(host, parent) {
    const style = window.getComputedStyle(host);
    const px = number(parseFloat(style.paddingLeft)), py = number(parseFloat(style.paddingTop));
    const paddingX = px + number(parseFloat(style.paddingRight)), paddingY = py + number(parseFloat(style.paddingBottom));
    const fallback = reference(parent);
    return { width: Math.max(0, host.clientWidth ? host.clientWidth - paddingX : fallback.width),
      height: Math.max(0, host.clientHeight ? host.clientHeight - paddingY : fallback.height), x: px, y: py };
  }
  function reference(parent) {
    if (parent.kind === 'form') return { width: number(parent.width), height: number(parent.height) };
    let width = number(parent.width) - number(parent.paddingLeft) - number(parent.paddingRight) - 2 * number(parent.borderWidth);
    let height = number(parent.height) - number(parent.paddingTop) - number(parent.paddingBottom) - 2 * number(parent.borderWidth);
    if (parent.kind === 'groupbox') height -= 28;
    if (parent.kind === 'card' && parent.headerVisible) height -= 56;
    if (parent.kind === 'tabcontrol') { if (parent.orientation === 'vertical') width -= 120; else height -= 36; }
    if (parent.kind === 'accordion') height -= ((parent.tabs?.length ?? 2) + 1) * 36;
    if (parent.kind === 'splitcontainer') {
      if (parent.orientation === 'vertical') height = (height - number(parent.gap)) / 2;
      else width = (width - number(parent.gap)) / 2;
    }
    return { width: Math.max(24, width), height: Math.max(20, height) };
  }
  function apply(group) {
    const { host, parent, children } = group;
    if (!host.isConnected) return;
    const area = size(host, parent), base = reference(parent);
    let left = 0, top = 0, right = area.width, bottom = area.height;
    // Edge docks consume space in child order; Fill receives the final remainder.
    const ordered = [...children].sort((a, b) => Number(a.item.dock === 'fill') - Number(b.item.dock === 'fill'));
    for (const { item, element } of ordered) {
      if (item.component || item.visible === false || element.hidden) continue;
      let x = number(item.x), y = number(item.y), width = number(item.width), height = number(item.height);
      const ml = number(item.marginLeft), mr = number(item.marginRight), mt = number(item.marginTop), mb = number(item.marginBottom);
      const dock = item.dock ?? 'none';
      if (dock !== 'none') {
        x = left + area.x; y = top + area.y;
        if (['top', 'bottom', 'fill'].includes(dock)) width = right - left - ml - mr;
        if (['left', 'right', 'fill'].includes(dock)) height = bottom - top - mt - mb;
        const takeW = Math.min(right - left, width + ml + mr), takeH = Math.min(bottom - top, height + mt + mb);
        if (dock === 'top') top += takeH;
        if (dock === 'bottom') { y = bottom - takeH + area.y; bottom -= takeH; }
        if (dock === 'left') left += takeW;
        if (dock === 'right') { x = right - takeW + area.x; right -= takeW; }
      } else {
        const anchor = new Set((item.anchor ?? 'top,left').split(','));
        const dx = area.width - base.width, dy = area.height - base.height;
        if (anchor.has('left') && anchor.has('right')) width += dx;
        else if (!anchor.has('left')) x += anchor.has('right') ? dx : dx / 2;
        if (anchor.has('top') && anchor.has('bottom')) height += dy;
        else if (!anchor.has('top')) y += anchor.has('bottom') ? dy : dy / 2;
      }
      width = limit(width, item.minimumWidth || 24, item.maximumWidth || Math.max(24, area.width - ml - mr));
      height = limit(height, item.minimumHeight || 20, item.maximumHeight || Math.max(20, area.height - mt - mb));
      x = dock === 'none' ? limit(x, 0, area.width - width - ml - mr) : Math.max(0, x);
      y = dock === 'none' ? limit(y, 0, area.height - height - mt - mb) : Math.max(0, y);
      const values = { position: 'absolute', left: `${Math.round(x)}px`, top: `${Math.round(y)}px`, width: `${Math.round(width)}px`, height: `${Math.round(height)}px` };
      for (const [key, value] of Object.entries(values)) if (element.style[key] !== value) element.style[key] = value;
    }
  }
  window.formaLayout = {
    refresh(state) {
      const next = new Map(), controls = new Map((state?.controls ?? []).map(item => [item.id, item]));
      for (const item of controls.values()) {
        const parent = controls.get(item.parentId), element = document.getElementById(item.id), host = element?.parentElement;
        if (!parent || !host || managed.has(parent.kind) || item.component) continue;
        // Default top/left children need no browser observer; they retain normal designer behavior.
        let group = next.get(host);
        if (!group) next.set(host, group = { host, parent, children: [] });
        group.children.push({ item, element });
      }
      for (const [host, group] of next) {
        if (!group.children.some(({ item }) => item.dock && item.dock !== 'none' || item.anchor && item.anchor !== 'top,left')) next.delete(host);
      }
      for (const host of groups.keys()) if (!next.has(host)) { observer?.unobserve(host); groups.delete(host); }
      if (!observer && typeof ResizeObserver !== 'undefined') observer = new ResizeObserver(entries => {
        for (const entry of entries) { const group = groups.get(entry.target); if (group) apply(group); }
        window.formaDesigner?.outline();
      });
      for (const [host, group] of next) {
        if (!groups.has(host)) observer?.observe(host);
        groups.set(host, group); apply(group);
      }
    },
    removing(element) {
      for (const host of groups.keys()) if (host === element || element?.contains(host)) { observer?.unobserve(host); groups.delete(host); }
    },
    clear() { observer?.disconnect(); observer = null; groups.clear(); }
  };
})();
