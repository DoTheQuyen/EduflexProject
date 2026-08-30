import { Injectable } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { environment } from '../environments/environment';
import { AuthHelperService } from './auth-helper.service';

/**
 * Resolves the documentation topic for the screen the user is currently on, and the
 * manual it should be read from.
 *
 * There are two published manuals, built from one shared source by docs-site/build.mjs:
 *
 *   docsStudentBaseUrl  the student manual  - the student subset only
 *   docsBaseUrl         the staff manual    - every topic, student pages included
 *
 * They are separate sites rather than one site with a hidden section, because a
 * static site serves every page it contains: anything present in the student build
 * would be reachable by URL and by its own search box regardless of the menu. Sending
 * students to a build that does not contain the staff topics is what actually scopes
 * them. This is scoping, not access control - neither site is behind a login, so a
 * student who is given a staff URL can still open it. Put auth in front of the staff
 * site if that matters.
 *
 * A route opts in to a topic by declaring
 * `data: { helpKey: 'user/forms/submit-a-form-request' }`. The key is the docs-site
 * file path minus its `.md` extension, and is identical in both manuals - the student
 * build preserves the same folder layout - so one key works whichever manual it opens.
 * No help content is stored in, or served by, this application.
 *
 * Both manuals are plain static output; keeping them out of this app is what lets them
 * be built, reviewed and deployed as documentation rather than as code.
 */
@Injectable({ providedIn: 'root' })
export class HelpService {
  // Where a user lands when the route they are on declares no helpKey. Students go to
  // the root of the student manual: their build has no `user/` overview page, because
  // student-home.md replaces it there.
  private readonly studentHomeKey = '';
  private readonly staffSectionKey = 'staff';
  private readonly adminSectionKey = 'admin';

  constructor(
    private router: Router,
    private authHelper: AuthHelperService
  ) {}

  /**
   * The deepest `helpKey` declared on the active route chain, so a child route's topic
   * wins over its parent's. Falls back to the landing page of the section matching the
   * signed-in user's role when no route on the chain declares one.
   */
  resolveHelpKey(): string {
    let activeRoute: ActivatedRoute | null = this.router.routerState.root;
    let deepestHelpKey = '';

    while (activeRoute) {
      const routeHelpKey = activeRoute.snapshot.data['helpKey'];
      if (routeHelpKey) {
        deepestHelpKey = routeHelpKey;
      }
      activeRoute = activeRoute.firstChild;
    }

    return deepestHelpKey || this.defaultSectionKey();
  }

  /**
   * URL of a topic on whichever manual the signed-in user is entitled to.
   *
   * The manuals are built with VitePress `cleanUrls: false`, so every topic is a real
   * `.html` file. That means no rewrite rules are needed on whatever serves them.
   */
  buildTopicUrl(helpKey: string): string {
    const trimmedBaseUrl = this.manualBaseUrl().replace(/\/+$/, '');
    const trimmedHelpKey = helpKey.replace(/^\/+|\/+$/g, '');
    return trimmedHelpKey ? `${trimmedBaseUrl}/${trimmedHelpKey}.html` : `${trimmedBaseUrl}/`;
  }

  /** Opens the current screen's topic in a new tab. */
  openCurrentTopic(): void {
    if (typeof window === 'undefined') {
      return; // Server-side render pass - there is no tab to open.
    }
    window.open(this.buildTopicUrl(this.resolveHelpKey()), '_blank', 'noopener');
  }

  /** True when the signed-in user reads the student manual rather than the staff one. */
  private readsStudentManual(): boolean {
    const currentRole = this.authHelper.getCurrentUser()?.role;
    return currentRole === 'Student' || currentRole === 'Customer';
  }

  private manualBaseUrl(): string {
    return this.readsStudentManual() ? environment.docsStudentBaseUrl : environment.docsBaseUrl;
  }

  private defaultSectionKey(): string {
    if (this.readsStudentManual()) {
      return this.studentHomeKey;
    }

    return this.authHelper.getCurrentUser()?.role === 'Admin'
      ? this.adminSectionKey
      : this.staffSectionKey;
  }
}
