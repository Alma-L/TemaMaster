import { useEffect, useState, type FC, type ReactNode } from 'react';
import {
  Brain,
  Scale,
  BadgeCheck,
  CheckCircle2,
  AlertCircle,
  Clock,
  HelpCircle,
  UserCircle2,
  FileDown,
  Copy,
  Check,
  Loader2,
  type LucideIcon,
} from 'lucide-react';
import { apiFetch, API_BASE_URL } from '../api/apiClient';
import { BankCustomer, DecisionRecord } from '../types';
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
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from 'src/components/ui/accordion';
import { Badge } from 'src/components/ui/badge';
import { Button } from 'src/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from 'src/components/ui/dialog';
import { cn } from 'src/lib/utils';

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

/**
 * Splits the audit trail into its entries. New decisions tag each entry with its
 * TAO phase ("[Thought] …", "[Action] …", "[Observation] …"); older ones don't, so
 * the tag is optional when matching.
 */
const parseAuditTrail = (auditTrail: string) => {
  const lines = auditTrail.split('|').map(l => l.trim().replace(/^\[(Thought|Action|Observation)\]\s*/, ''));
  return {
    mlPrediction: lines.find(l => l.startsWith('ML Prediction')) || '',
    businessRules: lines.find(l => l.startsWith('Business Rules')) || '',
    override: lines.find(l => l.startsWith('OVERRIDE')) || '',
    interestRate: lines.find(l => l.startsWith('Interest Rate Calculated')) || '',
    ratePolicy: lines.find(l => l.startsWith('Interest Rate Policy')) || '',
  };
};

const ProfileField: FC<{ label: string; value: string }> = ({ label, value }) => (
  <div>
    <p className="text-[11px] text-slate-400 uppercase tracking-wide">{label}</p>
    <p className="text-sm text-navy-900 font-medium mt-0.5">{value}</p>
  </div>
);

/** Kutia e bardhë brenda një hapi TAO */
const InfoBlock: FC<{ title: string; children: ReactNode; className?: string }> = ({
  title,
  children,
  className,
}) => (
  <div className={cn('bg-white p-4 rounded-lg border', className)}>
    <p className="text-sm text-muted-foreground font-medium mb-2">{title}</p>
    {children}
  </div>
);

const TechnicalNote: FC<{ title: string; children: ReactNode; mono?: boolean }> = ({ title, children, mono }) => (
  <div className="text-xs text-muted-foreground bg-white p-3 rounded-lg border">
    <p className="font-medium text-slate-600 mb-1">{title}</p>
    <p className={cn(mono && 'whitespace-pre-wrap font-mono text-slate-600')}>{children}</p>
  </div>
);

/** Titulli i një hapi Mendim / Veprim / Observim */
const StepTrigger: FC<{ icon: LucideIcon; tone: string; title: string; subtitle: string }> = ({
  icon: Icon,
  tone,
  title,
  subtitle,
}) => (
  <AccordionTrigger className="px-4 py-3.5 hover:no-underline hover:bg-slate-50">
    <div className="flex items-center gap-3">
      <div className={cn('w-8 h-8 rounded-md flex items-center justify-center flex-shrink-0', tone)}>
        <Icon className="w-4 h-4" />
      </div>
      <div className="text-left">
        <h3 className="font-semibold text-navy-900 text-sm">{title}</h3>
        <p className="text-xs text-muted-foreground font-normal">{subtitle}</p>
      </div>
    </div>
  </AccordionTrigger>
);

/**
 * Dritarja e Detajeve të Vendimit
 *
 * Majtas: profili i klientit që bëri kërkesën.
 * Djathtas: çfarë ndodhi me kërkesën e tij (vendimi dhe pse u mor).
 * Poshtë: cikli Mendim-Veprim-Observim (Thought-Action-Observation) teknik.
 */
