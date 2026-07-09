import { createSignal, For, Show } from "solid-js";
import { Dynamic } from "solid-js/web";
import {
  Activity,
  EllipsisVertical,
  Lock,
  PanelRight,
  Pin,
  Rows3,
  Settings,
  Trash2,
} from "../lib/icons";
import { store } from "../store";
import { type GroupMode, KIND_OPTIONS, type Tab } from "../types";

const tabClass = (active: boolean) =>
  `flex items-center gap-1.5 rounded-md px-2.5 py-1 text-sm transition-colors ${
    active
      ? "bg-accent/12 font-medium text-accent dark:bg-accent-soft/12 dark:text-accent-soft"
      : "text-zinc-500 hover:bg-zinc-200/70 hover:text-zinc-900 dark:text-zinc-400 dark:hover:bg-zinc-800/70 dark:hover:text-zinc-100"
  }`;

const controlClass =
  "rounded-md border border-zinc-300 bg-transparent px-2 py-1 text-sm dark:border-zinc-700";

function TabCount(props: { value: number | undefined }) {
  return (
    <Show when={props.value !== undefined}>
      <span class="tabular-nums opacity-65">{props.value}</span>
    </Show>
  );
}

export function FilterBar(props: { onOpenSettings: () => void; onOpenDiagnostics: () => void }) {
  const [menuOpen, setMenuOpen] = createSignal(false);
  const counts = () => store.counts();
  const kindLabel = (option: { label: string; value: string }) => {
    const all = counts();
    if (!all) return option.label;
    const count = option.value === "all" ? all.total : (all.kinds[option.value] ?? 0);
    return `${option.label} (${count})`;
  };
  const setTab = (tab: Tab) => store.setTab(tab);

  const menuItem =
    "flex w-full items-center gap-2 px-3 py-1.5 text-left text-sm transition-colors hover:bg-zinc-100 dark:hover:bg-zinc-800";

  return (
    <div class="flex flex-wrap items-center gap-2">
      <button type="button" class={tabClass(store.tab() === "all")} onClick={() => setTab("all")}>
        All
        <TabCount value={counts()?.total} />
      </button>
      <button
        type="button"
        class={tabClass(store.tab() === "pinned")}
        onClick={() => setTab("pinned")}
        title="Pinned"
      >
        <Pin size={13} strokeWidth={1.75} />
        <TabCount value={counts()?.pinned} />
      </button>
      <button
        type="button"
        class={tabClass(store.tab() === "sensitive")}
        onClick={() => setTab("sensitive")}
        title="Sensitive"
      >
        <Lock size={13} strokeWidth={1.75} />
        <TabCount value={counts()?.sensitive} />
      </button>
      <button
        type="button"
        class={tabClass(false)}
        onClick={() => store.setView(store.view() === "detail" ? "compact" : "detail")}
        title="Compact / Detail"
      >
        <Dynamic
          component={store.view() === "detail" ? Rows3 : PanelRight}
          size={14}
          strokeWidth={1.75}
        />
      </button>
      <div class="flex-1" />
      <select
        class={controlClass}
        value={store.group()}
        onChange={(event) => store.setGroup(event.currentTarget.value as GroupMode)}
      >
        <option value="none">No grouping</option>
        <option value="date">By date</option>
        <option value="source">By source</option>
      </select>
      <select
        class={controlClass}
        value={store.kind()}
        onChange={(event) => store.setKind(event.currentTarget.value)}
      >
        <For each={KIND_OPTIONS}>
          {(option) => <option value={option.value}>{kindLabel(option)}</option>}
        </For>
      </select>
      <div class="relative">
        <button
          type="button"
          class={`${controlClass} flex items-center`}
          onClick={() => setMenuOpen(!menuOpen())}
          title="More"
        >
          <EllipsisVertical size={15} strokeWidth={1.75} />
        </button>
        <Show when={menuOpen()}>
          <div class="absolute right-0 z-20 mt-1 w-44 rounded-lg border border-zinc-200 bg-white py-1 shadow-lg dark:border-zinc-700 dark:bg-zinc-900">
            <button
              type="button"
              class={menuItem}
              onClick={() => {
                setMenuOpen(false);
                props.onOpenSettings();
              }}
            >
              <Settings size={14} strokeWidth={1.75} class="text-zinc-500 dark:text-zinc-400" />
              Settings…
            </button>
            <button
              type="button"
              class={menuItem}
              onClick={() => {
                setMenuOpen(false);
                props.onOpenDiagnostics();
              }}
            >
              <Activity size={14} strokeWidth={1.75} class="text-zinc-500 dark:text-zinc-400" />
              Diagnostics…
            </button>
            <button
              type="button"
              class={`${menuItem} text-red-600 dark:text-red-400`}
              onClick={() => {
                setMenuOpen(false);
                if (confirm("Delete ALL clipboard history?")) void store.clearAllHistory();
              }}
            >
              <Trash2 size={14} strokeWidth={1.75} />
              Clear history…
            </button>
          </div>
        </Show>
      </div>
    </div>
  );
}
