# Assign permissions

**Who can do this:** Admin
**Where:** Staff Portal → **Administration** → **Roles**
**Before you start:** Decide which role needs the change. Permissions are
granted to **roles**, never to individual users.

## How access works

```
Permission keys  ->  Role  ->  User
```

A user has exactly one role. The role holds a set of permission keys, and each
key unlocks a menu item, a screen or an action. To change what someone can do,
either move them to a different role or change the permissions on the role they
already have.

There is no per-user override. This is deliberate — it keeps access auditable,
because you can answer "who can delete an enrolment?" by looking at roles alone.

## Steps

1. In the sidebar, select **Administration**, then **Roles**.
2. Select the role you want to change.
3. In the **Permissions** grid, tick or clear the permissions.
      - A column header checkbox applies that action to every module.
      - A checkbox beside a module name applies every action for that module.
      - Hovering a cell shows the permission key it maps to.
4. Select **Save Role**.

**Result:** The role's permissions are saved immediately and apply to every
user holding that role. If any users already hold it, the dialog tells you how
many before you save.

::: warning Users must sign in again
Permissions are read **once, at sign-in**, and cached in the browser for
that session. Anyone already signed in keeps their old access until they
sign out and back in.

If you change a role and the user reports that nothing happened, check this
before assuming a fault — it is by far the most common cause.
:::

## If something goes wrong

| Symptom | Cause | What to do |
|---|---|---|
| The user still sees the old menu | They are on a session that started before the change. | Ask them to sign out and back in. |
| The user sees nothing after signing in | Their role has no permissions ticked. | Tick at least the view permissions for the modules they need. |
| A cell shows **—** | That module has no such action. | Nothing to fix — configuration modules have a single Edit key. |
| A permission you expect is not listed | The key is not exposed by the build you are running. | Check the release notes for your version. |
| **Add Role** is missing | Your own role lacks the roles-add permission. | Have another administrator grant it. |

## See also

- [Permission matrix](../../reference/permission-matrix.md) — every key and what it unlocks
- [Create a role](../roles/create-a-role.md)
