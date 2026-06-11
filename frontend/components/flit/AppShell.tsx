import type { ReactNode } from "react";
import Link from "next/link";

export type NavItem = {
  href: string;
  label: string;
  icon?: ReactNode;
};

export function AppShell({
  children,
  email,
  nav,
  headerActions,
}: {
  children: ReactNode;
  email: string;
  nav: NavItem[];
  headerActions?: ReactNode;
}) {
  return (
    <div className="flex min-h-screen bg-flit-bg-app">
      <aside
        className="sticky top-0 flex h-screen w-[138px] shrink-0 flex-col items-center gap-6 py-8 flit-gradient-sidebar"
        style={{ borderTopRightRadius: "var(--flit-radius-sidebar)", borderBottomRightRadius: "var(--flit-radius-sidebar)" }}
        aria-label="Navegación principal"
      >
        <Link
          href="/"
          className="text-xl font-bold text-flit-text-inverse flit-focus-ring rounded-flit-md px-2"
        >
          FLIT
        </Link>
        <nav className="flex w-full flex-col items-center gap-2 px-3">
          {nav.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="flit-focus-ring flex w-full flex-col items-center gap-1 rounded-flit-md px-2 py-3 text-center text-xs font-medium text-flit-text-inverse/90 transition-colors duration-[var(--flit-duration-fast)] hover:bg-white/15 hover:text-flit-text-inverse"
            >
              {item.icon ?? (
                <span
                  className="flex h-8 w-8 items-center justify-center rounded-full bg-white/20 text-sm"
                  aria-hidden="true"
                >
                  {item.label.charAt(0)}
                </span>
              )}
              <span>{item.label}</span>
            </Link>
          ))}
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-end gap-4 border-b border-flit-border-soft bg-flit-bg-card/80 px-6 py-4 backdrop-blur-sm">
          <span className="hidden text-sm text-flit-text-secondary sm:inline">
            Identidad
          </span>
          <span className="text-sm font-medium text-flit-text-primary">{email}</span>
          {headerActions}
        </header>
        <main id="main-content" className="flex-1 p-6 lg:p-8">
          <div className="mx-auto max-w-5xl space-y-6">{children}</div>
        </main>
      </div>
    </div>
  );
}
