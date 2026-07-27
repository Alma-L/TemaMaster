import { useEffect, useState, type FC } from 'react';
import {
  X,
  Lightbulb,
  Zap,
  CheckCircle2,
  AlertCircle,
  Clock,
  Layers,
  HelpCircle,
  ChevronDown,
  UserCircle2,
  FileDown,
} from 'lucide-react';
import { apiFetch, API_BASE_URL } from '../api/apiClient';
import { BankCustomer } from '../types';
import {
  JOBS,
  MARITAL_OPTIONS,
  EDUCATION_OPTIONS,
  YES_NO,
  CONTACT_OPTIONS,
  MONTH_OPTIONS,
  POUTCOME_OPTIONS,
  labelFor,
} from '../utils/bankLabels';

interface DecisionRecord {
  id: number;
  customerId: number;
  mlPredictionResultId: number;
  mlPredicted: boolean;
  mlConfidence: number;
  finalDecision: boolean;
  auditTrail: string;
  approvedInterestRate: number;
  rulesApplied: string;
  createdAt: string;
  wasOverridden: boolean;
  overrideReason: string;
}

interface DecisionDetailProps {
  decision: DecisionRecord;
  onClose: () => void;
}

/**
 * Ndërton një fjali të thjeshtë në shqip që shpjegon pse u mor ky vendim,
 * bazuar drejtpërdrejt në të dhënat e vendimit (jo në gjurmën teknike të auditimit).
 */
const buildPlainSummary = (decision: DecisionRecord): string => {
  const confidence = (decision.mlConfidence * 100).toFixed(1);
  const finalLabel = decision.finalDecision ? 'MIRATUAR' : 'REFUZUAR';

  if (!decision.wasOverridden) {
    if (decision.finalDecision) {
      return `Modeli i Inteligjencës Artificiale parashikoi miratim me ${confidence}% besueshmëri, dhe kërkesa përmbushi të gjitha rregullat e bankës. Për këtë arsye, kërkesa u miratua me një normë interesi prej ${(decision.approvedInterestRate * 100).toFixed(2)}%.`;
    }
    return `Modeli i Inteligjencës Artificiale vlerësoi vetëm ${confidence}% probabilitet për sukses — shumë i ulët për miratim. Për këtë arsye, kërkesa u refuzua.`;
  }

  const predictedLabel = decision.mlPredicted ? 'miratim' : 'refuzim';
  return `Modeli i Inteligjencës Artificiale parashikoi fillimisht ${predictedLabel} me ${confidence}% besueshmëri. Megjithatë, rregullat e biznesit të bankës e ndryshuan vendimin final në ${finalLabel}, për shkak se: ${
    decision.overrideReason || 'u shkel një nga rregullat e bankës'
  }`;
};

const ProfileField: FC<{ label: string; value: string }> = ({ label, value }) => (
  <div>
    <p className="text-[11px] text-slate-400 uppercase tracking-wide">{label}</p>
    <p className="text-sm text-navy-900 font-medium mt-0.5">{value}</p>
  </div>
);

/**
 * Dritarja e Detajeve të Vendimit
 *
 * Majtas: profili i klientit që bëri kërkesën.
 * Djathtas: çfarë ndodhi me kërkesën e tij (vendimi dhe pse u mor).
 * Poshtë: cikli Mendim-Veprim-Observim (Thought-Action-Observation) teknik.
 */
