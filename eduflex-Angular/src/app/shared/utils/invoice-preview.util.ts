import { formatDateTime } from './date-time.util';

export interface InvoiceTemplatePreviewFields {
  category: string;
  logoUrl?: string | null;
  senderName: string;
  senderAddressLines: string[];
  senderAbn?: string | null;
  senderEmail: string;
  senderPhone?: string | null;
  bankName?: string | null;
  bankBsb?: string | null;
  bankAccountNumber?: string | null;
  bankAccountName?: string | null;
  defaultDescription?: string | null;
  defaultAmount?: number | null;
  defaultGstRatePercent?: number | null;
  invoiceNoPreview: string;
}

function escapeHtml(value: string): string {
  return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

const SAMPLE_STUDENT_BLOCK = `
  <div style="border:1.5px dashed #B8862F;border-radius:4px;padding:12px 16px;margin-bottom:24px;">
    <p style="margin:0 0 8px;color:#5C6B7A;font-size:11px;text-transform:uppercase;">Student &amp; Enrolment</p>
    <table>
      <tr>
        <td style="padding:2px 20px 6px 0;"><p style="margin:0;color:#5C6B7A;font-size:11px;text-transform:uppercase;">Student</p><p style="margin:0;font-weight:bold;font-size:13px;">Do Thi Quyen</p></td>
        <td style="padding:2px 20px 6px 0;"><p style="margin:0;color:#5C6B7A;font-size:11px;text-transform:uppercase;">Student ID</p><p style="margin:0;font-weight:bold;font-size:13px;">STU-004821</p></td>
        <td style="padding:2px 20px 6px 0;"><p style="margin:0;color:#5C6B7A;font-size:11px;text-transform:uppercase;">Course</p><p style="margin:0;font-weight:bold;font-size:13px;">Bachelor of IT</p></td>
        <td style="padding:2px 20px 6px 0;"><p style="margin:0;color:#5C6B7A;font-size:11px;text-transform:uppercase;">Campus</p><p style="margin:0;font-weight:bold;font-size:13px;">Southport</p></td>
      </tr>
    </table>
  </div>`;

function sampleClaimItemsTableHtml(description: string, amount: number, gstRate: number): string {
  const gstAmount = Math.round(amount * gstRate) / 100;
  const total = amount + gstAmount;
  return `
  <table width="100%" style="border-collapse:collapse;margin-bottom:8px;">
    <thead>
      <tr style="background:#F5F6F7;">
        <th style="text-align:left;padding:8px;border:1px solid #DCE0E4;">Item</th>
        <th style="text-align:right;padding:8px;border:1px solid #DCE0E4;">Amount</th>
        <th style="text-align:right;padding:8px;border:1px solid #DCE0E4;">GST</th>
        <th style="text-align:right;padding:8px;border:1px solid #DCE0E4;">Subtotal</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td style="padding:8px;border:1px solid #DCE0E4;">${escapeHtml(description)}</td>
        <td style="padding:8px;border:1px solid #DCE0E4;text-align:right;">${amount.toFixed(2)}</td>
        <td style="padding:8px;border:1px solid #DCE0E4;text-align:right;">${gstAmount.toFixed(2)}</td>
        <td style="padding:8px;border:1px solid #DCE0E4;text-align:right;">${total.toFixed(2)}</td>
      </tr>
    </tbody>
    <tfoot>
      <tr>
        <td colspan="3" style="padding:8px;text-align:right;font-weight:bold;border:1px solid #DCE0E4;">Total</td>
        <td style="padding:8px;text-align:right;font-weight:bold;border:1px solid #DCE0E4;">${total.toFixed(2)}</td>
      </tr>
    </tfoot>
  </table>`;
}

const SAMPLE_FREE_TEXT_BODY = `
  <div style="font-size:13px;line-height:1.7;border-bottom:1px solid #DCE0E4;padding-bottom:16px;margin-bottom:24px;">
    <p style="margin:0 0 12px;">Consulting services rendered for curriculum review, October 2026.</p>
    <p style="margin:0 0 12px;">Total due: $2,750.00 (incl. GST). Payment terms: 14 days.</p>
  </div>`;

// Client-side mirror of InvoiceService.BuildInvoiceHtml (backend, C#) — kept in sync
// manually since the backend one renders real sent invoices while this one renders
// sample/unsaved template data that never touches the server. Branches on category the
// same way the backend does: Custom gets free-text sample content, Partner gets a
// sample multi-item claims table plus the student/enrolment block, Customer keeps the
// original single-line sample.
export function buildInvoiceTemplatePreviewHtml(template: InvoiceTemplatePreviewFields): string {
  const logoHtml = template.logoUrl
    ? `<img src="${template.logoUrl}" style="max-height:72px;max-width:300px;" />`
    : `<h2 style="margin:0;color:#16233A;">${escapeHtml(template.senderName || 'Sender name')}</h2>`;

  const senderAddressHtml = (template.senderAddressLines || [])
    .map(l => l.trim()).filter(Boolean).map(escapeHtml).join('<br/>');
  const abnLine = template.senderAbn ? `<p style="margin:0;">ABN: ${escapeHtml(template.senderAbn)}</p>` : '';
  const phoneLine = template.senderPhone ? `<p style="margin:0;">M: ${escapeHtml(template.senderPhone)}</p>` : '';

  const bankBox = template.bankName
    ? `<div style="border:1px solid #DCE0E4;padding:12px 16px;border-radius:4px;">
         <p style="margin:0 0 4px;font-weight:bold;">Bank details:</p>
         <p style="margin:0;">Bank Name: ${escapeHtml(template.bankName)}</p>
         <p style="margin:0;">BSB: ${escapeHtml(template.bankBsb || '')}</p>
         <p style="margin:0;">Account Number: ${escapeHtml(template.bankAccountNumber || '')}</p>
         <p style="margin:0;">Account Name: ${escapeHtml(template.bankAccountName || '')}</p>
       </div>`
    : '';

  const description = template.defaultDescription?.trim() || 'Sample line item';
  const amount = template.defaultAmount ?? 100;
  const gstRate = template.defaultGstRatePercent ?? 10;

  const studentBlock = template.category === 'Partner' ? SAMPLE_STUDENT_BLOCK : '';
  const bodyHtml = template.category === 'Custom'
    ? SAMPLE_FREE_TEXT_BODY
    : sampleClaimItemsTableHtml(description, amount, gstRate);

  return `
<div style="font-family:Arial,Helvetica,sans-serif;color:#1B2430;">
  <table width="100%" style="margin-bottom:24px;">
    <tr>
      <td style="vertical-align:top;width:50%;">${logoHtml}</td>
      <td style="text-align:right;vertical-align:top;">
        <h2 style="margin:0 0 8px;">Invoice</h2>
        <p style="margin:0;font-size:14px;"><strong>Invoice No:</strong> ${escapeHtml(template.invoiceNoPreview)}</p>
        <p style="margin:0;font-size:14px;"><strong>Invoice Date:</strong> ${formatDateTime(new Date(), 'dd/MM/yyyy')}</p>
      </td>
    </tr>
  </table>

  <table width="100%" style="margin-bottom:24px;">
    <tr>
      <td style="vertical-align:top;width:50%;">
        <p style="margin:0 0 4px;color:#5C6B7A;font-size:12px;text-transform:uppercase;">From</p>
        <p style="margin:0;font-weight:bold;">${escapeHtml(template.senderName || 'Sender name')}</p>
        <p style="margin:0;">${senderAddressHtml}</p>
        ${abnLine}
        <p style="margin:0;">E: ${escapeHtml(template.senderEmail || '')}</p>
        ${phoneLine}
      </td>
      <td style="vertical-align:top;width:50%;text-align:right;">
        <p style="margin:0 0 4px;color:#5C6B7A;font-size:12px;text-transform:uppercase;">To</p>
        <p style="margin:0;font-weight:bold;">Sample Recipient</p>
        <p style="margin:0;">sample.recipient@example.com</p>
      </td>
    </tr>
  </table>

  ${studentBlock}

  ${bodyHtml}

  <table width="100%" style="margin-top:24px;">
    <tr>
      <td style="vertical-align:top;width:50%;">
        <div style="border:1px solid #DCE0E4;padding:12px 16px;border-radius:4px;">
          <p style="margin:0 0 4px;font-weight:bold;">Any queries regarding this invoice please contact:</p>
          <p style="margin:0;">${escapeHtml(template.senderName || '')}</p>
          <p style="margin:0;">E: ${escapeHtml(template.senderEmail || '')}</p>
          ${phoneLine}
        </div>
      </td>
      <td style="vertical-align:top;width:50%;padding-left:16px;">${bankBox}</td>
    </tr>
  </table>
</div>`.trim();
}
