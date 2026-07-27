import { useEffect, useState, type FC } from 'react';
import { ShieldCheck, Wallet, CalendarRange, Briefcase, Sparkles } from 'lucide-react';
import { apiFetch } from '../api/apiClient';
import { BusinessRule } from '../types';

/**
 * Emrat e njohur të rregullave, të përkthyera në shqip.
 * Nëse shtohet një rregull i ri me emër tjetër në bazën e të dhënave,
 * paneli e shfaq gjithsesi, duke përdorur emrin origjinal si rezervë.
 */
const RULE_NAME_TRANSLATIONS: Record<string, string> = {
  'Minimum Balance Rule': 'Bilanci Minimal',
  'Age Eligibility Rule': 'Pranueshmëria sipas Moshës',
  'No Default History': 'Historia e Pagesave',
};

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

  return (
    <div className="bg-white border border-slate-200 rounded-xl p-5 mb-6">
      <div className="flex items-center gap-2 mb-1">
        <ShieldCheck className="w-4 h-4 text-navy-600" />
        <h2 className="text-sm font-semibold text-navy-900">Kriteret e Miratimit të Bankës</h2>
      </div>
      <p className="text-sm text-slate-500 mb-4">
        Një kërkesë miratohet vetëm nëse modeli i AI-së parashikon miratim{' '}
        <span className="font-medium text-navy-700">dhe</span> plotëson të gjitha rregullat
        e mëposhtme. Nëse një rregull dështon, sistemi e anulon vendimin automatikisht — kjo
        shfaqet në tabelë me etiketën <span className="font-medium text-navy-700">"Rregull i Aktivizuar"</span>.
      </p>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        {rules.map(rule => {
          const Icon = ruleIcon(rule.name);
          return (
            <div key={rule.id} className="flex items-start gap-3 p-3 rounded-lg bg-slate-50 border border-slate-100">
              <div className="w-8 h-8 rounded-md bg-navy-50 flex items-center justify-center flex-shrink-0">
                <Icon className="w-4 h-4 text-navy-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-navy-900">
                  {RULE_NAME_TRANSLATIONS[rule.name] || rule.name}
                </p>
                <p className="text-xs text-slate-500 mt-0.5 leading-relaxed">
                  {describeRule(rule)}
                </p>
              </div>
            </div>
          );
        })}

        <div className="flex items-start gap-3 p-3 rounded-lg bg-navy-50 border border-navy-100">
          <div className="w-8 h-8 rounded-md bg-white flex items-center justify-center flex-shrink-0">
            <Sparkles className="w-4 h-4 text-navy-600" />
          </div>
          <div>
            <p className="text-sm font-medium text-navy-900">Parashikimi i AI-së</p>
            <p className="text-xs text-navy-700 mt-0.5 leading-relaxed">
              modeli i ML.NET duhet gjithashtu të parashikojë miratim, i bazuar në 45,211
              klientë realë historikë.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default BusinessRulesPanel;
