export async function fetchCompanyConfig<T>(companyId: string, path: string): Promise<T | null> {
  const res = await fetch(`/api/v1/admin/companies/${companyId}/config/${path}`, {
    credentials: "include",
  });
  if (!res.ok) return null;
  return res.json() as Promise<T>;
}

export async function saveCompanyConfig(
  companyId: string,
  path: string,
  body: unknown,
): Promise<boolean> {
  const res = await fetch(`/api/v1/admin/companies/${companyId}/config/${path}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    credentials: "include",
    body: JSON.stringify(body),
  });
  return res.ok;
}
