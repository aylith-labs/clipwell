import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { Provider } from "@/components/provider";
import "./global.css";

const base = process.env.PAGES_BASE || "";
const origin = base.startsWith("/clipwell")
  ? "https://aylith-labs.github.io"
  : "https://clipwell.aylith.com";

export const metadata: Metadata = {
  title: "Clipwell — your clipboard, in focus",
  description:
    "Find a copy in a few keystrokes. Keep it local. One clipboard API for the native picker, web UI, CLI, and AI tools.",
  metadataBase: new URL(`${origin}${base}/`),
};

const inter = Inter({
  subsets: ["latin"],
});

export default function Layout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={inter.className} suppressHydrationWarning>
      <body className="flex flex-col min-h-screen">
        <Provider>{children}</Provider>
      </body>
    </html>
  );
}
