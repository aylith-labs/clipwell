import { RefreshCw, TriangleAlert } from "../lib/icons";
import type { ConnState } from "../store";

/** Daemon-unreachable strip under the search box, with a Retry affordance. */
export function ErrorBanner(props: { conn: ConnState; onRetry: () => void }) {
  return (
    <div class="flex items-center gap-2 rounded-lg border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-700 dark:text-red-300">
      <TriangleAlert size={15} strokeWidth={1.75} class="shrink-0" />
      <span class="min-w-0 flex-1">Daemon unreachable. Showing the last loaded history.</span>
      <button
        type="button"
        onClick={props.onRetry}
        class="flex shrink-0 items-center gap-1.5 rounded-md bg-red-600/90 px-2.5 py-1 text-xs font-medium text-white transition-colors hover:bg-red-600"
      >
        <RefreshCw
          size={12}
          strokeWidth={2}
          class={props.conn === "retrying" ? "animate-spin motion-reduce:animate-none" : ""}
        />
        Retry
      </button>
    </div>
  );
}
