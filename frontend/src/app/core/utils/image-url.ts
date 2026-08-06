import { environment } from '../../../environments/environment';

// Room/hotel imageUrl values are server-relative paths (e.g. "/uploads/rooms/{id}.png"),
// served from the API's static-files root — not under /api, and not the Angular
// dev-server origin. This resolves them to an absolute URL an <img> can load.
export function resolveImageUrl(url: string | null | undefined): string | null {
  return url ? `${environment.apiOrigin}${url}` : null;
}
