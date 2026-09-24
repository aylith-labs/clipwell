import {
  AppWindow,
  ArrowRight,
  ArrowUpRight,
  Bot,
  Braces,
  Globe,
  Monitor,
  Plug,
  Puzzle,
  Radio,
  Tags,
  Zap,
} from "lucide-react";
import Link from "next/link";
import {
  buttonPrimary,
  buttonSecondary,
  Chip,
  focusRing,
  IconChip,
  SectionHeader,
} from "@/components/landing/primitives";
import { TerminalCard } from "@/components/landing/terminal";
import { ThemedImage } from "@/components/landing/themed-image";
import { cn } from "@/lib/cn";
import { gitConfig } from "@/lib/shared";

const githubUrl = `https://github.com/${gitConfig.user}/${gitConfig.repo}`;

const glow =
  "radial-gradient(60% 50% at 50% 0%, color-mix(in oklab, var(--color-fd-primary) 22%, transparent), transparent)";

const itemKinds = [
  "url",
  "github-pr",
  "jira",
  "email",
  "color",
  "path",
  "code",
  "image",
];

const platforms = [
  {
    icon: Monitor,
    title: "Cross-platform",
    body: "Windows, macOS, and Linux behind one clipboard-watcher interface. One daemon, one API.",
  },
  {
    icon: Puzzle,
    title: "Extensible via plugins",
    body: "Detectors and Ctrl+K actions load from external assemblies through a small public contract.",
  },
  {
    icon: Globe,
    title: "Native or web",
    body: "A native Avalonia picker, plus a Solid web UI with full parity: wrap it with Tauri or open it in any browser at /app.",
  },
];

const protocols = [
  {
    icon: Braces,
    name: "REST",
    desc: "GET /api/clipboard, settings, image, counts, delete, clear. OpenAPI spec included.",
    endpoint: "GET /api/clipboard",
  },
  {
    icon: Radio,
    name: "WebSocket / SSE",
    desc: "A clipboard.changed event on every capture, pushed live to subscribers.",
    endpoint: "GET /api/clipboard/ws",
  },
  {
    icon: Bot,
    name: "MCP",
    desc: "clipboard_recent, search, get_text, and clear for AI agents, over HTTP/SSE at /mcp or stdio.",
    endpoint: "POST /mcp",
  },
];

