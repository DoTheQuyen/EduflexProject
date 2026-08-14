// Mirrors Eduflex/DTOs/VisaProcess/PractitionerTagDto.cs. See docs/09
// §C.9 — a business-managed catalog of staffing/routing labels, purely informational, no
// fixed vocabulary and no access-control behavior anywhere.

export interface PractitionerTag {
  id: string;
  name: string;
  colorHex: string;
  description?: string | null;
  active: boolean;
}

export interface SavePractitionerTagRequest {
  name: string;
  colorHex: string;
  description?: string | null;
  active: boolean;
}

// A handful of presets for the colour picker — a business can still type any hex value
// directly if none of these fit.
export const PRACTITIONER_TAG_COLOR_PRESETS: string[] = [
  '#2e4258', // navy
  '#b8862f', // brass
  '#1f7a5c', // success
  '#6b5b95', // violet
  '#5c6b7a'  // muted
];
