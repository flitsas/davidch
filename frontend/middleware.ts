import { NextRequest, NextResponse } from "next/server";
import { ACCESS_COOKIE, REFRESH_COOKIE } from "./lib/auth/cookies";

const PUBLIC = ["/login", "/activate", "/forgot-password", "/reset-password"];

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5080";

export async function middleware(req: NextRequest) {
  const { pathname } = req.nextUrl;
  if (PUBLIC.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  const access = req.cookies.get(ACCESS_COOKIE);
  if (access) return NextResponse.next();

  const refresh = req.cookies.get(REFRESH_COOKIE);
  if (!refresh) {
    return NextResponse.redirect(new URL("/login", req.url));
  }

  const refreshRes = await fetch(`${API_BASE}/api/auth/refresh`, {
    method: "POST",
    headers: { Cookie: `${REFRESH_COOKIE}=${refresh.value}` },
  });

  if (!refreshRes.ok) {
    return NextResponse.redirect(new URL("/login", req.url));
  }

  const response = NextResponse.next();
  for (const cookie of refreshRes.headers.getSetCookie?.() ?? []) {
    response.headers.append("Set-Cookie", cookie);
  }
  return response;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|api).*)"],
};
