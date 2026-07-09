import { ArrowRight, ArrowUpRight } from 'lucide-react';
import Link from 'next/link';
import { cn } from '@/lib/cn';

const glow =
  'radial-gradient(60% 50% at 50% 0%, color-mix(in oklab, var(--color-fd-primary) 22%, transparent), transparent)';

const focusRing =
  'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-fd-primary focus-visible:ring-offset-2 focus-visible:ring-offset-fd-background';

const buttonBase = 'inline-flex h-11 items-center gap-2 rounded-lg px-5 text-sm font-medium';
const buttonPrimary = cn(
  buttonBase,
  'bg-fd-primary text-fd-primary-foreground transition-colors hover:bg-fd-primary/90',
  focusRing,
);
const buttonSecondary = cn(
  buttonBase,
  'border border-fd-border bg-fd-card transition-colors hover:bg-fd-accent',
  focusRing,
);

const quickLinks = [
  {
    href: '/docs/architecture',
    title: 'Architecture',
    desc: 'The daemon, the watchers, and how thin clients speak one API.',
  },
  {
    href: '/docs/tech-stack',
    title: 'Tech stack',
    desc: '.NET 10, Avalonia, SQLite, and the web UI toolchain.',
  },
  {
    href: '/docs/plugins',
    title: 'Plugin host',
    desc: 'Detectors and Ctrl+K actions loaded from external assemblies.',
  },
  {
    href: '/docs/adr/0001-daemon-plus-thin-client-on-dotnet',
    title: 'Decision records',
    desc: 'Every architectural call, written down as an ADR at decision time.',
  },
];

export default function HomePage() {
  return (
    <main className="flex flex-1 flex-col">
      <section className="relative flex flex-col items-center px-6 pt-24 pb-16 text-center">
        <div
          className="pointer-events-none absolute inset-0 -z-10 opacity-60"
          style={{ background: glow }}
        />
        <span className="inline-flex items-center gap-2 rounded-full border border-fd-border bg-fd-card px-3 py-1 text-xs font-medium text-fd-muted-foreground">
          <span className="size-1.5 rounded-full bg-fd-primary" />
          The Clipwell engineering notebook
        </span>
        <h1 className="mt-6 max-w-2xl text-balance text-4xl font-semibold tracking-tight sm:text-5xl">
          How Clipwell is built
        </h1>
        <p className="mt-6 max-w-xl text-balance text-lg text-fd-muted-foreground">
          Architecture, ADRs, and the decisions behind the daemon, the picker, and the web UI.
          Written as it was built.
        </p>
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <Link href="/docs" className={cn(buttonPrimary, 'group')}>
            Read the docs
            <ArrowRight className="size-4 transition-transform group-hover:translate-x-0.5 motion-reduce:transition-none motion-reduce:group-hover:translate-x-0" />
          </Link>
          <a href="/clipwell" className={buttonSecondary}>
            Product docs
          </a>
        </div>
      </section>

      <section className="mx-auto mb-24 w-full max-w-5xl px-6">
        <div className="grid divide-y divide-fd-border rounded-xl border border-fd-border bg-fd-card sm:grid-cols-2 sm:divide-x lg:grid-cols-4 lg:divide-y-0">
          {quickLinks.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className={cn(
                'group flex flex-col p-6 transition-colors hover:bg-fd-accent/40',
                focusRing,
              )}
            >
              <div className="flex items-center justify-between gap-2">
                <span className="font-semibold">{link.title}</span>
                <ArrowUpRight className="size-4 text-fd-muted-foreground transition-colors group-hover:text-fd-foreground" />
              </div>
              <p className="mt-2 text-sm text-fd-muted-foreground">{link.desc}</p>
            </Link>
          ))}
        </div>
      </section>
    </main>
  );
}
