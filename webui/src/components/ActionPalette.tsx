import { createMemo, createSignal, For, Show } from "solid-js";
import { Dynamic } from "solid-js/web";
import { actionsFor } from "../lib/actions";
import { ACTION_ICONS } from "../lib/icons";
import type { ClipItem } from "../types";

export function ActionPalette(props: { item: ClipItem; onClose: () => void }) {
  const [query, setQuery] = createSignal("");
  const [idx, setIdx] = createSignal(0);
  const all = actionsFor(props.item);
  const filtered = createMemo(() =>
    all.filter((a) => a.label.toLowerCase().includes(query().trim().toLowerCase())),
  );

  const run = async () => {
    const a = filtered()[idx()];
    props.onClose();
    if (a) await a.execute(props.item);
  };

  const onKey = (e: KeyboardEvent) => {
    if (e.key === "Enter") {
      e.preventDefault();
      void run();
    } else if (e.key === "Escape") {
      e.preventDefault();
      props.onClose();
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      setIdx((i) => Math.min(filtered().length - 1, i + 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setIdx((i) => Math.max(0, i - 1));
    }
  };

  return (
    <div
      class="absolute inset-0 z-40 flex justify-center bg-black/60 pt-20"
      onClick={props.onClose}
    >
      <div
        class="h-fit w-[420px] rounded-xl bg-white p-2 shadow-2xl dark:bg-zinc-900"
        onClick={(e) => e.stopPropagation()}
      >
        <input
          autofocus
          value={query()}
          onInput={(e) => {
            setQuery(e.currentTarget.value);
            setIdx(0);
          }}
          onKeyDown={onKey}
          placeholder="Actions…"
          class="mb-2 w-full rounded-md border border-zinc-300 bg-transparent px-3 py-2 outline-none transition-colors focus:border-accent dark:border-zinc-700 dark:focus:border-accent-soft"
        />
        <For each={filtered()} fallback={<div class="px-3 py-2 opacity-50">No actions</div>}>
          {(a, i) => (
            <button
              type="button"
              onMouseEnter={() => setIdx(i())}
              onClick={() => void run()}
              class={`flex w-full items-center gap-2.5 rounded-md px-3 py-2 text-left transition-colors ${
                i() === idx()
                  ? "bg-accent/12 font-medium text-accent dark:bg-accent-soft/12 dark:text-accent-soft"
                  : "hover:bg-zinc-100 dark:hover:bg-zinc-800"
              }`}
            >
              <Show when={ACTION_ICONS[a.id]}>
                <Dynamic
                  component={ACTION_ICONS[a.id]}
                  size={15}
                  strokeWidth={1.75}
                  class={
                    i() === idx()
                      ? "text-accent dark:text-accent-soft"
                      : "text-zinc-500 dark:text-zinc-400"
                  }
                />
              </Show>
              {a.label}
            </button>
          )}
        </For>
        <div class="mt-1 flex justify-end gap-3 border-t border-zinc-200 px-3 py-2 text-[11px] text-zinc-500 dark:border-zinc-800 dark:text-zinc-400">
          <span>
            <kbd class="rounded border border-zinc-300 bg-zinc-50 px-1 font-mono text-[10px] dark:border-zinc-700 dark:bg-zinc-800">
              ↵
            </kbd>{" "}
            Run
          </span>
          <span>
            <kbd class="rounded border border-zinc-300 bg-zinc-50 px-1 font-mono text-[10px] dark:border-zinc-700 dark:bg-zinc-800">
              Esc
            </kbd>{" "}
            Close
          </span>
        </div>
      </div>
    </div>
  );
}
