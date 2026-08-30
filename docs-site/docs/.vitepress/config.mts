import { defineConfig } from 'vitepress'

/**
 * One config, two manuals.
 *
 *   DOCS_TARGET=staff    (default)  every topic          -> ../site
 *   DOCS_TARGET=student             the student subset   -> ../site-student
 *
 * Separate builds rather than one site with a hidden section: a static site serves
 * every page it contains, so a staff page merely left out of the student sidebar
 * would still be reachable by URL and by the student site's own search. The only
 * thing that actually scopes a student is the page not being in their build —
 * which is what `srcExclude` below does.
 *
 * build.mjs runs this twice in separate processes so the config is re-evaluated
 * with a different DOCS_TARGET each time.
 */
const isStudent = process.env.DOCS_TARGET === 'student'

// Never reaches the student build. Everything else under docs/ does.
const studentExclusions = [
  'staff/**',
  'admin/**',
  'reference/permission-matrix.md',
  'reference/glossary.md', // replaced by glossary-student.md, which drops the staff-only terms
  'reference/statuses.md', // replaced by statuses-student.md — the shared page also covers
                           // enquiry, task, template and role-type statuses, none of which
                           // a student ever sees
  'index.md',              // replaced by student-home.md
  'user/index.md'          // the "Student guide" overview, which only makes sense inside the staff manual
]

// Pages that stand in for a shared one in the student build. Both are rewritten onto
// the shared page's path so existing links keep resolving in either manual.
const studentRewrites = {
  'student-home.md': 'index.md',
  'reference/glossary-student.md': 'reference/glossary.md',
  'reference/statuses-student.md': 'reference/statuses.md'
}

const staffSidebar = [
  {
    text: 'Student guide',
    collapsed: false,
    items: [
      { text: 'Overview', link: '/user/' },
      { text: 'Sign in', link: '/user/getting-started/sign-in' },
      { text: 'Portal tour', link: '/user/getting-started/portal-tour' },
      { text: 'Key concepts', link: '/user/getting-started/key-concepts' },
      { text: 'Your dashboard', link: '/user/dashboard' },
      { text: 'Create an application', link: '/user/applications/create-an-application' },
      { text: 'Track application status', link: '/user/applications/track-application-status' },
      { text: 'Submit a form request', link: '/user/forms/submit-a-form-request' },
      { text: 'Complete your profile', link: '/user/profile/complete-your-profile' },
      { text: 'Troubleshooting', link: '/user/troubleshooting' }
    ]
  },
  {
    text: 'Staff guide',
    collapsed: false,
    items: [
      { text: 'Overview', link: '/staff/' },
      { text: 'Your dashboard', link: '/staff/dashboard' },
      { text: 'Working with lists', link: '/staff/lists' },
      { text: 'Manage enquiries', link: '/staff/enquiries/manage-enquiries' },
      { text: 'Add a student', link: '/staff/students/add-a-student' },
      { text: 'View a student record', link: '/staff/students/view-a-student' },
      { text: 'Review an application', link: '/staff/applications/review-an-application' },
      { text: 'Create an enrolment', link: '/staff/enrolments/create-an-enrolment' },
      { text: 'Work a migration case', link: '/staff/migration-cases/work-a-migration-case' },
      { text: 'Manage tasks', link: '/staff/tasks/manage-tasks' },
      { text: 'Manage partners', link: '/staff/partners/manage-partners' },
      { text: 'Troubleshooting', link: '/staff/troubleshooting' }
    ]
  },
  {
    text: 'Record tabs',
    collapsed: false,
    items: [
      { text: 'Documents tab', link: '/staff/record-tabs/documents' },
      { text: 'Forms tab', link: '/staff/record-tabs/forms' },
      { text: 'Communication tab', link: '/staff/record-tabs/communication' },
      { text: 'Audit Trail tab', link: '/staff/record-tabs/activity-log' },
      { text: 'Tasks tab', link: '/staff/record-tabs/tasks' }
    ]
  },
  {
    text: 'Finance & marketing',
    collapsed: false,
    items: [
      { text: 'Record a commission', link: '/staff/finance/record-a-commission' },
      { text: 'Accounts and the timeline', link: '/staff/finance/accounts' },
      { text: 'Send a custom invoice', link: '/staff/finance/send-a-custom-invoice' },
      { text: 'Manage course promotions', link: '/staff/marketing/manage-course-promotions' },
      { text: 'Manage student feedback', link: '/staff/marketing/moderate-feedback' }
    ]
  },
  {
    text: 'Admin guide',
    collapsed: false,
    items: [
      { text: 'Overview', link: '/admin/' },
      { text: 'Add a user', link: '/admin/users/add-a-user' },
      { text: 'Assign permissions', link: '/admin/users/assign-permissions' },
      { text: 'Create a role', link: '/admin/roles/create-a-role' },
      { text: 'Manage departments', link: '/admin/departments/manage-departments' },
      { text: 'Create a form template', link: '/admin/templates/create-a-form-template' },
      { text: 'Configure a visa process template', link: '/admin/templates/configure-a-visa-process-template' },
      { text: 'Manage practitioner tags', link: '/admin/templates/manage-practitioner-tags' },
      { text: 'Manage email templates', link: '/admin/templates/manage-email-templates' },
      { text: 'Manage invoice templates', link: '/admin/templates/manage-invoice-templates' },
      { text: 'App settings', link: '/admin/settings/app-settings' },
      { text: 'Troubleshooting', link: '/admin/troubleshooting' }
    ]
  },
  {
    text: 'Reference',
    collapsed: false,
    items: [
      { text: 'Statuses', link: '/reference/statuses' },
      { text: 'Permission matrix', link: '/reference/permission-matrix' },
      { text: 'Glossary', link: '/reference/glossary' },
      { text: 'Release notes', link: '/release-notes' }
    ]
  }
]

