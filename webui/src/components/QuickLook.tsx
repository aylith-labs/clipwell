import { Show } from "solid-js";
import { Dynamic } from "solid-js/web";
import { imageUrl } from "../lib/client";
import { fullText, type MetaOptions, meta } from "../lib/format";
import { kindIcon, Pencil } from "../lib/icons";
import type { ClipItem } from "../types";

export function QuickLook(props: { item: ClipItem; metaOpts: MetaOptions; onClose: () => void }) {
  const showImage = () => props.item.hasImage && !props.item.isSensitive;
  return (
    <div class="absolute inset-0 z-30 flex bg-black/75 p-9" onClick={props.onClose}>
      <div
        class="flex w-full flex-col rounded-xl bg-white p-5 shadow-2xl dark:bg-zinc-900"
        onClick={(e) => e.stopPropagation()}
      >
        <div class="mb-3">
          <div class="flex items-center gap-2 text-sm font-semibold">
            <Dynamic
              component={kindIcon(props.item.kind)}
              size={15}
              strokeWidth={1.75}
              class="text-zinc-500 dark:text-zinc-400"
            />
            {props.item.kind ?? "text"}
            <Show when={props.item.isEdited}>
              <span class="flex items-center gap-0.5 rounded-md bg-zinc-200/70 px-1.5 py-0.5 text-[10px] font-normal text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400">
                <Pencil size={9} strokeWidth={1.75} />
                edited
              </span>
            </Show>
          </div>
          <div class="text-[11px] text-zinc-500 dark:text-zinc-400">
            {meta(props.item, props.metaOpts)}
          </div>
        </div>
        <div class="min-h-0 flex-1 overflow-auto">
          <Show
            when={showImage()}
            fallback={
              <pre class="whitespace-pre-wrap break-words font-mono text-sm">
                {fullText(props.item)}
              </pre>
            }
          >
            <img src={imageUrl(props.item.timestamp)} alt="" class="max-h-full rounded" />
          </Show>
        </div>
      </div>
    </div>
  );
}
