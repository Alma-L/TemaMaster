import { useState, useEffect, useMemo } from 'react';
import {
  CheckCircle,
  XCircle,
  AlertCircle,
  TrendingUp,
  Zap,
  Info,
  ChevronRight,
  ChevronLeft,
  Filter,
  X,
  Loader2,
  type LucideIcon,
} from 'lucide-react';
import { DecisionDetail } from './DecisionDetail';
import { NewDecisionForm } from './NewDecisionForm';
import { BusinessRulesPanel } from './BusinessRulesPanel';
import { Topbar } from './Topbar';
import { OutcomeDonut } from './OutcomeDonut';
import { OverrideReasonsCard } from './OverrideReasonsCard';
import { ModelPerformanceCard } from './ModelPerformanceCard';
import { useDecisions, DecisionFilters, EMPTY_FILTERS } from '../hooks/useDecisions';
import { DecisionRecord } from '../types';
import { Button } from 'src/components/ui/button';
import { Badge, type BadgeProps } from 'src/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from 'src/components/ui/card';
import { Input } from 'src/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from 'src/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from 'src/components/ui/table';
import { Tooltip, TooltipContent, TooltipTrigger } from 'src/components/ui/tooltip';
import { cn } from 'src/lib/utils';

// Radix Select doesn't allow an empty value, so "all" stands for "no filter"
const ALL = 'all';

// Number inputs without browser spin buttons, so "Min %" / "Max %" fit in the narrow cells
const percentInputClass =
  'h-8 px-2 text-xs min-w-[72px] [appearance:textfield] ' +
  '[&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none';

/** Min–max input pair for a percentage column (values typed as 0–100) */
const PercentRange: React.FC<{
  min: string;
  max: string;
  onMin: (v: string) => void;
  onMax: (v: string) => void;
  label: string;
}> = ({ min, max, onMin, onMax, label }) => (
  <div className="flex items-center gap-1 min-w-[160px]">
    <Input
      type="number"
      min={0}
      max={100}
      step="0.1"
      inputMode="decimal"
      placeholder="Min %"
      aria-label={`${label} minimale`}
      value={min}
      onChange={e => onMin(e.target.value)}
      className={percentInputClass}
    />
    <span className="text-muted-foreground text-xs">–</span>
    <Input
      type="number"
      min={0}
      max={100}
      step="0.1"
      inputMode="decimal"
      placeholder="Max %"
      aria-label={`${label} maksimale`}
      value={max}
      onChange={e => onMax(e.target.value)}
      className={percentInputClass}
    />
  </div>
);

/** Select filter with a "Të gjitha" option meaning "no filter" */
const BoolSelect: React.FC<{
  value: '' | 'true' | 'false';
  onChange: (v: '' | 'true' | 'false') => void;
  label: string;
  trueLabel: string;
  falseLabel: string;
  className?: string;
}> = ({ value, onChange, label, trueLabel, falseLabel, className }) => (
  <Select
    value={value || ALL}
    onValueChange={v => onChange(v === ALL ? '' : (v as 'true' | 'false'))}
  >
    <SelectTrigger aria-label={label} className={cn('h-8 text-xs', className)}>
      <SelectValue />
    </SelectTrigger>
    <SelectContent>
      <SelectItem value={ALL}>Të gjitha</SelectItem>
      <SelectItem value="true">{trueLabel}</SelectItem>
      <SelectItem value="false">{falseLabel}</SelectItem>
    </SelectContent>
  </Select>
);

