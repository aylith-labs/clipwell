import Image from "next/image";
import { cn } from "@/lib/cn";
import { MEDIA_BASE } from "@/lib/media";

/**
 * A bare light/dark screenshot pair for landing layouts: the `-light` variant
 * shows in light mode, the `-dark` variant in dark mode. Unlike `<ThemedShot>`
 * (used by MDX), this adds no `<figure>` wrapper or default margins, so it drops
 * cleanly into custom frames and media wells. `className` merges onto each `<img>`.
 *
 * Both variants must share intrinsic dimensions so the theme swap is CLS-free.
 * Media lives on `media.aylith.com` (see `@/lib/media`); static export uses the
 * absolute remote URL directly via `images.unoptimized`.
 */
export function ThemedImage({
  name,
  alt,
  width,
  height,
  priority = false,
  className,
}: {
  name: string;
  alt: string;
  width: number;
  height: number;
  priority?: boolean;
  className?: string;
}) {
  return (
    <>
      <Image
        src={`${MEDIA_BASE}/${name}-light.png`}
        alt={alt}
        width={width}
        height={height}
        priority={priority}
        className={cn("block h-auto w-full dark:hidden", className)}
      />
      <Image
        src={`${MEDIA_BASE}/${name}-dark.png`}
        alt={alt}
        width={width}
        height={height}
        priority={priority}
        className={cn("hidden h-auto w-full dark:block", className)}
      />
    </>
  );
}
