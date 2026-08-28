import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

const severityColors = {
  success: { base: '#1F7A5C', dark: 'color-mix(in srgb, #1F7A5C 80%, black)' },
  error: { base: '#B3413A', dark: 'color-mix(in srgb, #B3413A 80%, black)' },
  warn: { base: '#B8862F', dark: 'color-mix(in srgb, #B8862F 80%, black)' },
  info: { base: '#2E4258', dark: '#16233A' },
};

function severityTokens() {
  const tokens: Record<string, { background: string; borderColor: string; color: string }> = {};
  for (const [key, { base, dark }] of Object.entries(severityColors)) {
    tokens[key] = {
      background: `color-mix(in srgb, ${base} 12%, white)`,
      borderColor: base,
      color: dark,
    };
  }
  return tokens;
}

export const EduflexPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: 'color-mix(in srgb, #16233A 8%, white)',
      100: 'color-mix(in srgb, #16233A 20%, white)',
      200: 'color-mix(in srgb, #16233A 40%, white)',
      300: 'color-mix(in srgb, #16233A 60%, white)',
      400: 'color-mix(in srgb, #16233A 80%, white)',
      500: '#16233A',
      600: 'color-mix(in srgb, #16233A 90%, black)',
      700: 'color-mix(in srgb, #16233A 80%, black)',
      800: 'color-mix(in srgb, #16233A 65%, black)',
      900: 'color-mix(in srgb, #16233A 50%, black)',
      950: 'color-mix(in srgb, #16233A 35%, black)',
    },
  },
  components: {
    // Default button padding (0.375rem/0.625rem) is noticeably tighter than the
    // roomier spacing used everywhere else in this redesign (table cells, dialogs) —
    // bumping it centrally so every p-button in the app gets consistent breathing room.
    button: {
      root: {
        paddingX: '1rem',
        paddingY: '0.55rem',
        gap: '0.5rem',
      },
    },
    message: {
      colorScheme: {
        light: severityTokens(),
      },
    },
    toast: {
      colorScheme: {
        light: severityTokens(),
      },
    },
    // SelectButton's items render as ToggleButtons internally — this is the pressed-brass
    // "raised/selected" language already used for the sidebar's active nav item (see
    // sidebar.component.css .sidebar-item-active), reused here as the app-wide default so
    // any segmented control reads as the same tactile control, not a one-off per component.
    togglebutton: {
      root: {
        background: '#F5F6F7',
        borderColor: '#DCE0E4',
        color: '#5C6B7A',
        hoverColor: '#1B2430',
        hoverBackground: '#F5F6F7',
        checkedColor: '#1B2430',
        checkedBorderColor: 'transparent',
      },
      content: {
        checkedBackground: 'linear-gradient(150deg, #D9B876 0%, #B8862F 100%)',
        checkedShadow:
          'inset 0 1px 0 rgba(255, 255, 255, 0.45), inset 0 -2px 4px rgba(0, 0, 0, 0.2), 0 3px 8px rgba(0, 0, 0, 0.25)',
      },
    },
    datatable: {
      // Neutral, low-saturation header — a bold saturated color band reads well as a
      // screenshot but is genuinely more fatiguing over long viewing sessions (WCAG /
      // UX-pattern guidance: color should carry meaning for status/actions, not be
      // used decoratively on structural chrome you look at all day). A faint warm
      // tint (not flat gray) plus a crisp border gives definition without the strain.
      headerCell: {
        background: 'color-mix(in srgb, #B8862F 5%, white)',
        color: '#1B2430',
        hoverBackground: 'color-mix(in srgb, #B8862F 10%, white)',
        hoverColor: '#1B2430',
        borderColor: '#DCE0E4',
        padding: '0.85rem 1rem',
      },
      bodyCell: {
        borderColor: '#DCE0E4',
        padding: '0.75rem 1rem',
      },
      row: {
        stripedBackground: '#F5F6F7',
        hoverBackground: 'color-mix(in srgb, #16233A 6%, white)',
      },
    },
  },
});