const MetricCard: React.FC<{
  title: string;
  value: string;
  caption: string;
  icon: LucideIcon;
  tone: 'navy' | 'success' | 'warning';
}> = ({ title, value, caption, icon: Icon, tone }) => {
  const toneClasses = {
    navy: { border: 'border-l-navy-600', bg: 'bg-navy-50', text: 'text-navy-600' },
    success: { border: 'border-l-success-600', bg: 'bg-success-50', text: 'text-success-600' },
    warning: { border: 'border-l-warning-600', bg: 'bg-warning-50', text: 'text-warning-600' },
  }[tone];

  return (
    <Card className={cn('border-l-[3px] shadow-none', toneClasses.border)}>
      <CardContent className="p-5 flex items-start justify-between">
        <div>
          <p className="text-muted-foreground text-xs font-medium uppercase tracking-wide">{title}</p>
          <p className="text-2xl font-bold text-navy-900 mt-1.5 tabular-nums">{value}</p>
          <p className="text-slate-400 text-xs mt-1.5">{caption}</p>
        </div>
        <div className={cn('w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0', toneClasses.bg)}>
          <Icon className={cn('w-4 h-4', toneClasses.text)} />
        </div>
      </CardContent>
    </Card>
  );
};

const statusBadge = (
  decision: DecisionRecord
): { variant: BadgeProps['variant']; label: string; icon: LucideIcon } => {
  if (decision.wasOverridden) {
    return { variant: 'warning', label: 'Anuluar nga Rregullat', icon: AlertCircle };
  }
  return decision.finalDecision
    ? { variant: 'success', label: 'Miratuar', icon: CheckCircle }
    : { variant: 'danger', label: 'Refuzuar', icon: XCircle };
};

/**
 * Paneli i Vendimeve Bankare (XAI Monitoring Dashboard)
 *
 * Shfaq vendimet e marra nga sistemi hibrid: parashikimi i modelit ML.NET
 * i kombinuar me rregullat e biznesit, së bashku me shpjegimin e plotë (XAI)
 * pas çdo vendimi.
 */
