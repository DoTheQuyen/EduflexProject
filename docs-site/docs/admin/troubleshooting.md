# Troubleshooting

| Symptom | Likely cause | What to do |
|---|---|---|
| A permission change had no effect | The user is on a session that started before the change. | Ask them to sign out and back in. See [Assign permissions](users/assign-permissions.md). |
| A new user cannot sign in | The account is inactive, or no role is assigned. | Check the user record in **Administration → Users**. |
| A form template does not appear for students | It is not attached to their application. | Attach it from the application detail page. |
| An email was not sent | The template is empty, or the triggering event did not fire. | Check the template, then the related record history. |
| A role cannot be deleted | Users are still assigned to it. | Move those users to another role first. |
