import type { NavItem } from "@/components/flit/AppShell";
import { hasPermission, type MeResponse } from "@/lib/auth/session";

export function buildAdminNav(session: MeResponse): NavItem[] {
  const items: NavItem[] = [{ href: "/", label: "Inicio" }];

  if (hasPermission(session, "tramites:read")) {
    items.push({ href: "/tramites", label: "Trámites" });
  }

  if (hasPermission(session, "users:read")) {
    items.push({ href: "/tramites/dashboard", label: "Dashboard" });
  }

  if (hasPermission(session, "users:read")) {
    items.push({ href: "/admin/users", label: "Usuarios" });
  }
  if (hasPermission(session, "roles:read")) {
    items.push({ href: "/admin/roles", label: "Roles" });
  }
  if (session.isSuperAdmin) {
    items.push({ href: "/admin/companies", label: "Compañías" });
    items.push({ href: "/admin/ot", label: "Organismos de tránsito" });
    items.push({ href: "/admin/procedure-types", label: "Tipos de trámite" });
  }

  if (hasPermission(session, "tramites:update") && !session.isSuperAdmin) {
    items.push({ href: "/ot/settings/integracion", label: "Configuración OT" });
    items.push({ href: "/ot/settings/documentos", label: "Documentos OT" });
  }

  return items;
}