export const DecisionDashboard: React.FC = () => {
  // draftFilters follow the inputs as the user types; appliedFilters are sent to
  // the API after a short pause, so typing doesn't fire a request per keystroke.
  const [draftFilters, setDraftFilters] = useState<DecisionFilters>(EMPTY_FILTERS);
  const [appliedFilters, setAppliedFilters] = useState<DecisionFilters>(EMPTY_FILTERS);
  const { decisions, stats, loading, error, page, setPage, hasNextPage, refetch } = useDecisions(appliedFilters);
  const [selectedDecision, setSelectedDecision] = useState<DecisionRecord | null>(null);
  const [showNewDecisionForm, setShowNewDecisionForm] = useState(false);

  // Apliko filtrat pas 300ms pa shtypje; kthehu te faqja e parë (rezultat i ri)
  useEffect(() => {
    if (JSON.stringify(draftFilters) === JSON.stringify(appliedFilters)) return;
    const timer = setTimeout(() => {
      setAppliedFilters(draftFilters);
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [draftFilters, appliedFilters, setPage]);

  const setFilter = <K extends keyof DecisionFilters>(key: K, value: DecisionFilters[K]) =>
    setDraftFilters(prev => ({ ...prev, [key]: value }));

  const activeFilterCount = Object.values(draftFilters).filter(v => v.trim() !== '').length;

  const clearFilters = () => {
    setDraftFilters(EMPTY_FILTERS);
    setAppliedFilters(EMPTY_FILTERS);
    setPage(1);
  };

  // Metrikat llogariten nga TË GJITHA vendimet në databazë (me filtrat aktivë),
  // jo vetëm nga faqja aktuale, që shkallët të jenë reale
  const metrics = useMemo(() => {
    if (!stats || stats.total === 0) return null;
    const total = stats.total;
    return {
      total,
      approved: stats.approved,
      overridden: stats.overridden,
      automationRate: ((total - stats.overridden) / total) * 100,
      approvalRate: (stats.approved / total) * 100,
      interventionRate: (stats.overridden / total) * 100,
    };
  }, [stats]);

  // Gjendja e ngarkimit
  if (loading && !decisions) {
    return (
      <div className="flex items-center justify-center h-screen bg-slate-50">
        <div className="text-center">
          <Loader2 className="h-10 w-10 animate-spin text-navy-700 mx-auto mb-4" />
          <p className="text-muted-foreground font-medium">Duke ngarkuar vendimet...</p>
        </div>
      </div>
    );
  }

  // Gjendja e gabimit
  if (error) {
    return (
      <div className="flex items-center justify-center h-screen bg-slate-50 p-4">
        <Card className="max-w-md w-full text-center">
          <CardContent className="p-8">
            <XCircle className="w-10 h-10 text-danger-600 mx-auto mb-4" />
            <h2 className="text-lg font-semibold text-navy-900 mb-2">Gabim në Lidhje</h2>
            <p className="text-muted-foreground text-sm mb-6">{error}</p>
            <Button className="w-full" onClick={() => window.location.reload()}>
              Provo Përsëri
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50">
      <Topbar
        stats={stats}
        filtered={activeFilterCount > 0}
        onNewDecision={() => setShowNewDecisionForm(true)}
      />

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Rregullat reale të biznesit, marrë nga API-ja */}
        <BusinessRulesPanel />

        {/* Which population the system results below refer to (the model card uses the test set) */}
        {metrics && (
          <p className="text-xs font-semibold uppercase tracking-wide text-navy-700 mb-3">
            {activeFilterCount > 0
              ? `Rezultatet e sistemit — vendimet që përputhen me filtrat (${metrics.total.toLocaleString('sq-AL')})`
              : `Rezultatet e sistemit — dataset i plotë (${metrics.total.toLocaleString('sq-AL')} klientë)`}
          </p>
        )}

        {/* Metrics Summary Cards */}
        {metrics && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-8">
            <MetricCard
              title="Shkalla e Automatizimit"
              value={`${metrics.automationRate.toFixed(1)}%`}
              caption={`${(metrics.total - metrics.overridden).toLocaleString('sq-AL')} vendime pa ndërhyrje`}
              icon={Zap}
              tone="navy"
            />
            <MetricCard
              title="Shkalla e Miratimit"
              value={`${metrics.approvalRate.toFixed(1)}%`}
              caption={`${metrics.approved.toLocaleString('sq-AL')} nga ${metrics.total.toLocaleString('sq-AL')} kërkesa`}
              icon={TrendingUp}
              tone="success"
            />
            <MetricCard
              title="Ndërhyrja e Rregullave"
              value={`${metrics.interventionRate.toFixed(1)}%`}
              caption={`${metrics.overridden.toLocaleString('sq-AL')} vendime ndryshuan nga rregullat`}
              icon={AlertCircle}
              tone="warning"
            />
          </div>
        )}

        {/* Rezultati (rreth) + arsyet e anulimit, krah për krah */}
        {stats && stats.total > 0 && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5 mb-8">
            <OutcomeDonut stats={stats} />
            <div className="lg:col-span-2">
              <OverrideReasonsCard stats={stats} />
            </div>
          </div>
        )}

        {/* Performanca e modelit ML.NET (metrikat e testimit) */}
        <ModelPerformanceCard />

        {/* Decision Table */}
        <Card className="overflow-hidden shadow-none">
          <CardHeader className="px-6 py-4 border-b flex-row items-center gap-2 space-y-0">
            <CardTitle className="text-base text-navy-900">Regjistri i Vendimeve</CardTitle>
            {activeFilterCount > 0 && (
              <Badge variant="secondary" className="gap-1 font-medium text-navy-700 bg-navy-50">
                <Filter className="w-3 h-3" />
                {activeFilterCount} {activeFilterCount === 1 ? 'filtër aktiv' : 'filtra aktivë'}
              </Badge>
            )}
            {loading && decisions && (
              <Loader2 className="h-3.5 w-3.5 animate-spin text-navy-600" aria-label="Duke ngarkuar" />
            )}
            <Badge variant="secondary" className="ml-auto font-medium text-slate-600">
              Faqja {page}
            </Badge>
          </CardHeader>

          {decisions && (
            <Table>
              <TableHeader>
                <TableRow className="bg-slate-50 hover:bg-slate-50">
                  {['ID e Klientit', 'Probabiliteti i AI-së', 'Vendimi', 'Norma e Interesit', 'Statusi', 'Veprime'].map(
                    title => (
                      <TableHead
                        key={title}
                        className="px-6 text-xs font-semibold text-slate-500 uppercase tracking-wider"
                      >
                        {title}
                      </TableHead>
                    )
                  )}
                </TableRow>
                {/* Filtrat, nën kolonën përkatëse */}
                <TableRow className="bg-white hover:bg-white">
                  <TableHead className="px-6 py-2.5">
                    <Input
                      type="text"
                      inputMode="numeric"
                      placeholder="Kërko ID…"
                      aria-label="Filtro sipas ID-së së klientit"
                      value={draftFilters.customerId}
                      onChange={e => setFilter('customerId', e.target.value.replace(/[^0-9]/g, ''))}
                      className="h-8 px-2 text-xs min-w-[100px]"
                    />
                  </TableHead>
                  <TableHead className="px-6 py-2.5">
                    <PercentRange
                      label="Probabiliteti"
                      min={draftFilters.minProbability}
                      max={draftFilters.maxProbability}
                      onMin={v => setFilter('minProbability', v)}
                      onMax={v => setFilter('maxProbability', v)}
                    />
                  </TableHead>
                  <TableHead className="px-6 py-2.5">
                    <BoolSelect
                      label="Filtro sipas vendimit"
                      value={draftFilters.finalDecision}
                      onChange={v => setFilter('finalDecision', v)}
                      trueLabel="Miratuar"
                      falseLabel="Refuzuar"
                      className="min-w-[110px]"
                    />
                  </TableHead>
                  <TableHead className="px-6 py-2.5">
                    <PercentRange
                      label="Norma e interesit"
                      min={draftFilters.minInterestRate}
                      max={draftFilters.maxInterestRate}
                      onMin={v => setFilter('minInterestRate', v)}
                      onMax={v => setFilter('maxInterestRate', v)}
                    />
                  </TableHead>
                  <TableHead className="px-6 py-2.5">
                    <BoolSelect
                      label="Filtro sipas statusit"
                      value={draftFilters.wasOverridden}
                      onChange={v => setFilter('wasOverridden', v)}
                      trueLabel="Anuluar nga Rregullat"
                      falseLabel="Pa ndërhyrje"
                      className="min-w-[160px]"
                    />
                  </TableHead>
                  <TableHead className="px-6 py-2.5 text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={clearFilters}
                      disabled={activeFilterCount === 0}
                      className="h-8 text-xs text-slate-600"
                    >
                      <X />
                      Pastro
                    </Button>
                  </TableHead>
                </TableRow>
              </TableHeader>

              <TableBody>
                {decisions.length === 0 && (
                  <TableRow className="hover:bg-transparent">
                    <TableCell colSpan={6} className="px-6 py-12 text-center text-muted-foreground">
                      {activeFilterCount > 0 ? (
                        <>
                          Asnjë vendim nuk përputhet me filtrat.{' '}
                          <Button variant="link" className="h-auto p-0" onClick={clearFilters}>
                            Pastro filtrat
                          </Button>
                        </>
                      ) : (
                        'Nuk u gjetën vendime. Ekzekuto API-në për të gjeneruar të dhëna.'
                      )}
                    </TableCell>
                  </TableRow>
                )}

                {decisions.map(decision => {
                  const status = statusBadge(decision);
                  const StatusIcon = status.icon;
                  return (
                    <TableRow
                      key={decision.id}
                      className="cursor-pointer [&>td]:whitespace-nowrap"
                      onClick={() => setSelectedDecision(decision)}
                    >
                      {/* Customer ID */}
                      <TableCell className="px-6 py-3.5">
                        <Badge variant="muted" className="text-sm font-medium tabular-nums">
                          #{decision.customerId}
                        </Badge>
                      </TableCell>

                      {/* ML Score */}
                      <TableCell className="px-6 py-3.5">
                        <div className="flex items-center gap-2">
                          <div className="w-16 bg-slate-200 rounded-full h-1.5">
                            <div
                              className="bg-navy-600 h-1.5 rounded-full transition-all"
                              style={{ width: `${decision.mlConfidence * 100}%` }}
                            />
                          </div>
                          <span className="text-sm text-navy-900 tabular-nums">
                            {(decision.mlConfidence * 100).toFixed(1)}%
                          </span>
                        </div>
                      </TableCell>

                      {/* Final Decision */}
                      <TableCell className="px-6 py-3.5">
                        <Badge
                          variant={decision.finalDecision ? 'success' : 'danger'}
                          className="text-sm font-medium border-transparent"
                        >
                          {decision.finalDecision ? 'Miratuar' : 'Refuzuar'}
                        </Badge>
                      </TableCell>

                      {/* Interest Rate */}
                      <TableCell className="px-6 py-3.5 text-navy-900 tabular-nums">
                        {decision.approvedInterestRate > 0
                          ? `${(decision.approvedInterestRate * 100).toFixed(2)}%`
                          : 'N/A'}
                      </TableCell>

                      {/* Status with Override Badge */}
                      <TableCell className="px-6 py-3.5">
                        <div className="flex items-center gap-2">
                          <Badge variant={status.variant} className="gap-1 font-medium">
                            <StatusIcon className="w-3.5 h-3.5" />
                            {status.label}
                          </Badge>
                          {decision.wasOverridden && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Badge
                                  variant="warning"
                                  className="gap-1 font-medium cursor-help border-transparent bg-warning-100"
                                  onClick={e => e.stopPropagation()}
                                >
                                  <Info className="w-3 h-3" />
                                  Rregull i Aktivizuar
                                </Badge>
                              </TooltipTrigger>
                              <TooltipContent className="max-w-xs">
                                {decision.overrideReason || 'Është aktivizuar një rregull biznesi'}
                              </TooltipContent>
                            </Tooltip>
                          )}
                        </div>
                      </TableCell>

                      {/* Actions */}
                      <TableCell className="px-6 py-3.5 text-right">
                        <Button
                          variant="link"
                          className="h-auto p-0 text-navy-700"
                          onClick={e => {
                            e.stopPropagation();
                            setSelectedDecision(decision);
                          }}
                        >
                          Shiko Detajet
                        </Button>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}

          {/* Pagination */}
          {decisions && (decisions.length > 0 || page > 1) && (
            <div className="px-6 py-3 border-t flex items-center justify-between">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(Math.max(1, page - 1))}
                disabled={page <= 1 || loading}
              >
                <ChevronLeft />
                Faqja Paraardhëse
              </Button>
              <span className="text-sm text-muted-foreground">Faqja {page}</span>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setPage(page + 1)}
                disabled={!hasNextPage || loading}
              >
                Faqja Tjetër
                <ChevronRight />
              </Button>
            </div>
          )}
        </Card>
      </div>

      {/* Decision Detail Modal */}
      {selectedDecision && (
        <DecisionDetail decision={selectedDecision} onClose={() => setSelectedDecision(null)} />
      )}

      {/* New Decision Form Modal */}
      {showNewDecisionForm && (
        <NewDecisionForm
          nextCustomerId={Math.floor(Date.now() / 1000) % 100000 + 500000}
          onClose={() => setShowNewDecisionForm(false)}
          onCreated={() => refetch()}
        />
      )}
    </div>
  );
};

export default DecisionDashboard;
