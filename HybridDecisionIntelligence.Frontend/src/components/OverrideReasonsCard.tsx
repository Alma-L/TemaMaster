import type { FC } from 'react';
import type { DecisionStats } from '../hooks/useDecisions';
import { ruleLabel } from '../utils/bankLabels';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from 'src/components/ui/card';

const fmt = (n: number) => n.toLocaleString('sq-AL');

/**
 * Pse anulojnë rregullat: për çdo rregull, sa miratime të AI-së i anuloi.
 * Shtyllat horizontale janë blu të thella; gjatësia është përqindja e të gjitha
 * anulimeve. Një vendim mund të dështojë disa rregulla, prandaj shuma > 100%.
 */
export const OverrideReasonsCard: FC<{ stats: DecisionStats }> = ({ stats }) => {
  const rows = stats.overridesByRule ?? [];
  const aiApproved = stats.approved + stats.overridden;
  const share = (n: number) => (stats.overridden === 0 ? 0 : (n / stats.overridden) * 100);

  return (
    <Card className="shadow-none h-full flex flex-col">
      <CardHeader className="p-5 pb-2 space-y-1">
        <CardTitle className="text-base text-navy-900">Pse anulojnë rregullat</CardTitle>
        <CardDescription>
          {stats.overridden > 0
            ? `${fmt(stats.overridden)} miratime të AI-së u anuluan. Një vendim mund të dështojë më shumë se një rregull.`
            : 'Asnjë vendim nuk është anuluar nga rregullat me filtrat aktualë.'}
        </CardDescription>
      </CardHeader>

      <CardContent className="p-5 pt-4 flex-1 flex flex-col gap-5">
        {rows.map(row => {
          const pct = share(row.count);
          return (
            <div key={row.rule}>
              <div className="flex items-baseline justify-between gap-3 mb-1.5">
                <span className="text-sm font-medium text-navy-900">{ruleLabel(row.rule)}</span>
                <span className="text-sm tabular-nums text-muted-foreground">
                  {fmt(row.count)} vendime ·{' '}
                  <span className="font-semibold text-navy-900">{pct.toFixed(0)}%</span>
                </span>
              </div>
              <div className="h-2.5 rounded-full bg-navy-50 overflow-hidden">
                <div
                  className="h-full rounded-full bg-navy-700 transition-[width] duration-500"
                  style={{ width: `${Math.max(pct, row.count > 0 ? 1 : 0)}%` }}
                />
              </div>
            </div>
          );
        })}

        {/* Çfarë ndodhi me miratimet e AI-së */}
        {aiApproved > 0 && (
          <div className="mt-auto rounded-lg bg-navy-50 border border-navy-100 p-4">
            <p className="text-sm text-navy-900 mb-3">
              AI-ja miratoi <span className="font-semibold">{fmt(aiApproved)}</span> kërkesa — rregullat
              anuluan <span className="font-semibold">{((stats.overridden / aiApproved) * 100).toFixed(0)}%</span>{' '}
              prej tyre.
            </p>
            <div className="flex h-3 gap-[2px] rounded-full overflow-hidden">
              <div className="bg-navy-900" style={{ width: `${(stats.approved / aiApproved) * 100}%` }} />
              <div className="bg-navy-500" style={{ width: `${(stats.overridden / aiApproved) * 100}%` }} />
            </div>
            <div className="flex justify-between mt-2 text-xs text-navy-800">
              <span className="inline-flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-[3px] bg-navy-900" />
                Miratuar {fmt(stats.approved)}
              </span>
              <span className="inline-flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-[3px] bg-navy-500" />
                Anuluar nga rregullat {fmt(stats.overridden)}
              </span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
};

export default OverrideReasonsCard;
