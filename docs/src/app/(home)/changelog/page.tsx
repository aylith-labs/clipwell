import {
  ArrowLeft,
  ArrowRight,
  ArrowUpRight,
  LockKeyhole,
  Pin,
  Sparkles,
} from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { FeatureFilm } from "@/components/changelog/feature-film";
import { buttonSecondary } from "@/components/landing/primitives";
import { ThemedImage } from "@/components/landing/themed-image";
import { ThemedClip } from "@/components/themed-clip";

export const metadata: Metadata = {
  title: "What's new in Clipwell",
  description:
    "Dated product updates, source links, and light and dark product captures from Clipwell.",
};

const source = "https://github.com/aylith-labs/clipwell";

export default function ChangelogPage() {
  return (
    <main className="mx-auto w-full max-w-6xl flex-1 px-6 pb-28">
      <div className="pt-10">
        <Link
          href="/"
          className="inline-flex items-center gap-2 text-sm text-fd-muted-foreground hover:text-fd-foreground"
        >
          <ArrowLeft className="size-4" /> Clipwell
        </Link>
      </div>

      <header className="relative overflow-hidden py-16 sm:py-24">
        <div className="pointer-events-none absolute -right-32 top-0 size-80 rounded-full bg-fd-primary/10 blur-3xl" />
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-fd-primary">
          From the workbench
        </p>
        <h1 className="mt-5 max-w-3xl text-balance text-5xl font-semibold tracking-tight sm:text-7xl">
          What&apos;s new in Clipwell
        </h1>
        <p className="mt-6 max-w-2xl text-lg leading-relaxed text-fd-muted-foreground">
          Product changes with the details that matter: what changed, why it
          helps, and where to see the code. These are dated source updates, not
          numbered releases.
        </p>
      </header>

      <FeatureFilm />

      <div className="mt-16 grid gap-10 border-t border-fd-border pt-12 lg:grid-cols-[13rem_minmax(0,1fr)]">
        <div className="text-sm text-fd-muted-foreground">
          <time dateTime="2026-09-24" className="font-mono">
            24 September 2026
          </time>
          <p className="mt-2 text-xs uppercase tracking-widest">
            Source update
          </p>
        </div>
        <article className="max-w-3xl">
          <div className="inline-flex items-center gap-2 rounded-full border border-fd-border bg-fd-card px-3 py-1 text-xs font-medium text-fd-muted-foreground">
            <LockKeyhole className="size-3.5 text-fd-primary" /> Privacy
          </div>
          <h2 className="mt-5 text-balance text-3xl font-semibold tracking-tight sm:text-4xl">
            Sensitive copies stay out of agent reads
          </h2>
          <p className="mt-5 text-base leading-relaxed text-fd-muted-foreground">
            Mark a clipboard item sensitive in the picker and Clipwell&apos;s
            MCP tools now omit it from recent items and search. Direct text
            reads refuse flagged items too. Your trusted picker still shows your
            full history, so you can manage the item yourself.
          </p>
          <div className="mt-7 grid gap-3 sm:grid-cols-3">
            {[
              ["01", "Mark", "Flag a private item as sensitive in the picker."],
              ["02", "Keep", "It remains in your local history for you."],
              [
                "03",
                "Protect",
                "MCP recent, search, and get-text do not expose it.",
              ],
            ].map(([number, title, detail]) => (
              <div
                key={number}
                className="rounded-xl border border-fd-border bg-fd-card p-4"
              >
                <span className="font-mono text-xs text-fd-primary">
                  {number}
                </span>
                <h3 className="mt-3 font-semibold">{title}</h3>
                <p className="mt-1 text-sm text-fd-muted-foreground">
                  {detail}
                </p>
              </div>
            ))}
          </div>
          <p className="mt-6 text-sm text-fd-muted-foreground">
            The change covers the daemon&apos;s HTTP MCP endpoint and the
            separate stdio MCP server. It does not hide sensitive items from
            trusted local REST picker clients.
          </p>
          <p className="mt-5 text-sm leading-relaxed text-fd-muted-foreground">
            This source-run study uses the actual browser-served web picker with
            seven fictional items in an isolated database and clipboard watching
            disabled. It is not footage of the native Windows picker or a real
            clipboard. The masked row is visible in the trusted picker; the MCP
            boundary described above comes from the source change, not a mocked
            agent response.
          </p>
          <figure className="mt-7 overflow-hidden rounded-xl border border-fd-border bg-fd-secondary">
            <ThemedImage
              name="i17-2026-09-25-db2d650/poster-draft"
              alt="Annotated Clipwell web picker showing a masked synthetic row and the source-backed MCP boundary"
              width={1600}
              height={900}
            />
            <figcaption className="px-4 py-3 text-sm text-fd-muted-foreground">
              Paired poster study from real web-picker pixels and a fictional
              fixture. The MCP result is source-backed, not simulated on screen.
            </figcaption>
          </figure>
          <ThemedClip
            name="i17-2026-09-25-db2d650/picker-search"
            poster="i17-2026-09-25-db2d650/picker-overview"
            width={1024}
            height={800}
            caption="Real, silent web-picker filtering footage in light and dark. It is source footage, not a narrated Cartesia/Remotion release film."
          />
          <p className="text-sm text-fd-muted-foreground">
            The final narrated films, timed captions, and listening review are
            still in production. Trusted local REST access remains available.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <a
              href={`${source}/commit/d14aa3a`}
              className="inline-flex items-center gap-1.5 text-sm font-medium text-fd-primary hover:underline"
            >
              Read the change <ArrowUpRight className="size-4" />
            </a>
            <Link
              href="/docs/integrations"
              className="inline-flex items-center gap-1.5 text-sm font-medium hover:underline"
            >
              MCP guide <ArrowRight className="size-4" />
            </Link>
          </div>
        </article>
      </div>

      <div className="mt-16 grid gap-10 border-t border-fd-border pt-12 lg:grid-cols-[13rem_minmax(0,1fr)]">
        <div className="text-sm text-fd-muted-foreground">
          <time dateTime="2026-08-02" className="font-mono">
            2 August 2026
          </time>
          <p className="mt-2 text-xs uppercase tracking-widest">
            Source update
          </p>
        </div>
        <article className="max-w-3xl">
          <div className="inline-flex items-center gap-2 rounded-full border border-fd-border bg-fd-card px-3 py-1 text-xs font-medium text-fd-muted-foreground">
            <Pin className="size-3.5 text-fd-primary" /> Reliability
          </div>
          <h2 className="mt-5 text-balance text-3xl font-semibold tracking-tight sm:text-4xl">
            Pinned means kept
          </h2>
          <p className="mt-5 text-base leading-relaxed text-fd-muted-foreground">
            Pinned copies now survive the retention sweep. Clipwell also saves
            pin, alias, and edit metadata through an atomic file replacement, so
            a crash during a save cannot truncate the live metadata file.
          </p>
          <p className="mt-5 text-sm leading-relaxed text-fd-muted-foreground">
            This hardening pass also fixed search and direct reads across the
            full history, instead of only a fixed recent window. The source
            commit documents the other daemon and client fixes in detail.
          </p>
          <a
            href={`${source}/commit/6e0e4d5`}
            className="mt-6 inline-flex items-center gap-1.5 text-sm font-medium text-fd-primary hover:underline"
          >
            Read the change <ArrowUpRight className="size-4" />
          </a>
        </article>
      </div>

      <section className="mt-20 rounded-2xl border border-fd-border bg-fd-card p-6 sm:p-10">
        <div className="grid items-center gap-8 lg:grid-cols-2">
          <div>
            <div className="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.2em] text-fd-primary">
              <Sparkles className="size-4" /> Explore the picker
            </div>
            <h2 className="mt-4 text-3xl font-semibold tracking-tight">
              A history you can actually use
            </h2>
            <p className="mt-4 text-fd-muted-foreground">
              Filter by kind, inspect a copy in Detail view, and get back to the
              app you were using. The product captures here show the interface
              in both light and dark themes.
            </p>
            <Link href="/docs/scenarios" className={`${buttonSecondary} mt-6`}>
              Explore workflows <ArrowRight className="size-4" />
            </Link>
          </div>
          <div className="overflow-hidden rounded-xl border border-fd-border bg-fd-secondary shadow-xl">
            <ThemedImage
              name="detail"
              alt="Clipwell Detail view with selected clipboard item and metadata"
              width={1143}
              height={847}
            />
          </div>
        </div>
      </section>
    </main>
  );
}
