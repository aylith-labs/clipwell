import { createMemo, createRoot, createSignal } from "solid-js";
import * as api from "./lib/client";
import { dateBucket, sourceBucket } from "./lib/format";
import { paste as platformPaste, setHotkey as platformSetHotkey } from "./lib/platform";
import type { ClipboardSettings, ClipItem, Counts, GroupMode, Tab, ViewMode } from "./types";

const PAGE = 100;

export interface Row {
  item: ClipItem;
  header?: string;
}

export type ConnState = "connected" | "down" | "retrying";

const DEFAULT_SETTINGS: ClipboardSettings = {
  retentionDays: 30,
  openAtCursor: false,
  theme: "system",
  defaultView: "compact",
  defaultGroup: "none",
  showSource: true,
  showTime: true,
  showChars: true,
  hotkey: "Alt+Shift+V",
};

/**
 * Windows captures store the raw CF_HTML payload, whose descriptor header
 * ("Version:… StartHTML:…") must not reach a text/html clipboard entry.
 */
function htmlPayload(raw: string): string | null {
  if (!/^Version:\d/.test(raw)) return raw;
  const start = raw.indexOf("<html");
  return start >= 0 ? raw.slice(start) : null;
}

function applyTheme(theme: ClipboardSettings["theme"]) {
  const dark =
    theme === "dark" ||
    (theme === "system" && window.matchMedia("(prefers-color-scheme: dark)").matches);
  document.documentElement.classList.toggle("dark", dark);
}

