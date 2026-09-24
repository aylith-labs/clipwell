"use client";

import { Play, RotateCcw } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { ThemedImage } from "@/components/landing/themed-image";
import { MEDIA_BASE } from "@/lib/media";

/** A real product capture with a theme-matched, annotated cover. */
export function FeatureFilm() {
  const [playing, setPlaying] = useState(false);
  const light = useRef<HTMLVideoElement>(null);
  const dark = useRef<HTMLVideoElement>(null);

  useEffect(() => {
    let wasDark = document.documentElement.classList.contains("dark");
    const observer = new MutationObserver(() => {
      const isDark = document.documentElement.classList.contains("dark");
      if (isDark === wasDark) return;
      wasDark = isDark;
      light.current?.pause();
      dark.current?.pause();
      setPlaying(false);
    });
    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ["class"],
    });
    return () => observer.disconnect();
  }, []);

  async function play() {
    setPlaying(true);
    const active = document.documentElement.classList.contains("dark")
      ? dark.current
      : light.current;
    try {
      await active?.play();
    } catch {
      // Native video controls remain available when autoplay is denied.
    }
  }

  function replay() {
    for (const video of [light.current, dark.current]) {
      if (video) video.currentTime = 0;
    }
    void play();
  }

  return (
    <div className="relative overflow-hidden rounded-[1.5rem] border border-fd-border bg-fd-card shadow-2xl shadow-fd-primary/10">
      <div className="flex items-center justify-between border-b border-fd-border px-5 py-3 text-xs text-fd-muted-foreground">
        <span className="font-mono uppercase tracking-[0.2em]">
          Clipwell / field notes
        </span>
        <span>01: The picker</span>
      </div>
      <div className="relative aspect-[16/10] overflow-hidden bg-fd-secondary">
        {(["light", "dark"] as const).map((theme) => (
          <video
            key={theme}
            ref={theme === "light" ? light : dark}
            className={`absolute inset-0 size-full object-cover ${theme === "light" ? "block dark:hidden" : "hidden dark:block"}`}
            poster={`${MEDIA_BASE}/picker-${theme}.png`}
            controls={playing}
            playsInline
            preload="none"
            onEnded={() => setPlaying(false)}
            aria-label={`Clipwell picker demo in ${theme} theme`}
          >
            <source
              src={`${MEDIA_BASE}/usage-${theme}.webm`}
              type="video/webm"
            />
            <track
              kind="captions"
              src={`${process.env.NEXT_PUBLIC_PAGES_BASE ?? ""}/usage-description.vtt`}
              srcLang="en"
              label="English description"
            />
            Your browser does not support WebM video.
          </video>
        ))}
        {!playing && (
          <div className="absolute inset-0 overflow-hidden bg-fd-background">
            <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_65%_30%,color-mix(in_oklab,var(--color-fd-primary)_22%,transparent),transparent_62%)]" />
            <div className="absolute inset-x-[12%] top-[13%] rotate-[-4deg] overflow-hidden rounded-xl border border-fd-border bg-fd-card shadow-2xl sm:inset-x-[20%] sm:top-[10%]">
              <ThemedImage
                name="picker"
                alt="Clipwell picker with typed clipboard items and filter controls"
                width={718}
                height={847}
              />
            </div>
            <div className="absolute inset-x-0 bottom-0 h-3/5 bg-gradient-to-t from-fd-background via-fd-background/90 to-transparent" />
            <div className="absolute bottom-5 left-5 right-5 flex items-end justify-between gap-3 sm:bottom-8 sm:left-8 sm:right-8">
              <div>
                <p className="text-[0.65rem] font-semibold uppercase tracking-[0.2em] text-fd-primary">
                  Product walkthrough
                </p>
                <p className="mt-1 max-w-sm text-balance text-xl font-semibold tracking-tight sm:text-3xl">
                  Find the right copy, without losing your flow.
                </p>
                <p className="mt-2 hidden text-sm text-fd-muted-foreground sm:block">
                  A real, short capture of filtering the picker. Light and dark
                  versions.
                </p>
              </div>
              <button
                type="button"
                onClick={() => void play()}
                className="flex size-12 shrink-0 items-center justify-center rounded-full bg-fd-primary text-fd-primary-foreground shadow-lg transition-transform hover:scale-105 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-fd-primary motion-reduce:transition-none sm:size-16"
                aria-label="Play Clipwell picker walkthrough"
              >
                <Play className="size-5 fill-current sm:size-6" />
              </button>
            </div>
          </div>
        )}
      </div>
      <div className="flex items-center justify-between gap-4 px-5 py-3 text-xs text-fd-muted-foreground">
        <span>Real product capture · silent demo</span>
        <button
          type="button"
          onClick={replay}
          className="inline-flex items-center gap-1.5 rounded px-2 py-1 hover:text-fd-foreground focus-visible:outline-2 focus-visible:outline-fd-primary"
        >
          <RotateCcw className="size-3" /> Replay
        </button>
      </div>
    </div>
  );
}
