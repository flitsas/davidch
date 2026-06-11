export type IntegrationMode = "Dashboard" | "Qx";

export type IntegrationConfig = {
  integration_mode: IntegrationMode;
};

export function integrationModeLabel(mode: IntegrationMode): string {
  return mode === "Qx" ? "Quipux (QX)" : "Dashboard FLIT";
}