function makeStore() {
  const [items, setItems] = createSignal<ClipItem[]>([]);
  const [search, setSearchRaw] = createSignal("");
  const [tab, setTab] = createSignal<Tab>("all");
  const [kind, setKind] = createSignal("all");
  const [group, setGroup] = createSignal<GroupMode>("none");
  const [view, setView] = createSignal<ViewMode>("compact");
  const [selectedTs, setSelectedTs] = createSignal<string | null>(null);
  const [settings, setSettings] = createSignal<ClipboardSettings>(DEFAULT_SETTINGS);
  const [loaded, setLoaded] = createSignal(false);
  const [conn, setConn] = createSignal<ConnState>("connected");
  const [counts, setCounts] = createSignal<Counts | null>(null);
  const [quickLook, setQuickLook] = createSignal(false);
  const [actionsOpen, setActionsOpen] = createSignal(false);
  const [renameTs, setRenameTs] = createSignal<string | null>(null);
  const [editTs, setEditTs] = createSignal<string | null>(null);

  let oldest: string | null = null;
  let allLoaded = false;
  let loadingMore = false;

  function passes(i: ClipItem): boolean {
    if (tab() === "pinned" && !i.isUserPinned) return false;
    if (tab() === "sensitive" && !i.isSensitive) return false;
    if (kind() !== "all" && i.kind !== kind()) return false;
    const q = search().trim().toLowerCase();
    if (q) {
      const inText = (i.textContent ?? "").toLowerCase().includes(q);
      const inAlias = (i.alias ?? "").toLowerCase().includes(q);
      if (!inText && !inAlias) return false;
    }
    return true;
  }

  const rows = createMemo<Row[]>(() => {
    const g = group();
    let list = items().filter(passes);
    if (g === "source") {
      list = [...list].sort(
        (a, b) =>
          sourceBucket(a).localeCompare(sourceBucket(b)) || b.timestamp.localeCompare(a.timestamp),
      );
    } else if (g === "none") {
      list = [...list].sort((a, b) => Number(b.isUserPinned) - Number(a.isUserPinned));
    }
    const out: Row[] = [];
    let last: string | null = null;
    for (const item of list) {
      let header: string | undefined;
      if (g !== "none") {
        const bucket = g === "source" ? sourceBucket(item) : dateBucket(item.timestamp);
        if (bucket !== last) {
          header = bucket;
          last = bucket;
        }
      }
      out.push({ item, header });
    }
    return out;
  });

  const selected = createMemo(() => items().find((i) => i.timestamp === selectedTs()) ?? null);

  // Derived so it tracks the filtered view live (search/filter/group), not just reloads.
  const status = createMemo(() =>
    !loaded()
      ? "Connecting to daemon…"
      : items().length === 0
        ? "No clipboard history yet"
        : `${rows().length} of ${items().length} items`,
  );

  // Counts are decoration: fetched alongside every reload, failure-isolated so a
  // daemon without the endpoint just hides them.
  async function refreshCounts() {
    try {
      setCounts(await api.getCounts(search()));
    } catch {
      /* keep the last values */
    }
  }

  // Search keystrokes refilter instantly but only ping the counts endpoint after
  // a 200 ms pause (counts respect the active search, like the pills' semantics).
  let countsDebounce: ReturnType<typeof setTimeout> | undefined;
  function setSearch(value: string) {
    setSearchRaw(value);
    clearTimeout(countsDebounce);
    countsDebounce = setTimeout(() => void refreshCounts(), 200);
  }

  async function reload() {
    try {
      const page = await api.getPage(PAGE);
      setItems(page);
      oldest = page.at(-1)?.timestamp ?? null;
      allLoaded = page.length < PAGE;
      if (!selectedTs() || !page.some((i) => i.timestamp === selectedTs())) {
        setSelectedTs(rows()[0]?.item.timestamp ?? null);
      }
      setLoaded(true);
      setConn("connected");
      void refreshCounts();
    } catch {
      setConn("down");
    }
  }

  /** Manual retry from the error banner (also used by the auto-retry timer). */
  async function retry() {
    setConn("retrying");
    await reload();
  }

  // Fallback poll while disconnected; the WS reconnect (2 s) usually wins.
  setInterval(() => {
    if (conn() === "down") void retry();
  }, 5000);

  async function loadMore() {
    if (loadingMore || allLoaded || !oldest) return;
    loadingMore = true;
    try {
      const page = await api.getPage(PAGE, oldest);
      if (page.length === 0) {
        allLoaded = true;
        return;
      }
      const seen = new Set(items().map((i) => i.timestamp));
      const fresh = page.filter((i) => !seen.has(i.timestamp));
      setItems([...items(), ...fresh]);
      oldest = items().at(-1)?.timestamp ?? oldest;
      if (page.length < PAGE) allLoaded = true;
    } finally {
      loadingMore = false;
    }
  }

  async function init() {
    try {
      const s = await api.getSettings();
      setSettings(s);
      applyTheme(s.theme);
      setView(s.defaultView);
      setGroup(s.defaultGroup);
      void platformSetHotkey(s.hotkey);
    } catch {
      applyTheme("system");
    }
    window
      .matchMedia("(prefers-color-scheme: dark)")
      .addEventListener("change", () => applyTheme(settings().theme));
    await reload();
    // Resync on every WS (re)connect too — after a daemon restart this pulls
    // fresh history and clears the unreachable banner.
    api.listen(
      () => void reload(),
      () => void reload(),
    );
  }

  const select = (ts: string) => setSelectedTs(ts);
  function moveSelection(delta: number) {
    const list = rows();
    if (list.length === 0) return;
    const idx = list.findIndex((r) => r.item.timestamp === selectedTs());
    const next = Math.max(0, Math.min(list.length - 1, (idx < 0 ? 0 : idx) + delta));
    setSelectedTs(list[next].item.timestamp);
  }

  async function togglePin() {
    const i = selected();
    if (!i) return;
    await api.pinItem(i.timestamp, !i.isUserPinned);
    await reload();
  }
  async function toggleSensitive() {
    const i = selected();
    if (!i) return;
    await api.setSensitive(i.timestamp, !i.isSensitive);
    await reload();
  }
  async function del() {
    const i = selected();
    if (!i) return;
    await api.deleteItem(i.timestamp);
    await reload();
  }
  async function commitRename(alias: string) {
    const ts = renameTs();
    if (ts) {
      await api.renameItem(ts, alias.trim() || null);
      await reload();
    }
    setRenameTs(null);
  }
  function beginRename() {
    if (selected()) setRenameTs(selected()!.timestamp);
  }

  /** Open the edit modal for text-bearing, non-sensitive items. */
  function beginEdit() {
    const item = selected();
    if (item?.textContent && !item.isSensitive) setEditTs(item.timestamp);
  }
  async function commitEdit(text: string) {
    const ts = editTs();
    setEditTs(null);
    if (!ts) return;
    // Empty text clears the override, restoring the original capture.
    await api.editItem(ts, text);
    await reload();
  }

  /**
   * Copy the selected item's text and (in Tauri) paste it into the source app.
   * The default path restores captured HTML alongside the text so rich-text
   * targets keep formatting; `plain` (Alt+Shift+Enter) writes text only.
   */
  async function chooseSelected(plain = false) {
    const item = selected();
    if (!item?.textContent) return;
    const html = !plain && item.htmlContent ? htmlPayload(item.htmlContent) : null;
    try {
      if (html) {
        await navigator.clipboard.write([
          new ClipboardItem({
            "text/html": new Blob([html], { type: "text/html" }),
            "text/plain": new Blob([item.textContent], { type: "text/plain" }),
          }),
        ]);
      } else {
        await navigator.clipboard.writeText(item.textContent);
      }
    } catch {
      try {
        await navigator.clipboard.writeText(item.textContent);
      } catch {
        /* clipboard not available in this context */
      }
    }
    await platformPaste();
  }

  async function clearAllHistory() {
    await api.clearAll();
    await reload();
  }

  async function saveSettings(s: ClipboardSettings) {
    await api.saveSettings(s);
    setSettings(s);
    applyTheme(s.theme);
    void platformSetHotkey(s.hotkey);
  }

  return {
    // state
    items,
    rows,
    selected,
    search,
    setSearch,
    tab,
    setTab,
    kind,
    setKind,
    group,
    setGroup,
    view,
    setView,
    selectedTs,
    settings,
    setSettings,
    status,
    conn,
    counts,
    quickLook,
    setQuickLook,
    actionsOpen,
    setActionsOpen,
    renameTs,
    setRenameTs,
    editTs,
    setEditTs,
    // ops
    init,
    reload,
    retry,
    loadMore,
    select,
    moveSelection,
    togglePin,
    toggleSensitive,
    del,
    beginRename,
    commitRename,
    beginEdit,
    commitEdit,
    chooseSelected,
    saveSettings,
    clearAllHistory,
  };
}

export const store = createRoot(makeStore);
