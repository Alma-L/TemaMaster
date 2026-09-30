import { useState, type FC, type FormEvent } from 'react';
import { UserPlus, CheckCircle2, AlertCircle, Loader2 } from 'lucide-react';
import { apiFetch } from '../api/apiClient';
import { MakeDecisionRequest, MakeDecisionResponse } from '../types';
import {
  JOBS,
  MARITAL_OPTIONS,
  EDUCATION_OPTIONS,
  YES_NO,
  CONTACT_OPTIONS,
  MONTH_OPTIONS,
  POUTCOME_OPTIONS,
  type LabelOption,
} from '../utils/bankLabels';
import { Button } from 'src/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from 'src/components/ui/dialog';
import { Input } from 'src/components/ui/input';
import { Label } from 'src/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from 'src/components/ui/select';
import { cn } from 'src/lib/utils';

interface NewDecisionFormProps {
  nextCustomerId: number;
  onClose: () => void;
  onCreated: () => void;
}

type FormState = MakeDecisionRequest;

type FieldDef =
  | { key: keyof FormState; label: string; kind: 'number'; min?: number; max?: number; wide?: boolean }
  | { key: keyof FormState; label: string; kind: 'select'; options: LabelOption[]; wide?: boolean };

/** Fushat e formularit, në rendin që shfaqen (dy kolona) */
const FIELDS: FieldDef[] = [
  { key: 'customerId', label: 'ID e Klientit', kind: 'number' },
  { key: 'age', label: 'Mosha', kind: 'number', min: 18, max: 100 },
  { key: 'job', label: 'Profesioni', kind: 'select', options: JOBS },
  { key: 'marital', label: 'Gjendja Civile', kind: 'select', options: MARITAL_OPTIONS },
  { key: 'education', label: 'Arsimimi', kind: 'select', options: EDUCATION_OPTIONS },
  { key: 'balance', label: 'Bilanci Bankar (€)', kind: 'number' },
  { key: 'housing', label: 'Kredi Banesore Aktive?', kind: 'select', options: YES_NO },
  { key: 'loan', label: 'Kredi Personale Aktive?', kind: 'select', options: YES_NO },
  { key: 'default', label: 'Histori Mospagimi?', kind: 'select', options: YES_NO },
  { key: 'duration', label: 'Kohëzgjatja e Thirrjes (sekonda)', kind: 'number', min: 0 },
  { key: 'campaign', label: 'Kontakte në këtë Fushatë', kind: 'number', min: 0 },
  { key: 'previous', label: 'Kontakte Paraprake (fushata të tjera)', kind: 'number', min: 0 },
  { key: 'contact', label: 'Mënyra e Kontaktit', kind: 'select', options: CONTACT_OPTIONS },
  { key: 'month', label: 'Muaji i Kontaktit', kind: 'select', options: MONTH_OPTIONS },
  { key: 'day', label: 'Dita e Muajit', kind: 'number', min: 1, max: 31 },
  { key: 'pDays', label: 'Ditë nga Kontakti i Fundit (-1 = asnjëherë)', kind: 'number', min: -1 },
  {
    key: 'pOutcome',
    label: 'Rezultati i Fushatës së Mëparshme',
    kind: 'select',
    options: POUTCOME_OPTIONS,
    wide: true,
  },
];

/**
 * Formulari për vlerësimin e një klienti të ri.
 *
 * Dërgon të dhënat në POST /api/v1/decisions/make-decision, ku modeli i AI-së
 * dhe motori i rregullave të biznesit gjenerojnë një vendim të ri në kohë reale.
 */
