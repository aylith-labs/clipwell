import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

/** Consistent keyboard-focus ring for interactive landing elements. */
export const focusRing =
  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fd-primary focus-visible:ring-offset-2 focus-visible:ring-offset-fd-background";

const buttonBase =
  "inline-flex h-11 items-center gap-2 rounded-lg px-5 text-sm font-medium";

export const buttonPrimary = cn(
  buttonBase,
  "bg-fd-primary text-fd-primary-foreground transition-colors hover:bg-fd-primary/90",
  focusRing,
);

export const buttonSecondary = cn(
  buttonBase,
  "border border-fd-border bg-fd-card transition-colors hover:bg-fd-accent",
  focusRing,
);

/** A boxed lucide icon used as a feature glyph. */
export function IconChip({ icon: Icon }: { icon: LucideIcon }) {
  return (
    <span className="flex size-9 items-center justify-center rounded-lg border border-fd-border bg-fd-secondary">
      <Icon className="size-4" strokeWidth={1.75} />
    </span>
  );
}

/** A small monospace pill, e.g. a detected item kind or an endpoint. */
export function Chip({ children }: { children: ReactNode }) {
  return (
    <span className="rounded-md border border-fd-border bg-fd-secondary px-1.5 py-0.5 font-mono text-xs text-fd-muted-foreground">
      {children}
    </span>
  );
}

/** Section eyebrow + heading, with an optional lede paragraph. */
export function SectionHeader({
  eyebrow,
  title,
  lede,
}: {
  eyebrow: string;
  title: string;
  lede?: string;
}) {
  return (
    <div>
      <p className="text-xs font-medium uppercase tracking-widest text-fd-muted-foreground">
        {eyebrow}
      </p>
      <h2 className="mt-2 text-3xl font-semibold tracking-tight">{title}</h2>
      {lede ? (
        <p className="mt-3 max-w-2xl text-fd-muted-foreground">{lede}</p>
      ) : null}
    </div>
  );
}