export const DecisionDetail: FC<DecisionDetailProps> = ({
  decision,
  onClose,
}: DecisionDetailProps) => {
  const [expandedSection, setExpandedSection] = useState<string | null>('thought');
  const [customer, setCustomer] = useState<BankCustomer | null>(null);
  const [customerLoading, setCustomerLoading] = useState(true);
  const [customerMissing, setCustomerMissing] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setCustomerLoading(true);
    setCustomerMissing(false);
    apiFetch<BankCustomer>(`v1/customers/${decision.customerId}`, { method: 'GET' })
      .then(data => {
        if (!cancelled) setCustomer(data);
      })
      .catch(() => {
        if (!cancelled) setCustomerMissing(true);
      })
      .finally(() => {
        if (!cancelled) setCustomerLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [decision.customerId]);

  const parseAuditTrail = (auditTrail: string) => {
    const lines = auditTrail.split('|').map(l => l.trim());
    return {
      mlPrediction: lines.find(l => l.startsWith('ML Prediction')) || '',
      businessRules: lines.find(l => l.startsWith('Business Rules')) || '',
      override: lines.find(l => l.startsWith('OVERRIDE')) || '',
      interestRate: lines.find(l => l.startsWith('Interest Rate')) || '',
    };
  };

  const auditComponents = parseAuditTrail(decision.auditTrail);
  const plainSummary = buildPlainSummary(decision);

  const toggleSection = (section: string) => {
    setExpandedSection(expandedSection === section ? null : section);
  };

  return (
    <div className="fixed inset-0 bg-navy-950 bg-opacity-40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-2xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-navy-900 px-6 py-5 flex items-center justify-between z-10">
          <div className="flex items-center gap-3">
            <Layers className="w-5 h-5 text-navy-300" />
            <div>
              <h2 className="text-lg font-semibold text-white">Analiza e Vendimit</h2>
              <p className="text-navy-300 text-sm">
                Klienti #{decision.customerId} • {new Date(decision.createdAt).toLocaleString('sq-AL')}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-navy-300 hover:text-white hover:bg-white hover:bg-opacity-10 p-2 rounded-lg transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Profili i klientit (majtas) + Vendimi (djathtas) */}
        <div className="grid grid-cols-1 md:grid-cols-2 divide-y md:divide-y-0 md:divide-x divide-slate-200 border-b border-slate-200">
          {/* Profili i Klientit */}
          <div className="px-6 py-5">
            <div className="flex items-center gap-2 mb-4">
              <UserCircle2 className="w-4 h-4 text-navy-600" />
              <h3 className="text-sm font-semibold text-navy-900">Profili i Klientit</h3>
            </div>

            {customerLoading ? (
              <div className="flex items-center gap-2 text-sm text-slate-400">
                <div className="animate-spin rounded-full h-4 w-4 border-2 border-slate-200 border-t-navy-600"></div>
                Duke ngarkuar profilin...
              </div>
            ) : customerMissing || !customer ? (
              <p className="text-sm text-slate-400 italic">
                Profili i plotë i klientit nuk është i disponueshëm për këtë vendim (i krijuar para
                se sistemi të ruante profilet e klientëve).
              </p>
            ) : (
              <div className="grid grid-cols-2 gap-x-4 gap-y-3">
                <ProfileField label="Mosha" value={`${customer.age} vjeç`} />
                <ProfileField label="Profesioni" value={labelFor(JOBS, customer.job)} />
                <ProfileField label="Gjendja Civile" value={labelFor(MARITAL_OPTIONS, customer.marital)} />
                <ProfileField label="Arsimimi" value={labelFor(EDUCATION_OPTIONS, customer.education)} />
                <ProfileField label="Bilanci Bankar" value={`€${customer.balance.toLocaleString('sq-AL')}`} />
                <ProfileField label="Histori Mospagimi" value={labelFor(YES_NO, customer.default)} />
                <ProfileField label="Kredi Banesore" value={labelFor(YES_NO, customer.housing)} />
                <ProfileField label="Kredi Personale" value={labelFor(YES_NO, customer.loan)} />
                <ProfileField label="Mënyra e Kontaktit" value={labelFor(CONTACT_OPTIONS, customer.contact)} />
                <ProfileField
                  label="Data e Kontaktit"
                  value={`${customer.day} ${labelFor(MONTH_OPTIONS, customer.month)}`}
                />
                <ProfileField label="Kontakte në Fushatë" value={`${customer.campaign}`} />
                <ProfileField
                  label="Fushata e Mëparshme"
                  value={labelFor(POUTCOME_OPTIONS, customer.pOutcome)}
                />
              </div>
            )}
          </div>

          {/* Çfarë ndodhi me kërkesën */}
          <div className="px-6 py-5">
            <div className="flex items-center justify-between mb-4">
              <div>
                <p className="text-sm text-slate-500 font-medium">Vendimi Final</p>
                <p className="text-xl font-bold text-navy-900 mt-1">
                  {decision.finalDecision ? 'Miratuar' : 'Refuzuar'}
                </p>
              </div>
              <div className="text-right">
                <div className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-semibold ${
                  decision.finalDecision
                    ? 'bg-success-50 text-success-700'
                    : 'bg-danger-50 text-danger-700'
                }`}>
                  {decision.finalDecision ? (
                    <>
                      <CheckCircle2 className="w-4 h-4" />
                      MIRATUAR
                    </>
                  ) : (
                    <>
                      <AlertCircle className="w-4 h-4" />
                      REFUZUAR
                    </>
                  )}
                </div>
                {decision.wasOverridden && (
                  <div className="mt-2 inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-semibold bg-warning-100 text-warning-700">
                    <AlertCircle className="w-3 h-3" />
                    Anuluar nga Rregullat
                  </div>
                )}
              </div>
            </div>

            <div className="bg-navy-50 border border-navy-100 rounded-lg p-4 flex items-start gap-3">
              <HelpCircle className="w-4 h-4 text-navy-600 flex-shrink-0 mt-0.5" />
              <div>
                <p className="text-xs font-semibold text-navy-700 uppercase tracking-wide mb-1">
                  Pse u mor ky vendim?
                </p>
                <p className="text-sm text-navy-800 leading-relaxed">{plainSummary}</p>
              </div>
            </div>
          </div>
        </div>

        {/* TAO Framework Visualization */}
        <div className="px-6 py-6 space-y-3">
          <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide">
            Detajet teknike (procesi hap pas hapi)
          </p>
          {/* THOUGHT Section */}
          <div className="border border-slate-200 rounded-lg overflow-hidden">
            <button
              onClick={() => toggleSection('thought')}
              className="w-full px-4 py-3.5 bg-white hover:bg-slate-50 transition flex items-center justify-between"
            >
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-md bg-navy-50 flex items-center justify-center flex-shrink-0">
                  <Lightbulb className="w-4 h-4 text-navy-600" />
                </div>
                <div className="text-left">
                  <h3 className="font-semibold text-navy-900 text-sm">Mendimi (AI)</h3>
                  <p className="text-xs text-slate-500">Analiza e Parashikimit të Modelit ML.NET</p>
                </div>
              </div>
              <ChevronDown className={`w-4 h-4 text-slate-400 transition-transform ${expandedSection === 'thought' ? 'rotate-180' : ''}`} />
            </button>
            {expandedSection === 'thought' && (
              <div className="px-4 py-4 bg-slate-50 border-t border-slate-200 space-y-3">
                <div className="bg-white p-4 rounded-lg border border-slate-200">
                  <p className="text-sm text-slate-500 font-medium mb-2">Parashikimi i AI-së</p>
                  <p className="text-lg font-bold text-navy-800">
                    {decision.mlPredicted ? 'MIRATO' : 'REFUZO'}
                  </p>
                  <p className="text-xs text-slate-400 mt-1">
                    Bazuar në modelin e klasifikimit binar të ML.NET, i trajnuar me të dhëna bankare reale
                  </p>
                </div>

                <div className="bg-white p-4 rounded-lg border border-slate-200">
                  <p className="text-sm text-slate-500 font-medium mb-2">Niveli i Besueshmërisë</p>
                  <div className="flex items-center gap-3">
                    <div className="flex-1">
                      <div className="w-full bg-slate-200 rounded-full h-2">
                        <div
                          className="bg-navy-600 h-2 rounded-full transition-all"
                          style={{ width: `${decision.mlConfidence * 100}%` }}
                        ></div>
                      </div>
                    </div>
                    <span className="font-bold text-navy-800 min-w-fit tabular-nums">
                      {(decision.mlConfidence * 100).toFixed(2)}%
                    </span>
                  </div>
                </div>

                <div className="text-xs text-slate-500 bg-white p-3 rounded-lg border border-slate-200">
                  <p className="font-medium text-slate-600 mb-1">Të dhëna teknike (nga sistemi)</p>
                  <p>{auditComponents.mlPrediction || 'Komponenti i parashikimit të AI-së'}</p>
                </div>
              </div>
            )}
          </div>

          {/* ACTION Section */}
          <div className="border border-slate-200 rounded-lg overflow-hidden">
            <button
              onClick={() => toggleSection('action')}
              className="w-full px-4 py-3.5 bg-white hover:bg-slate-50 transition flex items-center justify-between"
            >
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-md bg-warning-50 flex items-center justify-center flex-shrink-0">
                  <Zap className="w-4 h-4 text-warning-600" />
                </div>
                <div className="text-left">
                  <h3 className="font-semibold text-navy-900 text-sm">Veprimi (Rregullat e Biznesit)</h3>
                  <p className="text-xs text-slate-500">Përpunimi nga Motori i Rregullave</p>
                </div>
              </div>
              <ChevronDown className={`w-4 h-4 text-slate-400 transition-transform ${expandedSection === 'action' ? 'rotate-180' : ''}`} />
            </button>
            {expandedSection === 'action' && (
              <div className="px-4 py-4 bg-slate-50 border-t border-slate-200 space-y-3">
                <div className="bg-white p-4 rounded-lg border border-slate-200">
                  <p className="text-sm text-slate-500 font-medium mb-2">Vlerësimi i Rregullave të Biznesit</p>
                  <p className="text-lg font-bold text-warning-700">
                    {auditComponents.businessRules.includes('PASS') ? 'KALUAR' : 'DËSHTUAR'}
                  </p>
                  <p className="text-xs text-slate-400 mt-1">
                    {auditComponents.businessRules || 'Vlerësimi i rregullave të biznesit'}
                  </p>
                </div>

                <div className="bg-white p-4 rounded-lg border border-slate-200">
                  <p className="text-sm text-slate-500 font-medium mb-3">Rregullat e Aplikuara</p>
                  <div className="space-y-2">
                    {decision.rulesApplied.split(',').filter(r => r.trim()).map((rule: string, idx: number) => (
                      <div
                        key={idx}
                        className="flex items-center gap-2 text-sm text-slate-700 bg-slate-50 px-3 py-2 rounded-md border border-slate-200"
                      >
                        <span className="w-1.5 h-1.5 rounded-full bg-warning-600"></span>
                        {rule.trim()}
                      </div>
                    ))}
                  </div>
                </div>

                {decision.wasOverridden && (
                  <div className="bg-danger-50 p-4 rounded-lg border border-danger-200">
                    <p className="text-sm text-danger-700 font-semibold mb-1">Anulim i Aktivizuar</p>
                    <p className="text-sm text-danger-600">
                      {decision.overrideReason || 'U aplikua anulimi nga rregullat e biznesit'}
                    </p>
                  </div>
                )}

                <div className="text-xs text-slate-500 bg-white p-3 rounded-lg border border-slate-200">
                  <p className="font-medium text-slate-600 mb-1">Të dhëna teknike (nga sistemi)</p>
                  <p>{auditComponents.override || 'Detajet e vlerësimit të rregullave'}</p>
                </div>
              </div>
            )}
          </div>

          {/* OBSERVATION Section */}
          <div className="border border-slate-200 rounded-lg overflow-hidden">
            <button
              onClick={() => toggleSection('observation')}
              className="w-full px-4 py-3.5 bg-white hover:bg-slate-50 transition flex items-center justify-between"
            >
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 rounded-md bg-success-50 flex items-center justify-center flex-shrink-0">
                  <CheckCircle2 className="w-4 h-4 text-success-600" />
                </div>
                <div className="text-left">
                  <h3 className="font-semibold text-navy-900 text-sm">Observimi (Vendimi Final)</h3>
                  <p className="text-xs text-slate-500">Verdikti Final dhe Regjistrimi</p>
                </div>
              </div>
              <ChevronDown className={`w-4 h-4 text-slate-400 transition-transform ${expandedSection === 'observation' ? 'rotate-180' : ''}`} />
            </button>
            {expandedSection === 'observation' && (
              <div className="px-4 py-4 bg-slate-50 border-t border-slate-200 space-y-3">
                <div className="bg-white p-4 rounded-lg border border-slate-200">
                  <p className="text-sm text-slate-500 font-medium mb-2">Vendimi Final</p>
                  <p className="text-lg font-bold text-success-700">
                    {decision.finalDecision ? 'VENDIMI: MIRATUAR' : 'VENDIMI: REFUZUAR'}
                  </p>
                  <p className="text-xs text-slate-400 mt-1">
                    Verdikti i kombinuar i AI-së dhe rregullave të biznesit
                  </p>
                </div>

                {decision.approvedInterestRate > 0 && (
                  <div className="bg-white p-4 rounded-lg border border-slate-200">
                    <p className="text-sm text-slate-500 font-medium mb-2">Norma e Interesit e Miratuar</p>
                    <p className="text-3xl font-bold text-navy-800 tabular-nums">
                      {(decision.approvedInterestRate * 100).toFixed(3)}%
                    </p>
                    <p className="text-xs text-slate-400 mt-1">
                      {auditComponents.interestRate || 'Llogaritja e normës së interesit'}
                    </p>
                  </div>
                )}

                <div className="bg-white p-4 rounded-lg border border-slate-200 flex items-center gap-3">
                  <Clock className="w-4 h-4 text-slate-400" />
                  <div>
                    <p className="text-xs text-slate-500 font-medium">Koha e Vendimit</p>
                    <p className="text-sm font-mono text-navy-800">
                      {new Date(decision.createdAt).toLocaleString('sq-AL')}
                    </p>
                  </div>
                </div>

                <div className="text-xs text-slate-500 bg-white p-3 rounded-lg border border-slate-200">
                  <p className="font-medium text-slate-600 mb-2">Gjurma e Plotë e Auditimit (origjinale nga sistemi)</p>
                  <p className="whitespace-pre-wrap font-mono text-slate-600">
                    {decision.auditTrail}
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="sticky bottom-0 bg-white border-t border-slate-200 px-6 py-4 flex justify-end gap-3">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition text-sm font-medium"
          >
            Mbyll
          </button>
          <button
            onClick={() => {
              const text = `Vendimi #${decision.id}\n\n${plainSummary}\n\nGjurma teknike:\n${decision.auditTrail}`;
              navigator.clipboard.writeText(text);
              alert('Gjurma e auditimit u kopjua!');
            }}
            className="px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition text-sm font-medium"
          >
            Kopjo Gjurmën e Auditimit
          </button>
          <a
            href={`${API_BASE_URL}/v1/decisions/${decision.id}/report`}
            target="_blank"
            rel="noopener noreferrer"
            className="px-4 py-2 bg-navy-700 text-white rounded-lg hover:bg-navy-800 transition text-sm font-medium flex items-center gap-2"
          >
            <FileDown className="w-4 h-4" />
            Shkarko Raportin PDF
          </a>
        </div>
      </div>
    </div>
  );
};

export default DecisionDetail;
