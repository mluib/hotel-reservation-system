import { DecodedUser, Role } from '../models/auth.model';

// The backend builds its JWTs from a plain list of System.Security.Claims.Claim
// objects passed straight into a JwtSecurityToken constructor (see JwtTokenService),
// which does NOT go through ASP.NET Core's short-name outbound claim mapping. That
// means claim keys in the token payload are the full ClaimTypes URIs, not "role"/
// "name" — only the standard registered claims ("sub", "exp", ...) get short names.
// Confirmed against a real decoded token: Name/NameIdentifier sit under the xmlsoap
// schema, but ClaimTypes.Role specifically resolves to the microsoft.com schema below
// (a different base domain — easy to get wrong by assumption, so verified directly).
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const NAME_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const NAMEID_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

function base64UrlDecode(segment: string): string {
  const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  return atob(padded);
}

/** Decodes a JWT's payload without verifying its signature — verification already
 * happened server-side; this is purely for reading claims to drive the UI. */
export function decodeToken(token: string): DecodedUser | null {
  const parts = token.split('.');
  if (parts.length !== 3) return null;

  try {
    const payload = JSON.parse(base64UrlDecode(parts[1])) as Record<string, unknown>;

    const rawRoles = payload[ROLE_CLAIM] ?? payload['role'];
    const roles: Role[] = Array.isArray(rawRoles)
      ? (rawRoles as Role[])
      : rawRoles
        ? [rawRoles as Role]
        : [];

    const userId = (payload['sub'] as string) ?? (payload[NAMEID_CLAIM] as string) ?? '';
    const userName = (payload[NAME_CLAIM] as string) ?? (payload['name'] as string) ?? '';
    const exp = Number(payload['exp'] ?? 0);

    if (!userId) return null;

    return { userId, userName, roles, expiresAtEpochSeconds: exp };
  } catch {
    return null;
  }
}

export function isExpired(user: DecodedUser): boolean {
  if (!user.expiresAtEpochSeconds) return false;
  return Date.now() >= user.expiresAtEpochSeconds * 1000;
}