export const NewDecisionForm: FC<NewDecisionFormProps> = ({ nextCustomerId, onClose, onCreated }) => {
  const [form, setForm] = useState<FormState>({
    customerId: nextCustomerId,
    age: 35,
    job: 'management',
    marital: 'married',
    education: 'tertiary',
    balance: 5000,
    housing: 'no',
    loan: 'no',
    default: 'no',
    duration: 180,
    campaign: 1,
    previous: 0,
    contact: 'cellular',
    day: 15,
    month: 'may',
    pDays: -1,
    pOutcome: 'unknown',
  });
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<MakeDecisionResponse | null>(null);

  const updateField = (field: FieldDef, value: string) => {
    setForm(prev => ({
      ...prev,
      [field.key]: field.kind === 'select' ? value : Number(value),
    }));
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    const payload: MakeDecisionRequest = { ...form };

    try {
      const response = await apiFetch<MakeDecisionResponse>('v1/decisions/make-decision', {
        method: 'POST',
        body: JSON.stringify(payload),
      });
      setResult(response);
      onCreated();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Vlerësimi dështoi. Provo përsëri.');
    } finally {
      setSubmitting(false);
    }
  };

  const resetForNext = () => {
    setResult(null);
    setForm(prev => ({ ...prev, customerId: prev.customerId + 1 }));
  };

  return (
    <Dialog open onOpenChange={open => !open && onClose()}>
      <DialogContent className="max-w-xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-navy-900">
            <UserPlus className="w-5 h-5 text-navy-600" />
            Vlerëso Klient të Ri
          </DialogTitle>
          <DialogDescription>
            {result
              ? 'Vendimi u gjenerua dhe u ruajt në Regjistrin e Vendimeve.'
              : 'Plotëso të dhënat e klientit. Modeli i AI-së dhe rregullat e biznesit do të gjenerojnë një vendim të ri në kohë reale.'}
          </DialogDescription>
        </DialogHeader>

        {result ? (
          // Rezultati i vendimit të sapo krijuar
          <div className="space-y-4">
            <div
              className={cn(
                'rounded-lg p-4 border flex items-start gap-3',
                result.finalDecision ? 'bg-success-50 border-success-200' : 'bg-danger-50 border-danger-200'
              )}
            >
              {result.finalDecision ? (
                <CheckCircle2 className="w-5 h-5 text-success-600 flex-shrink-0" />
              ) : (
                <AlertCircle className="w-5 h-5 text-danger-600 flex-shrink-0" />
              )}
              <div>
                <p
                  className={cn(
                    'font-semibold text-base',
                    result.finalDecision ? 'text-success-700' : 'text-danger-700'
                  )}
                >
                  {result.finalDecision ? 'Kërkesa u Miratua' : 'Kërkesa u Refuzua'}
                </p>
                <p className="text-sm text-slate-600 mt-1">
                  AI-ja parashikoi {result.mlPrediction ? 'miratim' : 'refuzim'} me{' '}
                  <span className="font-semibold text-navy-900">{(result.mlConfidence * 100).toFixed(1)}%</span>{' '}
                  besueshmëri.
                  {result.wasOverridden && ' Rregullat e biznesit e ndryshuan vendimin final.'}
                </p>
                {result.finalDecision && (
                  <p className="text-sm text-slate-600 mt-1">
                    Norma e interesit:{' '}
                    <span className="font-semibold text-navy-900">
                      {(result.approvedInterestRate * 100).toFixed(2)}%
                    </span>
                  </p>
                )}
              </div>
            </div>

            <div className="text-xs text-muted-foreground bg-slate-50 p-3 rounded-lg border">
              <p className="font-medium text-slate-600 mb-1">Gjurma e Auditimit</p>
              <p className="whitespace-pre-wrap font-mono">{result.auditTrail}</p>
            </div>

            <DialogFooter className="gap-2 sm:gap-0">
              <Button variant="secondary" onClick={resetForNext}>
                Vlerëso Një Tjetër
              </Button>
              <Button onClick={onClose}>Mbyll dhe Shiko Panelin</Button>
            </DialogFooter>
          </div>
        ) : (
          // Formulari i të dhënave të klientit
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              {FIELDS.map(field => {
                const id = `field-${field.key}`;
                return (
                  <div key={field.key} className={cn('space-y-1.5', field.wide && 'col-span-2')}>
                    <Label htmlFor={id} className="text-xs font-semibold text-slate-600">
                      {field.label}
                    </Label>
                    {field.kind === 'number' ? (
                      <Input
                        id={id}
                        type="number"
                        min={field.min}
                        max={field.max}
                        value={form[field.key]}
                        onChange={e => updateField(field, e.target.value)}
                        required
                      />
                    ) : (
                      <Select value={String(form[field.key])} onValueChange={v => updateField(field, v)}>
                        <SelectTrigger id={id}>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {field.options.map(o => (
                            <SelectItem key={o.value} value={o.value}>
                              {o.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  </div>
                );
              })}
            </div>

            {error && (
              <div className="bg-danger-50 border border-danger-200 text-danger-700 text-sm rounded-lg p-3">
                {error}
              </div>
            )}

            <DialogFooter className="gap-2 sm:gap-0 pt-2">
              <Button type="button" variant="secondary" onClick={onClose}>
                Anulo
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting && <Loader2 className="animate-spin" />}
                {submitting ? 'Duke vlerësuar...' : 'Dërgo për Vlerësim'}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
};

export default NewDecisionForm;