export default function HomePage() {
  return (
    <main className="flex flex-1 flex-col">
      {/* Hero */}
      <section className="relative overflow-hidden px-6 pt-14 pb-20 sm:pt-20 lg:pt-10 lg:pb-12">
        <div
          className="pointer-events-none absolute inset-0 -z-10 opacity-50"
          style={{ background: glow }}
        />
        <div className="mx-auto grid w-full max-w-6xl items-center gap-12 lg:min-h-[min(640px,calc(100svh-10rem))] lg:grid-cols-[minmax(0,1fr)_minmax(0,1.02fr)] lg:gap-16">
          <div>
            <a
              href={githubUrl}
              className={cn(
                "inline-flex items-center gap-2 rounded-full border border-fd-border bg-fd-card px-3 py-1 text-xs font-medium text-fd-muted-foreground transition-colors hover:border-fd-muted-foreground/40 hover:text-fd-foreground",
                focusRing,
              )}
            >
              <span className="size-1.5 rounded-full bg-fd-primary" />
              Open source
              <span className="h-3 w-px bg-fd-border" />
              MIT
              <span className="h-3 w-px bg-fd-border" />
              .NET 10 + Avalonia
            </a>
            <h1 className="mt-8 max-w-2xl text-balance text-5xl font-semibold leading-[1.06] tracking-tight sm:text-6xl xl:text-7xl">
              Your clipboard,
              <br />
              in focus.
            </h1>
            <p className="mt-6 max-w-xl text-balance text-lg leading-relaxed text-fd-muted-foreground">
              Find a copy in a few keystrokes. Keep it local. Let the native
              picker, web UI, CLI, and AI tools work from the same clipboard
              API.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <Link href="/docs/install" className={cn(buttonPrimary, "group")}>
                Get started
                <ArrowRight className="size-4 transition-transform group-hover:translate-x-0.5 motion-reduce:transition-none motion-reduce:group-hover:translate-x-0" />
              </Link>
              <Link href="/changelog" className={buttonSecondary}>
                See what&apos;s new
              </Link>
            </div>
            <p className="mt-7 text-xs font-medium uppercase tracking-[0.18em] text-fd-muted-foreground">
              Local history <span className="mx-2 text-fd-primary">·</span>{" "}
              Typed search <span className="mx-2 text-fd-primary">·</span> Open
              API
            </p>
          </div>
          <div className="relative mx-auto w-full max-w-[580px] lg:max-w-none">
            <div className="absolute -inset-4 rounded-[2rem] bg-fd-primary/10 blur-3xl" />
            <div className="relative overflow-hidden rounded-[1.4rem] border border-fd-border bg-fd-card p-2 shadow-2xl shadow-fd-primary/10">
              <div className="flex items-center justify-between px-3 pb-2 pt-1 text-[0.65rem] font-medium uppercase tracking-[0.18em] text-fd-muted-foreground">
                <span>Clipwell / picker</span>
                <span>Live product capture</span>
              </div>
              <div className="aspect-[718/640] overflow-hidden rounded-xl border border-fd-border [mask-image:linear-gradient(to_bottom,black_80%,transparent)]">
                <ThemedImage
                  name="picker"
                  alt="The Clipwell picker showing typed clipboard items, filter tabs, and an image thumbnail"
                  width={718}
                  height={847}
                  priority
                />
              </div>
            </div>
            <div className="absolute -bottom-3 left-5 rounded-full border border-fd-border bg-fd-card px-4 py-2 text-xs font-semibold shadow-lg sm:-left-8">
              Search the whole history
            </div>
            <div className="absolute -right-2 top-20 hidden rounded-full border border-fd-border bg-fd-card px-4 py-2 text-xs font-semibold shadow-lg sm:block">
              Light and dark
            </div>
          </div>
        </div>
      </section>

      <section className="mx-auto w-full max-w-5xl px-6 pb-16">
        <div className="grid items-center gap-6 rounded-2xl border border-fd-border bg-fd-card p-6 sm:p-8 md:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
          <div>
            <p className="text-xs font-medium uppercase tracking-widest text-fd-primary">
              Start locally
            </p>
            <h2 className="mt-2 text-xl font-semibold">
              One daemon, every client.
            </h2>
            <p className="mt-2 text-sm text-fd-muted-foreground">
              Start the API, then summon the native picker. The same history is
              available to the web UI, CLI, and MCP.
            </p>
          </div>
          <TerminalCard title="quickstart">
            <p>
              <span className="select-none pr-2 text-fd-muted-foreground">
                $
              </span>
              dotnet run --project daemon
            </p>
            <p>
              <span className="select-none pr-2 text-fd-muted-foreground">
                $
              </span>
              dotnet run --project ui
            </p>
          </TerminalCard>
        </div>
      </section>

      {/* Features */}
      <section className="mx-auto w-full max-w-5xl px-6 py-16">
        <SectionHeader
          eyebrow="Features"
          title="Fast to summon, easy to query"
        />
        <div className="mt-8 grid grid-cols-1 gap-4 md:grid-cols-6">
          {/* A — rich picker */}
          <Link
            href="/docs/scenarios"
            className={cn(
              "group relative flex flex-col overflow-hidden rounded-xl border border-fd-border bg-fd-card transition-colors hover:bg-fd-accent/40 md:col-span-4",
              focusRing,
            )}
          >
            <div className="p-6">
              <IconChip icon={AppWindow} />
              <h3 className="mt-4 text-lg font-semibold">A rich picker</h3>
              <p className="mt-2 text-sm text-fd-muted-foreground">
                Compact and Detail views, Quick Look, filters, grouping by date
                or source, rename, edit, and a Ctrl+K action palette.
              </p>
            </div>
            <ArrowUpRight className="absolute right-6 top-6 size-4 text-fd-muted-foreground transition-colors group-hover:text-fd-foreground" />
            <div className="relative mt-auto aspect-[16/10] overflow-hidden border-t border-fd-border bg-fd-secondary/50">
              <ThemedImage
                name="detail"
                alt="The Clipwell detail view showing a selected item with its metadata"
                width={1143}
                height={847}
                className="absolute left-6 top-6 w-full rounded-tl-lg border border-fd-border shadow-sm transition-transform duration-300 ease-out group-hover:scale-[1.02] motion-reduce:transition-none motion-reduce:group-hover:scale-100"
              />
            </div>
          </Link>

          {/* B — warm show latency */}
          <div className="flex flex-col rounded-xl border border-fd-border bg-fd-card p-6 md:col-span-2">
            <IconChip icon={Zap} />
            <p className="mt-4 font-mono text-5xl font-semibold tracking-tight tabular-nums">
              ~16<span className="text-2xl text-fd-muted-foreground"> ms</span>
            </p>
            <p className="text-sm text-fd-muted-foreground">
              hotkey to visible picker, measured warm
            </p>
            <p className="mt-2 text-sm text-fd-muted-foreground">
              The window is pre-warmed, so the global hotkey shows it in about
              one display frame. No cold start.
            </p>
          </div>

          {/* C — typed items */}
          <div className="flex flex-col rounded-xl border border-fd-border bg-fd-card p-6 md:col-span-2">
            <IconChip icon={Tags} />
            <h3 className="mt-4 text-lg font-semibold">Typed, with favicons</h3>
            <p className="mt-2 text-sm text-fd-muted-foreground">
              Every item is classified, with site favicons, image thumbnails,
              and the source app.
            </p>
            <div className="mt-4 flex flex-wrap gap-1.5">
              {itemKinds.map((kind) => (
                <Chip key={kind}>{kind}</Chip>
              ))}
            </div>
          </div>

          {/* D — queryable */}
          <div className="flex flex-col rounded-xl border border-fd-border bg-fd-card p-6 md:col-span-4">
            <IconChip icon={Plug} />
            <h3 className="mt-4 text-lg font-semibold">
              Queryable by anything
            </h3>
            <p className="mt-2 text-sm text-fd-muted-foreground">
              REST for one-shot calls, WebSocket and SSE to stream changes live,
              MCP so AI agents can read and act.
            </p>
            <div className="mt-4 overflow-x-auto rounded-lg border border-fd-border bg-fd-secondary/50 p-3 font-mono text-xs leading-5">
              <div>
                <span className="select-none pr-2 text-fd-muted-foreground">
                  $
                </span>
                curl 127.0.0.1:8787/api/clipboard?limit=1
              </div>
              <div className="mt-2 text-fd-muted-foreground">
                {'{ "items": [ {'}
              </div>
              <div className="pl-4 text-fd-muted-foreground">
                "kind": <span className="text-fd-foreground">"github-pr"</span>,
              </div>
              <div className="pl-4 text-fd-muted-foreground">
                "text":{" "}
                <span className="text-fd-foreground">"clipwell#42"</span>,
              </div>
              <div className="pl-4 text-fd-muted-foreground">
                "source": <span className="text-fd-foreground">"Chrome"</span>
              </div>
              <div className="text-fd-muted-foreground">{"} ] }"}</div>
            </div>
          </div>

          {/* E — platform row */}
          <div className="grid divide-y divide-fd-border rounded-xl border border-fd-border bg-fd-card sm:grid-cols-3 sm:divide-x sm:divide-y-0 md:col-span-6">
            {platforms.map((platform) => {
              const Icon = platform.icon;
              return (
                <div key={platform.title} className="p-6">
                  <div className="flex items-center gap-2">
                    <Icon
                      className="size-4 text-fd-muted-foreground"
                      strokeWidth={1.75}
                    />
                    <h3 className="font-semibold">{platform.title}</h3>
                  </div>
                  <p className="mt-2 text-sm text-fd-muted-foreground">
                    {platform.body}
                  </p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Protocols */}
      <section className="mx-auto w-full max-w-5xl px-6 py-16">
        <SectionHeader
          eyebrow="API"
          title="One history, three protocols"
          lede="The daemon owns the clipboard and its SQLite history. Anything on your machine can read it, listen to it, and drive it."
        />
        <div className="mt-8 grid divide-y divide-fd-border rounded-xl border border-fd-border bg-fd-card sm:grid-cols-3 sm:divide-x sm:divide-y-0">
          {protocols.map((protocol) => {
            const Icon = protocol.icon;
            return (
              <Link
                key={protocol.name}
                href="/docs/integrations"
                className={cn(
                  "group flex flex-col p-6 transition-colors hover:bg-fd-accent/40 first:rounded-t-xl last:rounded-b-xl sm:first:rounded-l-xl sm:first:rounded-tr-none sm:last:rounded-r-xl sm:last:rounded-bl-none",
                  focusRing,
                )}
              >
                <Icon
                  className="size-4 text-fd-muted-foreground"
                  strokeWidth={1.75}
                />
                <p className="mt-3 font-mono text-sm font-semibold">
                  {protocol.name}
                </p>
                <p className="mt-2 text-sm text-fd-muted-foreground">
                  {protocol.desc}
                </p>
                <div className="mt-4">
                  <Chip>{protocol.endpoint}</Chip>
                </div>
              </Link>
            );
          })}
        </div>
      </section>

      {/* CTA */}
      <section className="mx-auto mb-24 w-full max-w-5xl px-6">
        <div className="relative overflow-hidden rounded-2xl border border-fd-border bg-fd-card p-10 text-center">
          <div
            className="pointer-events-none absolute inset-0 -z-10 opacity-40"
            style={{ background: glow }}
          />
          <h2 className="text-2xl font-semibold">Built in the open</h2>
          <p className="mx-auto mt-3 max-w-xl text-fd-muted-foreground">
            Architecture, ADRs, and the technology behind the one-frame picker.
          </p>
          <div className="mt-6 flex flex-wrap justify-center gap-3">
            <Link href="/docs/install" className={buttonPrimary}>
              Install Clipwell
            </Link>
            <a
              href="/clipwell/engineering"
              className={cn(buttonSecondary, "group")}
            >
              Engineering docs
              <ArrowUpRight className="size-4 transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5 motion-reduce:transition-none motion-reduce:group-hover:translate-x-0 motion-reduce:group-hover:translate-y-0" />
            </a>
          </div>
        </div>
      </section>
    </main>
  );
}
