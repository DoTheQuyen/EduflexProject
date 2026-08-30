# Send a custom invoice

**Who can do this:** Depends on the recipient — see the warning below
**Where:** Staff Portal → **Template** → **Invoice Templates** → **Send Custom
Invoice**
**Before you start:** At least one invoice template must exist, since the
custom invoice still borrows a template's letterhead and numbering.

Use this for a one-off charge that no template covers. Regular commission
invoices should be raised from the
[commission record](record-a-commission.md) instead.

## Steps

1. Choose the **Invoice template**. Its logo, sender details and numbering are
   applied to your invoice.
2. Fill in **Bill to** — the recipient's **Name**, **Email** and a
   **Reference**.
3. Write the **Invoice content**: **Subject** and **Message**.
4. Set the **Amount**, the **GST %**, and check the **Billing total**.
5. Send.

## Writing the message

The message field is the body of the invoice PDF, sitting inside the template's
letterhead. Its placeholder says so:

> *Write anything — this becomes the body of the invoice PDF, inside the
> template's logo, sender details and numbering.*

Blank lines start a new paragraph, so leave one between paragraphs rather than
running everything together.

Because this is free text, it is also the part with no validation. Read it back
before sending — the numbering and sender details come from the template and
will be right; the description of what you are charging for is entirely yours.

## Sent invoices

Every invoice issued, custom or not, appears in **Template** → **Invoice
Templates** → **Sent Invoices**.

| Filter | Options |
|---|---|
| Category | **All categories**, **Customer**, **Partner**, **Custom** |
| Status | **All statuses**, **Sent**, **Paid**, **Cancelled**, **Failed** |

Empty, it shows *No invoices sent yet.*

**Failed** means the invoice did not reach the recipient. It is worth filtering
for periodically — a failed invoice is not a chased invoice, and nothing else
surfaces it.

::: warning The permission depends on who you are billing
Sending an invoice is **not** gated by `InvoiceTemplatesEdit`. The key
checked depends on the recipient:

| Recipient | Key checked |
|---|---|
| Student | `EnrolmentsEdit` |
| Education partner, business partner, custom | `FinanceEdit` |
:::

## See also

- [Manage invoice templates](../../admin/templates/manage-invoice-templates.md)
- [Record a commission](record-a-commission.md)
