import { Terminal } from "lucide-react";
import type { ReactNode } from "react";

/** A terminal-styled card: window chrome dots + a title, over a monospace body. */
export function TerminalCard({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="overflow-hidden rounded-xl border border-fd-border bg-fd-card text-left shadow-sm">
      <div className="flex items-center gap-1.5 border-b border-fd-border px-4 py-2.5">
        <span className="size-2.5 rounded-full border border-fd-border bg-fd-secondary" />
        <span className="size-2.5 rounded-full border border-fd-border bg-fd-secondary" />
        <span className="size-2.5 rounded-full border border-fd-border bg-fd-secondary" />
        <span className="ml-auto flex items-center gap-1.5 text-xs text-fd-muted-foreground">
          <Terminal className="size-3.5" strokeWidth={1.75} />
          {title}
        </span>
      </div>
      <div className="overflow-x-auto p-4 font-mono text-[13px] leading-6">
        {children}
      </div>
    </div>
  );
}
