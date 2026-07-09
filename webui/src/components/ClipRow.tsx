import { createSignal, Show } from "solid-js";
import { Dynamic } from "solid-js/web";
import { imageUrl } from "../lib/client";
import {
  colorSwatch,
  faviconUrl,
  isMonoPreview,
  type MetaOptions,
  meta,
  preview,
} from "../lib/format";
import { kindIcon, Lock, Pencil, Pin } from "../lib/icons";
import type { Row } from "../store";
import { Highlighted } from "./Highlighted";

export function ClipRow(props: {
  row: Row;
  selected: boolean;
  metaOpts: MetaOptions;
  search: string;
  onSelect: () => void;
  onChoose: () => void;
}) {
  const [iconFailed, setIconFailed] = createSignal(false);
  const item = () => props.row.item;
  const thumb = () => item().hasImage && !item().isSensitive;
  const favicon = () => (item().isSensitive ? null : faviconUrl(item()));
  const showImageIcon = () => thumb() || (favicon() && !iconFailed());
  const swatch = () => colorSwatch(item());

  return (
    <>
      <Show when={props.row.header}>
        <div class="sticky top-0 z-10 bg-zinc-100/85 px-2 pt-3 pb-1 text-[11px] font-semibold uppercase tracking-wide text-zinc-400 backdrop-blur dark:bg-zinc-950/85 dark:text-zinc-500">
          {props.row.header}
        </div>
      </Show>
      <button
        type="button"
        onClick={props.onSelect}
        onDblClick={props.onChoose}
        class={`flex w-full items-start gap-3 rounded-lg px-3 py-2 text-left transition-colors ${
          props.selected
            ? "bg-accent/10 shadow-[inset_2px_0_0_var(--color-accent)] dark:bg-accent-soft/10 dark:shadow-[inset_2px_0_0_var(--color-accent-soft)]"
            : "hover:bg-zinc-200/70 dark:hover:bg-zinc-800/70"
        }`}
        title={item().isSensitive ? "" : (item().textContent ?? "")}
      >
        <span class="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center">
          <Show
            when={showImageIcon()}
            fallback={
              <Dynamic
                component={item().isSensitive ? Lock : kindIcon(item().kind)}
                size={18}
                strokeWidth={1.75}
                class={
                  props.selected
                    ? "text-accent dark:text-accent-soft"
                    : "text-zinc-500 dark:text-zinc-400"
                }
              />
            }
          >
            <img
              src={thumb() ? imageUrl(item().timestamp) : (favicon() as string)}
              alt=""
              class="h-6 w-6 rounded object-cover"
              onError={() => setIconFailed(true)}
            />
          </Show>
        </span>
        <span class="min-w-0 flex-1">
          <span
            class={`line-clamp-2 break-words text-sm ${isMonoPreview(item()) ? "font-mono text-[12.5px]" : ""}`}
          >
            <Show when={swatch()}>
              <span
                class="mr-1.5 inline-block h-2.5 w-2.5 rounded-[3px] border border-zinc-300 align-middle dark:border-zinc-600"
                style={{ background: swatch() ?? undefined }}
              />
            </Show>
            <Show when={!item().isSensitive} fallback={preview(item())}>
              <Highlighted text={preview(item())} query={props.search} />
            </Show>
          </span>
          <span class="flex items-center gap-1.5 text-[11px] text-zinc-500 dark:text-zinc-400">
            <span class="min-w-0 truncate">{meta(item(), props.metaOpts)}</span>
            <Show when={item().isEdited}>
              <span class="flex shrink-0 items-center gap-0.5 rounded-md bg-zinc-200/70 px-1 text-[10px] dark:bg-zinc-800">
                <Pencil size={9} strokeWidth={1.75} />
                edited
              </span>
            </Show>
          </span>
        </span>
        <Show when={item().isUserPinned}>
          <Pin
            size={13}
            strokeWidth={1.75}
            class="mt-0.5 shrink-0 text-accent dark:text-accent-soft"
          />
        </Show>
      </button>
    </>
  );
}
