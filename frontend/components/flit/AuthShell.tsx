import type { ReactNode } from "react";
import Link from "next/link";

export function AuthShell({
  title,
  subtitle,
  children,
  footer,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <div className="flex min-h-screen">
      <aside
        className="relative hidden w-[42%] flex-col justify-between overflow-hidden p-12 lg:flex"
        aria-hidden="true"
      >
        <div className="absolute inset-0 flit-gradient-sidebar" />
        <div className="absolute -right-24 top-1/4 h-64 w-64 rounded-full bg-white/10 blur-3xl" />
        <div className="absolute -left-16 bottom-1/4 h-48 w-48 rounded-full bg-flit-cyan/20 blur-2xl" />
        <div className="relative z-10">
          <Link href="/" className="text-3xl font-bold tracking-tight text-flit-text-inverse">
            FLIT
          </Link>
          <p className="mt-4 max-w-xs text-pretty text-lg font-medium text-flit-text-inverse/90">
            Plataforma de gestión vehicular y trámites
          </p>
        </div>
        <p className="relative z-10 text-sm text-flit-text-inverse/70">Identidad y acceso seguro</p>
      </aside>

      <main
        id="main-content"
        className="flex flex-1 flex-col items-center justify-center px-6 py-12 sm:px-10"
      >
        <div className="mb-8 w-full max-w-md lg:hidden">
          <Link href="/" className="text-2xl font-bold text-flit-text-brand">
            FLIT
          </Link>
        </div>
        <div className="w-full max-w-md rounded-flit-xl border border-flit-border-soft bg-flit-bg-card p-8 shadow-flit-card">
          <header className="mb-8">
            <h1 className="text-pretty text-2xl font-bold text-flit-text-brand">{title}</h1>
            {subtitle && <p className="mt-2 text-sm text-flit-text-secondary">{subtitle}</p>}
          </header>
          {children}
          {footer && <div className="mt-6 text-center text-sm">{footer}</div>}
        </div>
      </main>
    </div>
  );
}
