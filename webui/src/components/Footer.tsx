import { For } from "solid-js";

const HINTS: { keys: string; label: string }[] = [
  { keys: "↵", label: "Paste" },
  { keys: "Alt⇧↵", label: "Plain" },
  { keys: "Ctrl+Y", label: "Peek" },
  { keys: "Ctrl+K", label: "Actions" },
  { keys: "Ctrl+E", label: "Edit" },
];

/** Status line plus keycap hints for the core chords. */
export function Footer(props: { status: string }) {
  return (
    <div class="flex items-center justify-between gap-3 text-[11px] text-zinc-500 dark:text-zinc-400">
      <span class="min-w-0 truncate">{props.status}</span>
      <span class="hidden shrink-0 gap-2.5 sm:flex">
        <For each={HINTS}>
          {(hint) => (
            <span class="flex items-center gap-1">
              <kbd class="rounded border border-zinc-300 bg-zinc-50 px-1 py-px font-mono text-[10px] dark:border-zinc-700 dark:bg-zinc-900">
                {hint.keys}
              </kbd>
              {hint.label}
            </span>
          )}
        </For>
      </span>
    </div>
  );
}
