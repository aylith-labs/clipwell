import type { ClipItem } from "../types";

/** Code-like kinds render their preview in the mono stack (matches the Avalonia picker). */
export function isMonoPreview(item: ClipItem): boolean {
  return item.kind === "code" || item.kind === "color" || item.kind === "path";
}

/** A parseable CSS color for color items, used for the inline swatch. */
export function colorSwatch(item: ClipItem): string | null {
  if (item.kind !== "color" || item.isSensitive) return null;
  const value = (item.textContent ?? "").trim();
  return CSS.supports("color", value) ? value : null;
}

function relativeTime(timestamp: string): string {
  const t = new Date(timestamp).getTime();
  if (Number.isNaN(t)) return timestamp;
  const delta = Date.now() - t;
  const min = 60_000;
  const hour = 60 * min;
  const day = 24 * hour;
  if (delta < min) return "just now";
  if (delta < hour) return `${Math.floor(delta / min)}m ago`;
  if (delta < day) return `${Math.floor(delta / hour)}h ago`;
  if (delta < 7 * day) return `${Math.floor(delta / day)}d ago`;
  return new Date(t).toISOString().slice(0, 10);
}

export interface MetaOptions {
  showSource: boolean;
  showTime: boolean;
  showChars: boolean;
}

export function meta(item: ClipItem, opts: MetaOptions): string {
  const parts: string[] = [];
  if (opts.showSource && item.sourceApp) parts.push(item.sourceApp);
  if (opts.showTime) parts.push(relativeTime(item.timestamp));
  if (opts.showChars && item.textLength > 0 && !item.hasImage && !item.isSensitive)
    parts.push(item.textLength === 1 ? "1 char" : `${item.textLength.toLocaleString()} chars`);
  if (item.imageWidth != null && item.imageHeight != null)
    parts.push(`${item.imageWidth}×${item.imageHeight}`);
  return parts.join(" · ");
}

/** Masked preview for sensitive items; alias wins; otherwise one-lined text. */
export function preview(item: ClipItem): string {
  if (item.isSensitive) return "•••••••••••  (sensitive)";
  if (item.alias) return item.alias;
  const text = item.textContent ?? "";
  if (!text) return item.hasImage ? "image" : "(empty)";
  const oneLine = text.replace(/\s+/g, " ").trim();
  return oneLine.length > 200 ? `${oneLine.slice(0, 200)}…` : oneLine;
}

export function fullText(item: ClipItem): string {
  if (item.isSensitive) return "•••••••••••  (sensitive)";
  return item.textContent ?? "";
}

/** Site favicon for url/github-pr items (HTTPS only, no third-party service). */
export function faviconUrl(item: ClipItem): string | null {
  if (item.kind !== "url" && item.kind !== "github-pr") return null;
  try {
    const u = new URL((item.textContent ?? "").trim());
    if (u.protocol !== "https:") return null;
    return `https://${u.host}/favicon.ico`;
  } catch {
    return null;
  }
}

export function dateBucket(timestamp: string): string {
  const d = new Date(timestamp);
  if (Number.isNaN(d.getTime())) return "Earlier";
  const day = new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
  const now = new Date();
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
  const oneDay = 86_400_000;
  if (day === today) return "Today";
  if (day === today - oneDay) return "Yesterday";
  if (today - day < 7 * oneDay) return "Previous 7 days";
  if (today - day < 30 * oneDay) return "Previous 30 days";
  return "Earlier";
}

export function sourceBucket(item: ClipItem): string {
  return item.sourceApp || "Unknown source";
}
