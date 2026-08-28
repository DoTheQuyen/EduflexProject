import { Chart, registerables } from 'chart.js';

// Every dashboard chart (the monthly trend + the four status-breakdown sub-charts) shares
// this registration and the same gradient/shadow bar treatment, so they read as one
// consistent visual system rather than each component reinventing it slightly differently.
let registered = false;

export function ensureChartJsRegistered(): void {
  if (registered) {
    return;
  }
  registered = true;
  Chart.register(...registerables);

  // Soft drop shadow under every bar — subtle (low blur, low opacity) on purpose so it
  // reads as gentle elevation rather than obscuring the data.
  Chart.register({
    id: 'barShadow',
    beforeDatasetsDraw(chart: Chart) {
      const { ctx } = chart;
      ctx.save();
      ctx.shadowColor = 'rgba(22, 35, 58, 0.22)';
      ctx.shadowBlur = 5;
      ctx.shadowOffsetX = 0;
      ctx.shadowOffsetY = 3;
    },
    afterDatasetsDraw(chart: Chart) {
      chart.ctx.restore();
    },
  });
}

export function lighten(hex: string, amount: number): string {
  const num = parseInt(hex.slice(1), 16);
  const r = (num >> 16) & 255;
  const g = (num >> 8) & 255;
  const b = num & 255;
  const mix = (channel: number) => Math.round(channel + (255 - channel) * amount);
  return `rgb(${mix(r)}, ${mix(g)}, ${mix(b)})`;
}

// A subtle top-lighter/bottom-truer vertical gradient per bar — Chart.js evaluates
// backgroundColor functions at draw time, once chart.chartArea actually has real pixel
// bounds, so this can't be precomputed before the canvas is measured.
export function barGradient(hex: string) {
  return (context: any) => {
    const chart = context.chart;
    const area = chart?.chartArea;
    if (!area) {
      return hex; // first measurement pass, before chartArea exists yet
    }
    const gradient = chart.ctx.createLinearGradient(0, area.top, 0, area.bottom);
    gradient.addColorStop(0, lighten(hex, 0.22));
    gradient.addColorStop(1, hex);
    return gradient;
  };
}

export function reducedMotionPreferred(): boolean {
  return typeof window !== 'undefined' && !!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
}