export const DecisionDetail: FC<DecisionDetailProps> = ({ decision, onClose }) => {
  const [customer, setCustomer] = useState<BankCustomer | null>(null);
  const [customerLoading, setCustomerLoading] = useState(true);
  const [customerMissing, setCustomerMissing] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setCustomerLoading(true);
    setCustomerMissing(false);
    // Profile as it was when this decision was made (snapshot), not the latest one
    apiFetch<BankCustomer>(`v1/decisions/${decision.id}/customer`, { method: 'GET' })
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
  }, [decision.id]);

  const auditComponents = parseAuditTrail(decision.auditTrail);
  const plainSummary = buildPlainSummary(decision);
  // Explicit dd/MM/yyyy, HH:mm:ss: 'sq-AL' alone renders differently across browsers
  const decisionTime = new Date(decision.createdAt).toLocaleString('en-GB', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false,
  });

  const copyAuditTrail = async () => {
    const text = `Vendimi #${decision.id}\n\n${plainSummary}\n\nGjurma teknike:\n${decision.auditTrail}`;
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <Dialog open onOpenChange={open => !open && onClose()}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto p-0 gap-0">
        {/* Header */}
        <DialogHeader className="px-6 py-5 border-b text-left">
          <DialogTitle className="text-lg text-navy-900">Analiza e Vendimit</DialogTitle>
          <DialogDescription>
            Klienti #{decision.customerId} • {decisionTime}
          </DialogDescription>
        </DialogHeader>

        {/* Profili i klientit (majtas) + Vendimi (djathtas) */}
        <div className="grid grid-cols-1 md:grid-cols-2 divide-y md:divide-y-0 md:divide-x border-b">
          {/* Profili i Klientit */}
          <div className="px-6 py-5">
            <div className="flex items-center gap-2 mb-4">
              <UserCircle2 className="w-4 h-4 text-navy-600" />
              <h3 className="text-sm font-semibold text-navy-900">Profili i Klientit</h3>
            </div>

            {customerLoading ? (
              <div className="flex items-center gap-2 text-sm text-slate-400">
                <Loader2 className="h-4 w-4 animate-spin text-navy-600" />
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
                <ProfileField label="Fushata e Mëparshme" value={labelFor(POUTCOME_OPTIONS, customer.pOutcome)} />
              </div>
            )}
          </div>

          {/* Çfarë ndodhi me kërkesën */}
          <div className="px-6 py-5">
            <div className="flex items-center justify-between mb-4">
              <div>
                <p className="text-sm text-muted-foreground font-medium">Vendimi Final</p>
                <p className="text-xl font-bold text-navy-900 mt-1">
                  {decision.finalDecision ? 'Miratuar' : 'Refuzuar'}
                </p>
              </div>
              <div className="flex flex-col items-end gap-2">
                <Badge
                  variant={decision.finalDecision ? 'success' : 'danger'}
                  className="gap-2 px-3 py-1.5 text-sm border-transparent"
                >
                  {decision.finalDecision ? (
                    <CheckCircle2 className="w-4 h-4" />
                  ) : (
                    <AlertCircle className="w-4 h-4" />
                  )}
                  {decision.finalDecision ? 'MIRATUAR' : 'REFUZUAR'}
                </Badge>
                {decision.wasOverridden && (
                  <Badge variant="warning" className="gap-1 border-transparent bg-warning-100">
                    <AlertCircle className="w-3 h-3" />
                    Anuluar nga Rregullat
                  </Badge>
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

          <Accordion type="single" collapsible defaultValue="thought" className="space-y-3">
            {/* THOUGHT */}
            <AccordionItem value="thought" className="border rounded-lg overflow-hidden">
              <StepTrigger
                icon={Brain}
                tone="bg-navy-50 text-navy-600"
                title="Mendimi (AI)"
                subtitle="Analiza e Parashikimit të Modelit ML.NET"
              />
              <AccordionContent className="px-4 py-4 bg-slate-50 border-t space-y-3">
                <InfoBlock title="Parashikimi i AI-së">
                  <p className="text-lg font-bold text-navy-800">{decision.mlPredicted ? 'MIRATO' : 'REFUZO'}</p>
                  <p className="text-xs text-slate-400 mt-1">
                    Bazuar në modelin e klasifikimit binar të ML.NET, i trajnuar me të dhëna bankare reale
                  </p>
                </InfoBlock>

                <InfoBlock title="Niveli i Besueshmërisë">
                  <div className="flex items-center gap-3">
                    <div className="flex-1 bg-slate-200 rounded-full h-2">
                      <div
                        className="bg-navy-600 h-2 rounded-full transition-all"
                        style={{ width: `${decision.mlConfidence * 100}%` }}
                      />
                    </div>
                    <span className="font-bold text-navy-800 tabular-nums">
                      {(decision.mlConfidence * 100).toFixed(2)}%
                    </span>
                  </div>
                </InfoBlock>

                <TechnicalNote title="Të dhëna teknike (nga sistemi)">
                  {auditComponents.mlPrediction || 'Komponenti i parashikimit të AI-së'}
                </TechnicalNote>
              </AccordionContent>
            </AccordionItem>

            {/* ACTION */}
            <AccordionItem value="action" className="border rounded-lg overflow-hidden">
              <StepTrigger
                icon={Scale}
                tone="bg-warning-50 text-warning-600"
                title="Veprimi (Rregullat e Biznesit)"
                subtitle="Përpunimi nga Motori i Rregullave"
              />
              <AccordionContent className="px-4 py-4 bg-slate-50 border-t space-y-3">
                <InfoBlock title="Vlerësimi i Rregullave të Biznesit">
                  <p className="text-lg font-bold text-warning-700">
                    {auditComponents.businessRules.includes('PASS') ? 'KALUAR' : 'DËSHTUAR'}
                  </p>
                  <p className="text-xs text-slate-400 mt-1">
                    {auditComponents.businessRules || 'Vlerësimi i rregullave të biznesit'}
                  </p>
                </InfoBlock>

                <InfoBlock title="Rregullat e Aplikuara">
                  <div className="space-y-2">
                    {decision.rulesApplied
                      .split(',')
                      .filter(r => r.trim())
                      .map((rule, idx) => (
                        <div
                          key={idx}
                          className="flex items-center gap-2 text-sm text-slate-700 bg-slate-50 px-3 py-2 rounded-md border"
                        >
                          <span className="w-1.5 h-1.5 rounded-full bg-warning-600" />
                          {rule.trim()}
                        </div>
                      ))}
                  </div>
                </InfoBlock>

                {decision.wasOverridden && (
                  <div className="bg-danger-50 p-4 rounded-lg border border-danger-200">
                    <p className="text-sm text-danger-700 font-semibold mb-1">Anulim i Aktivizuar</p>
                    <p className="text-sm text-danger-600">
                      {decision.overrideReason || 'U aplikua anulimi nga rregullat e biznesit'}
                    </p>
                  </div>
                )}

                {auditComponents.ratePolicy && (
                  <InfoBlock title="Politika e Normës së Interesit (rregull dinamik)">
                    <p
                      className={cn(
                        'text-sm font-semibold',
                        auditComponents.ratePolicy.includes('PASS') ? 'text-navy-800' : 'text-danger-700'
                      )}
                    >
                      {auditComponents.ratePolicy.includes('PASS')
                        ? 'Oferta është brenda kufijve të politikës'
                        : 'Oferta del jashtë kufijve të politikës'}
                    </p>
                    <p className="text-xs text-slate-400 mt-1">{auditComponents.ratePolicy}</p>
                  </InfoBlock>
                )}

                <TechnicalNote title="Të dhëna teknike (nga sistemi)">
                  {auditComponents.override || 'Detajet e vlerësimit të rregullave'}
                </TechnicalNote>
              </AccordionContent>
            </AccordionItem>

            {/* OBSERVATION */}
            <AccordionItem value="observation" className="border rounded-lg overflow-hidden">
              <StepTrigger
                icon={BadgeCheck}
                tone="bg-success-50 text-success-600"
                title="Observimi (Vendimi Final)"
                subtitle="Verdikti Final dhe Regjistrimi"
              />
              <AccordionContent className="px-4 py-4 bg-slate-50 border-t space-y-3">
                <InfoBlock title="Vendimi Final">
                  <p className={`text-lg font-bold ${decision.finalDecision ? 'text-success-700' : 'text-danger-600'}`}>
                    {decision.finalDecision ? 'VENDIMI: MIRATUAR' : 'VENDIMI: REFUZUAR'}
                  </p>
                  <p className="text-xs text-slate-400 mt-1">Verdikti i kombinuar i AI-së dhe rregullave të biznesit</p>
                </InfoBlock>

                {decision.approvedInterestRate > 0 && (
                  <InfoBlock title="Norma e Interesit e Miratuar">
                    <p className="text-3xl font-bold text-navy-800 tabular-nums">
                      {(decision.approvedInterestRate * 100).toFixed(3)}%
                    </p>
                    <p className="text-xs text-slate-400 mt-1">
                      {auditComponents.interestRate || 'Llogaritja e normës së interesit'}
                    </p>
                  </InfoBlock>
                )}

                <div className="bg-white p-4 rounded-lg border flex items-center gap-3">
                  <Clock className="w-4 h-4 text-slate-400" />
                  <div>
                    <p className="text-xs text-muted-foreground font-medium">Koha e Vendimit</p>
                    <p className="text-sm font-mono text-navy-800">{decisionTime}</p>
                  </div>
                </div>

                <TechnicalNote title="Gjurma e Plotë e Auditimit (origjinale nga sistemi)" mono>
                  {decision.auditTrail}
                </TechnicalNote>
              </AccordionContent>
            </AccordionItem>
          </Accordion>
        </div>

        {/* Footer */}
        <DialogFooter className="sticky bottom-0 bg-white border-t px-6 py-4 gap-2 sm:gap-2">
          <Button variant="secondary" onClick={onClose}>
            Mbyll
          </Button>
          <Button variant="secondary" onClick={copyAuditTrail}>
            {copied ? <Check /> : <Copy />}
            {copied ? 'U kopjua' : 'Kopjo Gjurmën e Auditimit'}
          </Button>
          <Button asChild>
            <a href={`${API_BASE_URL}/v1/decisions/${decision.id}/report`} target="_blank" rel="noopener noreferrer">
              <FileDown />
              Shkarko Raportin PDF
            </a>
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};

export default DecisionDetail;
