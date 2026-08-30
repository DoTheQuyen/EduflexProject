# Manage invoice templates

**Who can do this:** Admin and Manager (`InvoiceTemplatesEdit`)
**Where:** Staff Portal → **Template** → **Invoice Templates**
**Before you start:** Have your organisation's ABN and bank details to hand.

::: warning This key does not control sending
`InvoiceTemplatesEdit` gates the template screen and the sent-invoice
ledger. Sending, resending, cancelling or confirming a **specific** invoice
is checked against a different permission depending on who it is addressed
to — see [Permission matrix](../../reference/permission-matrix.md).
:::

## The list

| Column | Shows |
|---|---|
| Template name | |
| Category | A badge — Customer, Partner or Custom. |

| Row button | What it does |
|---|---|
| **Preview** | Renders the template without opening the editor. |
| **Edit** | Opens the editor. |
| **Copy** | Duplicates the template as a starting point for a new one. |

**Copy** is the fastest way to create a second template that differs only in
wording or default amount.

## Create a template

Select **New Invoice Template**. The editor has three buttons at the top:
**Cancel**, **Preview** and **Save Template**.

### Identity

| Field | Notes |
|---|---|
| **Template name** | For example *Standard student invoice*. |
| **Category** | **Customer**, **Partner** or **Custom**. Chosen as a button group. **Disabled when editing** — the category is fixed once the template exists. |

The category decides who the invoice is addressed to, which in turn decides
which permission is required to send it. Pick it deliberately: to change it
later you must create a new template, or **Copy** this one and choose a
different category.

### Sender details

| Field | Required |
|---|---|
| **Sender name** | Yes — for example *Edu Flex PTY LTD* |
| **ABN** | Optional |
| **Address** | A list of address lines. Select **+ Add address line** to add another, and the remove control beside a line to delete it. |
| **Email** | Yes |
| **Phone** | Optional |

### Bank details

**Bank name**, **BSB**, **Account number** and **Account name**. These print on
the invoice as the payment instructions, so check them character by character —
an error here sends money to the wrong place.

### Invoice numbering

| Field | Effect |
|---|---|
| **Prefix** | The leading text on every invoice number, for example `INV-Eduflex`. |
| **Number padding** | How many digits the sequence is padded to. Defaults to 4, so the first invoice is `0001`. |

The editor shows a preview of the next invoice number that would be issued, so
you can confirm the format before saving.

### Default line item

| Field | Notes |
|---|---|
| **Description** | For example *Enrolment Service Fee*. |
| **Amount** | Free input — staff can override it when sending. |
| **GST %** | Free input. |

These are defaults that pre-fill a new invoice, not fixed values.

## Preview before saving

Select **Preview** to see the rendered invoice with your sender, bank and
numbering settings applied. Do this before saving a template you intend to
send from — layout problems are obvious in the preview and invisible in the
form.

## Related screens

| Screen | What it is for |
|---|---|
| **Sent Invoices** | The ledger of every invoice already issued. |
| **Send Custom Invoice** | Issue a one-off invoice that is not driven by a template. |

## See also

- [Record a commission](../../staff/finance/record-a-commission.md)
- [Permission matrix](../../reference/permission-matrix.md)
