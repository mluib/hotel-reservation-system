// Local dev talks to the backend's plain-HTTP launch profile (see backend's
// launchSettings.json, port 5044) rather than its HTTPS one. That sidesteps
// both trusting the ASP.NET Core dev-certificate in the browser and the
// http->https redirect interacting awkwardly with CORS preflight/redirects.
export const environment = {
  production: false,
  apiOrigin: 'http://localhost:5044',
  apiBaseUrl: 'http://localhost:5044/api',
};
