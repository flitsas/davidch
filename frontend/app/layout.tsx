import type { Metadata } from "next";
import { Poppins } from "next/font/google";
import "./globals.css";

const poppins = Poppins({
  variable: "--font-poppins",
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
  display: "swap",
});

export const metadata: Metadata = {
  title: "FLIT — Identidad",
  description: "FLIT 2.0 identity layer",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es" className={`${poppins.variable} h-full antialiased`}>
      <body className="min-h-full flex flex-col bg-flit-bg-app text-flit-text-primary">
        <a
          href="#main-content"
          className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded-flit-lg focus:bg-flit-bg-card focus:px-4 focus:py-2 focus:shadow-flit-card"
        >
          Saltar al contenido
        </a>
        {children}
      </body>
    </html>
  );
}
