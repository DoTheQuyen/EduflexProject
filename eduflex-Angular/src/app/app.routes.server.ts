import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // Authenticated, per-record detail pages — content is dynamic and login-gated, so it
  // can't be prerendered at build time (there's no fixed set of ids to enumerate). This
  // build's outputMode is 'static' (no deployed server), so RenderMode.Server isn't an
  // option either — these render fully client-side instead.
  {
    path: 'student-portal/application/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'staff-portal/enquiries/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'staff-portal/education-partners/:id/edit',
    renderMode: RenderMode.Client
  },
  {
    path: 'staff-portal/business-partners/:id/edit',
    renderMode: RenderMode.Client
  },
  {
    path: 'staff-portal/applications/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'staff-portal/enrolments/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'staff-portal/financial-records/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'staff-portal/dynamic-forms/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'staff-portal/email-templates/:id',
    renderMode: RenderMode.Client
  },
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
