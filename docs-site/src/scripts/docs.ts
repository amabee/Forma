export {};
const themeButton = document.getElementById('theme-toggle')!;
function labelTheme() {
  const dark = document.documentElement.dataset.theme === 'dark';
  themeButton.setAttribute('aria-label', `Switch to ${dark ? 'light' : 'dark'} mode`);
  themeButton.setAttribute('title', `Switch to ${dark ? 'light' : 'dark'} mode`);
}
labelTheme();
themeButton.addEventListener('click', () => {
  const theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
  document.documentElement.dataset.theme = theme;
  try { localStorage.setItem('forma-docs-theme', theme); } catch {}
  labelTheme();
});

const menuButton = document.getElementById('menu-toggle')!;
const backdrop = document.getElementById('nav-backdrop')!;
function closeMenu() {
  document.body.classList.remove('nav-open');
  menuButton.setAttribute('aria-expanded', 'false'); backdrop.hidden = true;
  if (document.getElementById('sidebar')!.contains(document.activeElement)) menuButton.focus();
}
menuButton.addEventListener('click', () => {
  const open = !document.body.classList.contains('nav-open');
  document.body.classList.toggle('nav-open', open);
  menuButton.setAttribute('aria-expanded', String(open)); backdrop.hidden = !open;
});
backdrop.addEventListener('click', closeMenu);
document.addEventListener('keydown', event => { if (event.key === 'Escape') closeMenu(); });

document.querySelectorAll<HTMLPreElement>('.prose pre').forEach(pre => {
  const copy = document.createElement('button');
  copy.className = 'copy-code'; copy.type = 'button'; copy.textContent = 'Copy'; copy.setAttribute('aria-label', 'Copy code example');
  copy.addEventListener('click', async () => {
    try { await navigator.clipboard.writeText(pre.querySelector('code')!.textContent ?? ''); copy.textContent = 'Copied!'; }
    catch { copy.textContent = 'Select to copy'; }
    setTimeout(() => { copy.textContent = 'Copy'; }, 1800);
  });
  pre.append(copy);
});
const headings = document.querySelectorAll('.prose h2[id]');
if ('IntersectionObserver' in window) {
  const observer = new IntersectionObserver(entries => {
    const entry = entries.find(entry => entry.isIntersecting);
    if (!entry) return;
    document.querySelectorAll('.toc a').forEach(link => link.classList.toggle('active', link.getAttribute('href') === '#' + entry.target.id));
  }, { rootMargin: '-100px 0px -65% 0px' });
  headings.forEach(heading => observer.observe(heading));
}

interface SearchPage { title: string; description: string; group: string; url: string; text: string; }
const dialog = document.getElementById('search-dialog') as HTMLDialogElement;
const input = document.getElementById('search-input') as HTMLInputElement;
const results = document.getElementById('search-results')!;
const searchStatus = document.getElementById('search-status')!;
let index: SearchPage[] | undefined, loading: Promise<void> | undefined;
let focusIndex = -1;
function render() {
  if (!index) return;
  results.replaceChildren(); focusIndex = -1;
  const query = input.value.trim().toLowerCase();
  const words = query.split(/\s+/).filter(Boolean);
  if (!words.length) { searchStatus.textContent = 'Search components, properties, and guides.'; return; }
  const matches = index.map(page => {
    const haystack = `${page.title} ${page.description} ${page.text}`.toLowerCase();
    if (!words.every(word => haystack.includes(word))) return { page, score: 0 };
    const score = words.reduce((total, word) => total + (page.title.toLowerCase().includes(word) ? 15 : page.description.toLowerCase().includes(word) ? 5 : 1), 0);
    return { page, score };
  }).filter(item => item.score).sort((a, b) => b.score - a.score || a.page.title.localeCompare(b.page.title));
  searchStatus.textContent = matches.length ? `${matches.length} results. Showing the best ${Math.min(matches.length, 20)}.` : 'Nothing yet. Try a component name or a property key.';
  for (const { page } of matches.slice(0, 20)) {
    const anchor = document.createElement('a'); anchor.className = 'search-result'; anchor.href = page.url;
    const group = document.createElement('span'); group.className = 'card-eyebrow'; group.textContent = page.group;
    const title = document.createElement('strong'); title.textContent = page.title;
    const snippet = document.createElement('p');
    const position = page.text.toLowerCase().indexOf(words[0]);
    snippet.textContent = position >= 0 ? (position > 40 ? '…' : '') + page.text.slice(Math.max(0, position - 40), position + 150) + '…' : page.description;
    anchor.append(group, title, snippet); results.append(anchor);
  }
}
function openSearch() {
  closeMenu(); if (!dialog.open) dialog.showModal(); input.focus();
  if (!index && !loading) {
    searchStatus.textContent = 'Loading the field guide…';
    loading = fetch(dialog.dataset.indexUrl!).then(async response => {
      if (!response.ok) throw new Error('Search unavailable');
      index = await response.json(); render();
    }).catch(() => { searchStatus.textContent = 'Search couldn’t load. Try opening it again, or use the navigation.'; }).finally(() => { loading = undefined; });
  } else render();
}
document.querySelectorAll('[data-search-open]').forEach(button => button.addEventListener('click', openSearch));
document.getElementById('search-close')!.addEventListener('click', () => dialog.close());
dialog.addEventListener('click', event => { if (event.target === dialog) { const rect = dialog.getBoundingClientRect(); if (event.clientX < rect.left || event.clientX > rect.right || event.clientY < rect.top || event.clientY > rect.bottom) dialog.close(); } });
input.addEventListener('input', render);
input.addEventListener('keydown', event => {
  const anchors = [...results.querySelectorAll<HTMLAnchorElement>('a')];
  if (['ArrowDown', 'ArrowUp'].includes(event.key) && anchors.length) {
    event.preventDefault(); focusIndex = (focusIndex + (event.key === 'ArrowDown' ? 1 : -1) + anchors.length) % anchors.length;
    anchors.forEach((anchor, index) => anchor.classList.toggle('selected', index === focusIndex));
    anchors[focusIndex].scrollIntoView({ block: 'nearest' });
  }
  if (event.key === 'Enter' && anchors.length) { event.preventDefault(); anchors[Math.max(0, focusIndex)].click(); }
});
document.addEventListener('keydown', event => {
  if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); openSearch(); }
});
