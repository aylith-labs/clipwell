import { Provider } from "@/components/provider";
import "../../../assets/fonts/inter.css";
import "./global.css";

export default function Layout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className="clipwell-inter" suppressHydrationWarning>
      <body className="flex flex-col min-h-screen">
        <Provider>{children}</Provider>
      </body>
    </html>
  );
}
