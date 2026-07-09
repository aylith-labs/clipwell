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
      <section className="relative flex flex-col items-center px-6 pt-24 pb-16 text-center">
        <div
          className="pointer-events-none absolute inset-0 -z-10 opacity-60"
          style={{ background: glow }}
        />
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
        <h1 className="mt-6 max-w-3xl text-balance text-5xl font-semibold tracking-tight sm:text-6xl md:text-7xl">
          Your clipboard,
          <br />
          as an API.
        </h1>
        <p className="mt-6 max-w-xl text-balance text-lg text-fd-muted-foreground">
          A cross-platform clipboard history built as a headless daemon with a
          thin picker on top. The native picker, the web UI, the CLI, and AI
          agents are all clients of the same public API.
        </p>
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <Link href="/docs" className={cn(buttonPrimary, "group")}>
            Read the docs
            <ArrowRight className="size-4 transition-transform group-hover:translate-x-0.5 motion-reduce:transition-none motion-reduce:group-hover:translate-x-0" />
          </Link>
          <a href={githubUrl} className={buttonSecondary}>
            View on GitHub
          </a>
        </div>
        <div className="mt-10 w-full max-w-xl">
          <TerminalCard title="quickstart">
            <p>
              <span className="select-none pr-2 text-fd-muted-foreground">
                $
              </span>
              dotnet run --project daemon
              <span className="pl-2 text-fd-muted-foreground">
                # clipboard API on :8787
              </span>
            </p>
            <p>
              <span className="select-none pr-2 text-fd-muted-foreground">
                $
              </span>
              dotnet run --project ui
              <span className="pl-2 text-fd-muted-foreground">
                # Alt+Shift+V to summon
              </span>
            </p>
          </TerminalCard>
        </div>
        <div className="relative mt-10 w-full max-w-2xl rounded-2xl border border-fd-border bg-fd-card/60 p-2 shadow-xl">
          <div className="aspect-[718/560] overflow-hidden rounded-xl [mask-image:linear-gradient(to_bottom,black_70%,transparent)]">
            <ThemedImage
              name="picker"
              alt="The Clipwell picker showing typed clipboard items, filter tabs, and an image thumbnail"
              width={718}
              height={847}
              priority
            />
          </div>
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
