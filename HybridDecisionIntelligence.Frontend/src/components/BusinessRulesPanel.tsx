import { useEffect, useState, type FC } from 'react';
import {
  ShieldCheck,
  Wallet,
  CalendarRange,
  Briefcase,
  Brain,
  CheckCircle,
  AlertCircle,
  XCircle,
  type LucideIcon,
} from 'lucide-react';
import { apiFetch } from '../api/apiClient';
import { BusinessRule } from '../types';
import { ruleLabel } from '../utils/bankLabels';
import { Badge } from 'src/components/ui/badge';
import { Card } from 'src/components/ui/card';

const ruleIcon = (name: string) => {
  if (/balance/i.test(name)) return Wallet;
  if (/age/i.test(name)) return CalendarRange;
  if (/job|employ/i.test(name)) return Briefcase;
  return ShieldCheck;
};

const describeRule = (rule: BusinessRule): string => {
  const parts: string[] = [];
  if (rule.minBalance > 0) {
    parts.push(`bilanci bankar duhet të jetë të paktën €${rule.minBalance.toLocaleString('sq-AL')}`);
  }
  if (rule.minAge > 18 || rule.maxAge < 100) {
    parts.push(`mosha duhet të jetë mes ${rule.minAge} dhe ${rule.maxAge} vjeç`);
  }
  if (rule.allowedJobs && rule.allowedJobs.length > 0) {
    parts.push(`profesioni duhet të jetë njëri prej: ${rule.allowedJobs.join(', ')}`);
  }
  if (parts.length === 0) {
    parts.push('klienti nuk duhet të ketë histori mospagimi (default)');
  }
  return parts.join('; ') + '.';
};

interface Condition {
  key: string;
  icon: LucideIcon;
  title: string;
  description: string;
}

/** Një kusht i miratimit: numri i hapit, ikona, titulli dhe përshkrimi */
const ConditionTile: FC<{ step: number; condition: Condition }> = ({ step, condition }) => {
  const Icon = condition.icon;
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 transition-colors hover:border-navy-200">
      <div className="flex items-center justify-between mb-3">
        <div className="w-9 h-9 rounded-lg bg-navy-50 flex items-center justify-center">
          <Icon className="w-[18px] h-[18px] text-navy-700" />
        </div>
        <span className="text-[11px] font-semibold tabular-nums tracking-wider text-navy-300">
          {String(step).padStart(2, '0')}
        </span>
      </div>
      <p className="text-sm font-semibold text-navy-900">{condition.title}</p>
      <p className="text-xs mt-1 leading-relaxed text-slate-500">{condition.description}</p>
    </div>
  );
};

/**
 * Paneli i Rregullave të Biznesit
 *
 * Shpjegon kriteret reale që sistemi përdor për të vlerësuar një kërkesë —
 * të marra drejtpërdrejt nga baza e të dhënave (GET /api/v1/businessrules),
 * jo tekst statik i shkruar me dorë.
 */
export const BusinessRulesPanel: FC = () => {
  const [rules, setRules] = useState<BusinessRule[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiFetch<BusinessRule[]>('v1/businessrules', { method: 'GET' })
      .then(data => {
        if (!cancelled) setRules(data);
      })
      .catch(() => {
        if (!cancelled) setRules([]);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  if (rules === null || rules.length === 0) {
    return null;
  }

  const conditions: Condition[] = [
    {
      key: 'ai',
      icon: Brain,
      title: 'Parashikimi i AI-së',
      description:
        'modeli i ML.NET duhet të parashikojë miratim, i trajnuar me 45,211 klientë realë historikë.',
    },
    ...rules.map(rule => ({
      key: `rule-${rule.id}`,
      icon: ruleIcon(rule.name),
      title: ruleLabel(rule.name),
      description: describeRule(rule),
    })),
  ];

  return (
    <Card className="mb-6 overflow-hidden border-navy-900 shadow-none">
      {/* Shiriti blu i thellë */}
      <div className="bg-gradient-to-br from-navy-900 to-navy-800 px-6 py-5 text-white">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div className="flex items-start gap-3 max-w-3xl">
            <div className="w-10 h-10 rounded-lg bg-white/10 ring-1 ring-white/15 flex items-center justify-center flex-shrink-0">
              <ShieldCheck className="w-5 h-5 text-white" />
            </div>
            <div>
              <h2 className="text-base font-semibold tracking-tight">Kriteret e Miratimit të Bankës</h2>
              <p className="text-sm text-navy-200 mt-1 leading-relaxed">
                Një kërkesë miratohet vetëm kur plotësohen <span className="font-semibold text-white">të gjitha</span>{' '}
                kushtet më poshtë — parashikimi i AI-së <span className="font-semibold text-white">dhe</span> rregullat
                e biznesit.
              </p>
            </div>
          </div>
          <span className="inline-flex items-center rounded-full bg-white/10 ring-1 ring-white/15 px-3 py-1 text-xs font-medium text-navy-100">
            {conditions.length} kushte · të gjitha të detyrueshme
          </span>
        </div>
      </div>

      {/* Kushtet, të numëruara si hapa */}
      <div className="bg-white p-5 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        {conditions.map((condition, index) => (
          <ConditionTile key={condition.key} step={index + 1} condition={condition} />
        ))}
      </div>

      {/* Rezultati */}
      <div className="bg-navy-50 border-t border-navy-100 px-6 py-3 flex flex-wrap items-center gap-x-6 gap-y-2 text-xs text-navy-800">
        <span className="font-semibold uppercase tracking-wide text-navy-600">Rezultati</span>
        <span className="inline-flex items-center gap-2">
          Të gjitha kushtet plotësohen
          <span className="text-navy-400">→</span>
          <Badge variant="success" className="gap-1 font-medium">
            <CheckCircle className="w-3 h-3" />
            Miratuar
          </Badge>
        </span>
        <span className="inline-flex items-center gap-2">
          AI miraton, por dështon një rregull
          <span className="text-navy-400">→</span>
          <Badge variant="warning" className="gap-1 font-medium">
            <AlertCircle className="w-3 h-3" />
            Rregull i Aktivizuar
          </Badge>
        </span>
        <span className="inline-flex items-center gap-2">
          AI nuk parashikon miratim
          <span className="text-navy-400">→</span>
          <Badge variant="danger" className="gap-1 font-medium">
            <XCircle className="w-3 h-3" />
            Refuzuar
          </Badge>
        </span>
      </div>
    </Card>
  );
};

export default BusinessRulesPanel;
