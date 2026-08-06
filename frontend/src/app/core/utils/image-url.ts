import { environment } from '../../../environments/environment';

// Room/hotel imageUrl values are server-relative paths (e.g. "/uploads/rooms/{id}.png"),
// served from the API's static-files root — not under /api, and not the Angular
// dev-server origin. This resolves them to an absolute URL an <img> can load.
//
// A re-upload overwrites the file at that same path, so the URL string never
// changes even though the bytes behind it did — the browser would otherwise keep
// showing the old cached image until a hard reload. Passing `version` (e.g. a
// RoomsService/HotelService imageVersion signal, bumped on every successful
// upload) appends a cache-busting query param so a fresh copy gets fetched.
export function resolveImageUrl(url: string | null | undefined, version?: number): string | null {
  if (!url) return null;
  const absolute = `${environment.apiOrigin}${url}`;
  return version ? `${absolute}?v=${version}` : absolute;
}
