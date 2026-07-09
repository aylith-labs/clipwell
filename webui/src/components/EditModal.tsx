import { createSignal } from "solid-js";
import { SquarePen } from "../lib/icons";

/** Modal editor for an item's text; saving stores a non-destructive override. */
export function EditModal(props: {
  initial: string;
  onSave: (text: string) => void;
  onClose: () => void;
}) {
  const [draft, setDraft] = createSignal(props.initial);

  const onKey = (event: KeyboardEvent) => {
    if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      props.onSave(draft());
    } else if (event.key === "Escape") {
      event.preventDefault();
      props.onClose();
    }
  };

  return (
    <div
      class="absolute inset-0 z-50 flex justify-center bg-black/60 pt-16"
      onClick={props.onClose}
    >
      <div
        class="h-fit w-[520px] max-w-[92%] rounded-xl bg-white p-5 shadow-2xl dark:bg-zinc-900"
        onClick={(event) => event.stopPropagation()}
      >
        <div class="mb-3 flex items-center gap-2">
          <SquarePen size={16} strokeWidth={1.75} class="text-zinc-500 dark:text-zinc-400" />
          <h2 class="text-base font-semibold">Edit content</h2>
        </div>
        <textarea
          autofocus
          value={draft()}
          onInput={(event) => setDraft(event.currentTarget.value)}
          onKeyDown={onKey}
          class="min-h-[240px] w-full resize-y rounded-lg border border-zinc-300 bg-transparent p-3 font-mono text-[12.5px] outline-none focus:border-accent dark:border-zinc-700 dark:focus:border-accent-soft"
        />
        <div class="mt-2 flex items-center justify-between text-xs text-zinc-500 dark:text-zinc-400">
          <span>{draft().length.toLocaleString()} chars · empty text restores the original</span>
          <span class="flex gap-3">
            <span>
              <kbd class="rounded border border-zinc-300 bg-zinc-50 px-1 font-mono text-[10px] dark:border-zinc-700 dark:bg-zinc-800">
                Ctrl+↵
              </kbd>{" "}
              Save
            </span>
            <span>
              <kbd class="rounded border border-zinc-300 bg-zinc-50 px-1 font-mono text-[10px] dark:border-zinc-700 dark:bg-zinc-800">
                Esc
              </kbd>{" "}
              Cancel
            </span>
          </span>
        </div>
        <div class="mt-4 flex justify-end gap-2">
          <button
            type="button"
            class="rounded-md px-4 py-2 text-sm transition-colors hover:bg-zinc-100 dark:hover:bg-zinc-800"
            onClick={props.onClose}
          >
            Cancel
          </button>
          <button
            type="button"
            class="rounded-md bg-accent px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-accent/90"
            onClick={() => props.onSave(draft())}
          >
            Save
          </button>
        </div>
      </div>
    </div>
  );
}
