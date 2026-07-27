import { useState, type FC, type FormEvent } from 'react';
import { X, UserPlus, CheckCircle2, AlertCircle, Loader2 } from 'lucide-react';
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
} from '../utils/bankLabels';

interface NewDecisionFormProps {
  nextCustomerId: number;
  onClose: () => void;
  onCreated: () => void;
}

/**
 * Formulari për vlerësimin e një klienti të ri.
 *
 * Dërgon të dhënat në POST /api/v1/decisions/make-decision, ku modeli i AI-së
 * dhe motori i rregullave të biznesit gjenerojnë një vendim të ri në kohë reale.
 */
export const NewDecisionForm: FC<NewDecisionFormProps> = ({ nextCustomerId, onClose, onCreated }) => {
  const [form, setForm] = useState({
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

  const TEXT_FIELDS = ['job', 'marital', 'education', 'housing', 'loan', 'default', 'contact', 'month', 'pOutcome'];

  const updateField = (field: keyof typeof form, value: string) => {
    setForm(prev => ({
      ...prev,
      [field]: TEXT_FIELDS.includes(field) ? value : Number(value),
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
    <div className="fixed inset-0 bg-navy-950 bg-opacity-40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-2xl max-w-xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-navy-900 px-6 py-5 flex items-center justify-between z-10">
          <div className="flex items-center gap-3">
            <UserPlus className="w-5 h-5 text-navy-300" />
            <h2 className="text-lg font-semibold text-white">Vlerëso Klient të Ri</h2>
          </div>
          <button
            onClick={onClose}
            className="text-navy-300 hover:text-white hover:bg-white hover:bg-opacity-10 p-2 rounded-lg transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {result ? (
          // Rezultati i vendimit të sapo krijuar
          <div className="p-6 space-y-4">
            <div
              className={`rounded-lg p-4 border flex items-start gap-3 ${
                result.finalDecision
                  ? 'bg-success-50 border-success-200'
                  : 'bg-danger-50 border-danger-200'
              }`}
            >
              {result.finalDecision ? (
                <CheckCircle2 className="w-5 h-5 text-success-600 flex-shrink-0" />
              ) : (
                <AlertCircle className="w-5 h-5 text-danger-600 flex-shrink-0" />
              )}
              <div>
                <p className={`font-semibold text-base ${result.finalDecision ? 'text-success-700' : 'text-danger-700'}`}>
                  {result.finalDecision ? 'Kërkesa u Miratua' : 'Kërkesa u Refuzua'}
                </p>
                <p className="text-sm text-slate-600 mt-1">
                  AI-ja parashikoi {result.mlPrediction ? 'miratim' : 'refuzim'} me{' '}
                  <span className="font-semibold text-navy-900">{(result.mlConfidence * 100).toFixed(1)}%</span> besueshmëri.
                  {result.wasOverridden && ' Rregullat e biznesit e ndryshuan vendimin final.'}
                </p>
                {result.finalDecision && (
                  <p className="text-sm text-slate-600 mt-1">
                    Norma e interesit: <span className="font-semibold text-navy-900">{(result.approvedInterestRate * 100).toFixed(2)}%</span>
                  </p>
                )}
              </div>
            </div>

            <div className="text-xs text-slate-500 bg-slate-50 p-3 rounded-lg border border-slate-200">
              <p className="font-medium text-slate-600 mb-1">Gjurma e Auditimit</p>
              <p className="whitespace-pre-wrap font-mono">{result.auditTrail}</p>
            </div>

            <p className="text-sm text-slate-500">
              Ky vendim tani është shtuar te Regjistri i Vendimeve në panel — mund ta shohësh dhe klikosh mbi të për detaje të plota.
            </p>

            <div className="flex justify-end gap-3 pt-2">
              <button
                onClick={resetForNext}
                className="px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition text-sm font-medium"
              >
                Vlerëso Një Tjetër
              </button>
              <button
                onClick={onClose}
                className="px-4 py-2 bg-navy-700 text-white rounded-lg hover:bg-navy-800 transition text-sm font-medium"
              >
                Mbyll dhe Shiko Panelin
              </button>
            </div>
          </div>
        ) : (
          // Formulari i të dhënave të klientit
          <form onSubmit={handleSubmit} className="p-6 space-y-4">
            <p className="text-sm text-slate-500">
              Plotëso të dhënat e klientit. Modeli i AI-së dhe rregullat e biznesit do të gjenerojnë
              një vendim të ri në kohë reale.
            </p>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">ID e Klientit</label>
                <input
                  type="number"
                  value={form.customerId}
                  onChange={e => updateField('customerId', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                  required
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Mosha</label>
                <input
                  type="number"
                  min={18}
                  max={100}
                  value={form.age}
                  onChange={e => updateField('age', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Profesioni</label>
                <select
                  value={form.job}
                  onChange={e => updateField('job', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {JOBS.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Gjendja Civile</label>
                <select
                  value={form.marital}
                  onChange={e => updateField('marital', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {MARITAL_OPTIONS.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Arsimimi</label>
                <select
                  value={form.education}
                  onChange={e => updateField('education', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {EDUCATION_OPTIONS.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Bilanci Bankar (€)</label>
                <input
                  type="number"
                  value={form.balance}
                  onChange={e => updateField('balance', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Kredi Banesore Aktive?</label>
                <select
                  value={form.housing}
                  onChange={e => updateField('housing', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {YES_NO.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Kredi Personale Aktive?</label>
                <select
                  value={form.loan}
                  onChange={e => updateField('loan', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {YES_NO.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Histori Mospagimi?</label>
                <select
                  value={form.default}
                  onChange={e => updateField('default', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {YES_NO.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">
                  Kohëzgjatja e Thirrjes (sekonda)
                </label>
                <input
                  type="number"
                  min={0}
                  value={form.duration}
                  onChange={e => updateField('duration', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                  required
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">
                  Kontakte në këtë Fushatë
                </label>
                <input
                  type="number"
                  min={0}
                  value={form.campaign}
                  onChange={e => updateField('campaign', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">
                  Kontakte Paraprake (fushata të tjera)
                </label>
                <input
                  type="number"
                  min={0}
                  value={form.previous}
                  onChange={e => updateField('previous', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                  required
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Mënyra e Kontaktit</label>
                <select
                  value={form.contact}
                  onChange={e => updateField('contact', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {CONTACT_OPTIONS.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Muaji i Kontaktit</label>
                <select
                  value={form.month}
                  onChange={e => updateField('month', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {MONTH_OPTIONS.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">Dita e Muajit</label>
                <input
                  type="number"
                  min={1}
                  max={31}
                  value={form.day}
                  onChange={e => updateField('day', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                  required
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-600 mb-1">
                  Ditë nga Kontakti i Fundit (-1 = asnjëherë)
                </label>
                <input
                  type="number"
                  min={-1}
                  value={form.pDays}
                  onChange={e => updateField('pDays', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                  required
                />
              </div>

              <div className="col-span-2">
                <label className="block text-xs font-semibold text-slate-600 mb-1">
                  Rezultati i Fushatës së Mëparshme
                </label>
                <select
                  value={form.pOutcome}
                  onChange={e => updateField('pOutcome', e.target.value)}
                  className="w-full border border-slate-300 rounded-lg px-3 py-2 text-sm text-navy-900 focus:outline-none focus:ring-2 focus:ring-navy-200 focus:border-navy-500"
                >
                  {POUTCOME_OPTIONS.map(o => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>
            </div>

            {error && (
              <div className="bg-danger-50 border border-danger-200 text-danger-700 text-sm rounded-lg p-3">
                {error}
              </div>
            )}

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition text-sm font-medium"
              >
                Anulo
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="px-4 py-2 bg-navy-700 text-white rounded-lg hover:bg-navy-800 transition text-sm font-medium disabled:opacity-60 flex items-center gap-2"
              >
                {submitting && <Loader2 className="w-4 h-4 animate-spin" />}
                {submitting ? 'Duke vlerësuar...' : 'Dërgo për Vlerësim'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
};

export default NewDecisionForm;
