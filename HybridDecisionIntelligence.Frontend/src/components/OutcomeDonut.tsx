import type { FC } from 'react';
import type { DecisionStats } from '../hooks/useDecisions';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from 'src/components/ui/card';
import { Tooltip, TooltipContent, TooltipTrigger } from 'src/components/ui/tooltip';

/**
 * Deep blue + white: three well-separated lightness steps of the navy ramp.
 * The largest slice (Refuzuar) takes the lightest step so the small, important
 * slices stay visible; every slice is also labelled with its count and share.
 */
const SLICES = [
  { key: 'approved', label: 'Miratuar', color: '#0e1e33' },
  { key: 'overridden', label: 'Anuluar nga Rregullat', color: '#356093' },
  { key: 'rejected', label: 'Refuzuar', color: '#b3c9e0' },
] as const;

const SIZE = 200;
const RADIUS = 78;
const STROKE = 30;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;
const GAP = 2; // white gap between slices, in px along the ring

const fmt = (n: number) => n.toLocaleString('sq-AL');

/**
 * Grafiku rrethor i rezultateve: sa vendime u miratuan, sa i anuluan rregullat
 * e biznesit dhe sa u refuzuan nga AI-ja — mbi të gjitha vendimet (me filtrat).
 */
export const OutcomeDonut: FC<{ stats: DecisionStats }> = ({ stats }) => {
  const values: Record<(typeof SLICES)[number]['key'], number> = {
    approved: stats.approved,
    overridden: stats.overridden,
    rejected: stats.total - stats.approved - stats.overridden,
  };
  const share = (n: number) => (stats.total === 0 ? 0 : (n / stats.total) * 100);

  // Each slice is a dashed circle stroke, offset to start where the previous ended
  let offset = 0;
  const arcs = SLICES.map(slice => {
    const length = (values[slice.key] / stats.total) * CIRCUMFERENCE;
    const visible = Math.max(length - GAP, length > 0 ? 1 : 0);
    const arc = { ...slice, value: values[slice.key], dash: visible, offset };
    offset += length;
    return arc;
  });

  return (
    <Card className="shadow-none h-full">
      <CardHeader className="p-5 pb-2 space-y-1">
        <CardTitle className="text-base text-navy-900">Rezultati i vendimeve</CardTitle>
        <CardDescription>
          Si përfunduan të gjitha vendimet e sistemit hibrid.
        </CardDescription>
      </CardHeader>

      <CardContent className="p-5 pt-3 flex flex-col items-center gap-5">
        {/* Rrethi */}
        <div className="relative flex-shrink-0" style={{ width: SIZE, height: SIZE }}>
          <svg
            viewBox={`0 0 ${SIZE} ${SIZE}`}
            width={SIZE}
            height={SIZE}
            className="-rotate-90"
            role="img"
            aria-label={SLICES.map(s => `${s.label} ${share(values[s.key]).toFixed(1)}%`).join(', ')}
          >
            {arcs.map(arc =>
              arc.value > 0 ? (
                <Tooltip key={arc.key}>
                  <TooltipTrigger asChild>
                    <circle
                      cx={SIZE / 2}
                      cy={SIZE / 2}
                      r={RADIUS}
                      fill="none"
                      stroke={arc.color}
                      strokeWidth={STROKE}
                      strokeDasharray={`${arc.dash} ${CIRCUMFERENCE - arc.dash}`}
                      strokeDashoffset={-arc.offset}
                      className="cursor-default transition-[stroke-width] hover:[stroke-width:34px]"
                    />
                  </TooltipTrigger>
                  <TooltipContent className="text-xs">
                    <span className="font-semibold">{arc.label}</span>: {fmt(arc.value)} (
                    {share(arc.value).toFixed(1)}%)
                  </TooltipContent>
                </Tooltip>
              ) : null
            )}
          </svg>
          <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
            <span className="text-2xl font-bold text-navy-900 tabular-nums">{fmt(stats.total)}</span>
            <span className="text-xs text-muted-foreground">vendime</span>
          </div>
        </div>

        {/* Legjenda (vlerat shfaqen kur kalon miun mbi rreth) */}
        <ul className="flex flex-wrap justify-center gap-x-4 gap-y-2">
          {SLICES.map(slice => (
            <li key={slice.key} className="flex items-center gap-2">
              <span
                className="w-3 h-3 rounded-[3px] flex-shrink-0 ring-1 ring-navy-900/10"
                style={{ backgroundColor: slice.color }}
              />
              <span className="text-sm text-navy-900">{slice.label}</span>
            </li>
          ))}
        </ul>
      </CardContent>
    </Card>
  );
};

export default OutcomeDonut;
