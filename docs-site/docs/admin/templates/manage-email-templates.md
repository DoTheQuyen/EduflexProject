# Manage email templates

**Who can do this:** Admin (`EmailTemplatesEdit`)
**Where:** Staff Portal → **Template** → **Email Templates**
**Before you start:** Nothing.

::: info Reading the list is not restricted
Any staff member composing an email can see the template list. The
`EmailTemplatesEdit` permission gates only creating, updating and
deactivating.
:::

## The list

| Column | Shows |
|---|---|
| Template name | The human-readable name. |
| Key | The identifier the system uses to find the template. |
| Subject | The subject line. |
| Type | A badge marking system-default templates. |

| Row button | When it appears |
|---|---|
| **Edit** | Always. |
| **Deactivate** | While the template is active. |

## Create a template

Select **New Email Template**.

| Field | Notes |
|---|---|
| **Key** | The identifier the sending flow looks up, for example `welcome-email`. **Cannot be changed after creation** — the field is disabled when editing, with the note *Key can't be changed after creation.* |
| **Template name** | Human-readable, for example *Welcome & login details*. |
| **Subject line** | For example *Welcome to Eduflex — your login details*. |
| **Body** | A rich-text editor. |

Save with **Save Template**, or discard with **Cancel**.

::: warning Choose the key carefully
The key is permanent, and it is what connects a system event to this
template. Getting it wrong means creating a replacement template rather
than renaming this one. Match the key the system already expects — do not
invent one and expect an event to find it.
:::

## Placeholders

The body supports `{{placeholder}}`-style tokens, filled in by the sending flow
when the email goes out. The editor shows the reminder: *Use
`{{studentFirstName}}`-style placeholders — the sending flow fills these in when
the email goes out.*

A placeholder the sending flow does not recognise is not substituted, so it
reaches the recipient as literal text. Send yourself a test before relying on a
new one.

## System default templates

Templates marked **System default** back built-in behaviour. Editing one changes
what the system sends. Deactivating one can stop an expected email going out
entirely — check what triggers it before you do.

## When a change takes effect

The template is read at send time, so a change applies to the **next** email of
that kind. Emails already sent are unaffected and are not retrospectively
rewritten.

## See also

- [Manage invoice templates](manage-invoice-templates.md)
- [Permission matrix](../../reference/permission-matrix.md)