const studentSidebar = [
  {
    text: 'Getting started',
    collapsed: false,
    items: [
      { text: 'Sign in', link: '/user/getting-started/sign-in' },
      { text: 'Portal tour', link: '/user/getting-started/portal-tour' },
      { text: 'Key concepts', link: '/user/getting-started/key-concepts' },
      { text: 'Your dashboard', link: '/user/dashboard' }
    ]
  },
  {
    text: 'Applications',
    collapsed: false,
    items: [
      { text: 'Create an application', link: '/user/applications/create-an-application' },
      { text: 'Track application status', link: '/user/applications/track-application-status' }
    ]
  },
  {
    text: 'Forms and profile',
    collapsed: false,
    items: [
      { text: 'Submit a form request', link: '/user/forms/submit-a-form-request' },
      { text: 'Complete your profile', link: '/user/profile/complete-your-profile' }
    ]
  },
  {
    text: 'Help',
    collapsed: false,
    items: [
      { text: 'Troubleshooting', link: '/user/troubleshooting' },
      { text: 'Statuses', link: '/reference/statuses' },
      { text: 'Glossary', link: '/reference/glossary' },
      { text: 'Release notes', link: '/release-notes' }
    ]
  }
]

export default defineConfig({
  lang: 'en',
  title: isStudent ? 'Eduflex Student Help' : 'Eduflex Help',
  description: isStudent
    ? 'How to apply, complete forms and track your application in Eduflex.'
    : 'User, staff and administrator guides for Eduflex.',

  outDir: isStudent ? '../site-student' : '../site',

  // The manuals are served from a sub-path in production (/help/staff, /help/student),
  // so asset URLs have to be prefixed. CI sets DOCS_BASE; locally it stays at the root.
  base: process.env.DOCS_BASE || '/',

  vite: {
    build: {
      // Emit every wireframe as a real file. Vite inlines assets under 4 kB as data
      // URIs by default, which bloats the HTML, defeats caching, and stops them being
      // addressable — several of the wireframes fall under that threshold.
      assetsInlineLimit: 0
    }
  },

  // Emit page.html rather than page/index.html. HelpService appends ".html", so the
  // manuals work on any static host without rewrite rules — nothing to configure on
  // Azure Static Web Apps, S3, or anything else.
  cleanUrls: false,

  // Fail the build on a broken internal link, the same guard mkdocs --strict gave us.
  ignoreDeadLinks: false,

  srcExclude: isStudent
    ? studentExclusions
    : ['student-home.md', 'reference/glossary-student.md', 'reference/statuses-student.md'],
  rewrites: isStudent ? studentRewrites : {},

  themeConfig: {
    search: { provider: 'local' },
    sidebar: isStudent ? studentSidebar : staffSidebar,
    outline: { level: [2, 3], label: 'On this page' },
    docFooter: { prev: 'Previous', next: 'Next' },
    externalLinkIcon: true
  }
})
