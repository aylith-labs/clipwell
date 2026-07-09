import { createSignal, For, onCleanup, onMount, Show } from "solid-js";
import { Dynamic } from "solid-js/web";
import { ActionPalette } from "./components/ActionPalette";
import { ClipRow } from "./components/ClipRow";
import { DiagnosticsModal } from "./components/DiagnosticsModal";
import { EditModal } from "./components/EditModal";
import { ErrorBanner } from "./components/ErrorBanner";
import { FilterBar } from "./components/FilterBar";
import { Footer } from "./components/Footer";
import { QuickLook } from "./components/QuickLook";
import { SettingsModal } from "./components/SettingsModal";
import { imageUrl } from "./lib/client";
import { fullText, type MetaOptions, meta } from "./lib/format";
import { kindIcon, Search } from "./lib/icons";
import { hide as platformHide } from "./lib/platform";
import { store } from "./store";

export function App() {
  const [settingsOpen, setSettingsOpen] = createSignal(false);
  const [diagOpen, setDiagOpen] = createSignal(false);
  const [renameDraft, setRenameDraft] = createSignal("");

  let searchEl: HTMLInputElement | undefined;
  const overlayOpen = () =>
    settingsOpen() ||
    diagOpen() ||
    store.actionsOpen() ||
    store.quickLook() ||
    store.renameTs() !== null ||
    store.editTs() !== null;

  const metaOpts = (): MetaOptions => ({
    showSource: store.settings().showSource,
    showTime: store.settings().showTime,
    showChars: store.settings().showChars,
  });

  onMount(() => {
    void store.init();
    window.addEventListener("keydown", onKey);
  });
  onCleanup(() => window.removeEventListener("keydown", onKey));

  function closeAll() {
    setSettingsOpen(false);
    setDiagOpen(false);
    store.setActionsOpen(false);
    store.setQuickLook(false);
    store.setRenameTs(null);
    store.setEditTs(null);
  }

  function openActions() {
    if (store.selected()) store.setActionsOpen(true);
  }
  function beginRename() {
    store.beginRename();
    setRenameDraft(store.selected()?.alias ?? "");
  }

  function onKey(e: KeyboardEvent) {
    const typing = ["INPUT", "TEXTAREA", "SELECT"].includes(document.activeElement?.tagName ?? "");
    if (e.key === "Escape") {
      if (overlayOpen()) {
        closeAll();
      } else {
        void platformHide();
      }
      return;
    }
    // Overlays own the keyboard while open.
    if (overlayOpen()) return;

    const ctrl = e.ctrlKey || e.metaKey;
    if (e.key === "ArrowDown") {
      e.preventDefault();
      store.moveSelection(1);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      store.moveSelection(-1);
    } else if (e.key === "Enter" && e.altKey && e.shiftKey) {
      e.preventDefault();
      void store.chooseSelected(true);
    } else if (e.key === "Enter") {
      e.preventDefault();
      void store.chooseSelected();
    } else if (e.key === "Delete" && !typing) {
      void store.del();
    } else if (ctrl && e.key.toLowerCase() === "p") {
      e.preventDefault();
      void store.togglePin();
    } else if (ctrl && e.shiftKey && e.key.toLowerCase() === "l") {
      // Old-app chords: Ctrl+E edits, Ctrl+Shift+L toggles sensitive.
      e.preventDefault();
      void store.toggleSensitive();
    } else if (ctrl && e.key.toLowerCase() === "e") {
      e.preventDefault();
      store.beginEdit();
    } else if (ctrl && e.key.toLowerCase() === "y") {
      e.preventDefault();
      if (store.selected()) store.setQuickLook(true);
    } else if (ctrl && e.key.toLowerCase() === "k") {
      e.preventDefault();
      openActions();
    } else if (ctrl && e.key === "1") {
      store.setTab("all");
    } else if (ctrl && e.key === "2") {
      store.setTab("pinned");
    } else if (ctrl && e.key === "3") {
      store.setTab("sensitive");
    } else if (e.key === "F2") {
      e.preventDefault();
      beginRename();
    }
  }

  function onScroll(e: Event) {
    const el = e.currentTarget as HTMLElement;
    if (el.scrollHeight - el.scrollTop - el.clientHeight < 400) void store.loadMore();
  }

  return (
    <div class="app-card relative flex h-full flex-col gap-3 p-4">
      {/* Search */}
      <div class="relative">
        <Search
          size={16}
          strokeWidth={1.75}
          class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-zinc-400 dark:text-zinc-500"
        />
        <input
          ref={searchEl}
          autofocus
          value={store.search()}
          onInput={(e) => store.setSearch(e.currentTarget.value)}
          placeholder="Search clipboard history…"
          class="w-full rounded-lg border border-zinc-300 bg-white py-2 pr-3 pl-9 text-base outline-none transition-colors focus:border-accent dark:border-zinc-700 dark:bg-zinc-900 dark:focus:border-accent-soft"
        />
      </div>

      {/* Filter bar */}
      <FilterBar
        onOpenSettings={() => setSettingsOpen(true)}
        onOpenDiagnostics={() => setDiagOpen(true)}
      />

      {/* Daemon-unreachable banner */}
      <Show when={store.conn() !== "connected"}>
        <ErrorBanner conn={store.conn()} onRetry={() => void store.retry()} />
      </Show>

      {/* List + optional detail pane */}
      <div class="flex min-h-0 flex-1 gap-3">
        <div class="relative min-w-0 flex-1">
          {/* Rename bar */}
          <Show when={store.renameTs()}>
            <div class="absolute inset-x-0 top-0 z-10 flex items-center gap-2 rounded-lg border border-accent bg-white p-2 dark:border-accent-soft dark:bg-zinc-900">
              <span class="text-sm text-zinc-500 dark:text-zinc-400">Rename:</span>
              <input
                autofocus
                class="flex-1 rounded-md border border-zinc-300 bg-transparent px-2 py-1 outline-none dark:border-zinc-700"
                value={renameDraft()}
                onInput={(e) => setRenameDraft(e.currentTarget.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    e.preventDefault();
                    void store.commitRename(renameDraft());
                    searchEl?.focus();
                  } else if (e.key === "Escape") {
                    e.preventDefault();
                    store.setRenameTs(null);
                    searchEl?.focus();
                  }
                }}
                placeholder="alias (empty to clear)"
              />
            </div>
          </Show>
          <div class="h-full overflow-auto" onScroll={onScroll}>
            <For each={store.rows()} fallback={<div class="p-4 opacity-50">{store.status()}</div>}>
              {(row) => (
                <ClipRow
                  row={row}
                  selected={row.item.timestamp === store.selectedTs()}
                  metaOpts={metaOpts()}
                  search={store.search()}
                  onSelect={() => store.select(row.item.timestamp)}
                  onChoose={() => void store.chooseSelected()}
                />
              )}
            </For>
          </div>
        </div>

        <Show when={store.view() === "detail" && store.selected()}>
          <div class="w-[340px] shrink-0 overflow-auto rounded-lg bg-zinc-200/50 p-4 dark:bg-zinc-900/60">
            {(() => {
              const item = store.selected()!;
              const showImage = item.hasImage && !item.isSensitive;
              return (
                <>
                  <div class="flex items-center gap-2 text-sm font-semibold">
                    <Dynamic
                      component={kindIcon(item.kind)}
                      size={14}
                      strokeWidth={1.75}
                      class="text-zinc-500 dark:text-zinc-400"
                    />
                    {item.kind ?? "text"}
                  </div>
                  <div class="mb-3 text-[11px] text-zinc-500 dark:text-zinc-400">
                    {meta(item, metaOpts())}
                  </div>
                  <Show
                    when={showImage}
                    fallback={
                      <pre class="whitespace-pre-wrap break-words font-mono text-[12.5px]">
                        {fullText(item)}
                      </pre>
                    }
                  >
                    <img src={imageUrl(item.timestamp)} alt="" class="max-w-full rounded" />
                  </Show>
                </>
              );
            })()}
          </div>
        </Show>
      </div>

      {/* Status + hints */}
      <Footer status={store.status()} />

      {/* Overlays */}
      <Show when={store.quickLook() && store.selected()}>
        <QuickLook
          item={store.selected()!}
          metaOpts={metaOpts()}
          onClose={() => store.setQuickLook(false)}
        />
      </Show>
      <Show when={store.actionsOpen() && store.selected()}>
        <ActionPalette item={store.selected()!} onClose={() => store.setActionsOpen(false)} />
      </Show>
      <Show when={store.editTs() && store.selected()}>
        <EditModal
          initial={store.selected()?.textContent ?? ""}
          onSave={(text) => void store.commitEdit(text)}
          onClose={() => store.setEditTs(null)}
        />
      </Show>
      <Show when={settingsOpen()}>
        <SettingsModal
          settings={store.settings()}
          onClose={() => setSettingsOpen(false)}
          onSave={(s) => {
            void store.saveSettings(s);
            setSettingsOpen(false);
          }}
        />
      </Show>
      <Show when={diagOpen()}>
        <DiagnosticsModal itemCount={store.items().length} onClose={() => setDiagOpen(false)} />
      </Show>
    </div>
  );
}
