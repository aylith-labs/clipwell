import { createMDX } from "fumadocs-mdx/next";

const withMDX = createMDX();

// "/" selects the custom-domain root; "/clipwell" retains project Pages mode.
const pagesBase = process.env.PAGES_BASE || "";
const base = pagesBase === "/" ? "" : pagesBase;

/** @type {import('next').NextConfig} */
const config = {
  output: "export",
  // TypeScript 7 is the native Go compiler and exposes no JS compiler API,
  // so Next.js has to type-check by shelling out to tsc.
  experimental: { useTypeScriptCli: true },
  reactStrictMode: true,
  basePath: base || undefined,
  images: { unoptimized: true },
  // Exposed to client components (e.g. <ThemedClip>) so raw asset URLs like
  // <video src> get the Pages subpath prefix that next/image applies automatically.
  env: { NEXT_PUBLIC_PAGES_BASE: base },
};

export default withMDX(config);
