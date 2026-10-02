import { useEffect, useState, type FC } from 'react';
import { Brain } from 'lucide-react';
import { apiFetch } from '../api/apiClient';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from 'src/components/ui/card';

/** Held-out evaluation written next to the model when it was trained (GET /api/v1/model/metrics) */
interface ModelMetrics {
  accuracy: number;
  areaUnderRocCurve: number;
  positivePrecision: number;
  positiveRecall: number;
  f1Score: number;
  trainTestSplit?: { testFraction: number };
}

/** Size of the held-out test set (20% of 45 211, TrainTestSplit seed 0): 9 092 customers, 1 100 subscribers */
const TEST_CUSTOMERS = 9092;
/** "9 092": a space every three digits, also for four-digit numbers (as in the thesis) */
const groupDigits = (n: number) => n.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ');

const METRICS: { key: keyof Omit<ModelMetrics, 'trainTestSplit'>; label: string; hint: string }[] = [
  { key: 'accuracy', label: 'Accuracy', hint: 'baza "gjithmonë jo": 87.9%' },
  { key: 'areaUnderRocCurve', label: 'AUC-ROC', hint: 'renditja e klientëve' },
  { key: 'positivePrecision', label: 'Precision', hint: 'sa nga të përzgjedhurit abonohen' },
  { key: 'positiveRecall', label: 'Recall', hint: 'sa nga abonentët gjenden' },
  { key: 'f1Score', label: 'F1-score', hint: 'ekuilibri P / R' },
];

/**
 * Performanca e modelit ML.NET mbi të dhënat e testimit (të pa para nga modeli).
 * Nuk shfaqet nëse modeli nuk ka metrika të ruajtura.
 */
export const ModelPerformanceCard: FC = () => {
  const [metrics, setMetrics] = useState<ModelMetrics | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiFetch<ModelMetrics>('v1/model/metrics', { method: 'GET' })
      .then(data => !cancelled && setMetrics(data))
      .catch(() => !cancelled && setMetrics(null));
    return () => {
      cancelled = true;
    };
  }, []);

  if (!metrics) return null;

  const testShare = Math.round((metrics.trainTestSplit?.testFraction ?? 0.2) * 100);

  return (
    <Card className="mb-8 shadow-none">
      <CardHeader className="p-5 pb-3 space-y-1">
        <CardTitle className="flex items-center gap-2 text-base text-navy-900">
          <Brain className="w-4 h-4 text-navy-600" />
          Performanca e Modelit ML.NET — bashkësia e testimit ({groupDigits(TEST_CUSTOMERS)} klientë)
        </CardTitle>
        <CardDescription>
          Vlerësuar mbi {testShare}% të të dhënave që modeli nuk i pa gjatë trajnimit; atributi
          "duration" është i përjashtuar.
        </CardDescription>
      </CardHeader>
      <CardContent className="p-5 pt-2 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        {METRICS.map(m => (
          <div key={m.key} className="rounded-lg border bg-slate-50 px-4 py-3">
            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{m.label}</p>
            <p className="text-xl font-bold text-navy-900 tabular-nums mt-1">
              {(metrics[m.key] * 100).toFixed(1)}%
            </p>
            <p className="text-[11px] text-slate-400 mt-0.5">{m.hint}</p>
          </div>
        ))}
      </CardContent>
    </Card>
  );
};

export default ModelPerformanceCard;
