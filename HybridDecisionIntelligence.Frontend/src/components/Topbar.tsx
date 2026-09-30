import { useEffect, useState, type FC } from 'react';
import { BarChart3, UserPlus } from 'lucide-react';
import { apiFetch } from '../api/apiClient';
import type { DecisionStats } from '../hooks/useDecisions';
import { Button } from 'src/components/ui/button';
import { cn } from 'src/lib/utils';

interface TopbarProps {
  stats: DecisionStats | null;
  filtered: boolean;
  onNewDecision: () => void;
}

type ApiStatus = 'checking' | 'online' | 'offline';

/** Një etiketë e thjeshtë (jo e klikueshme) me vlerë dhe përshkrim */
const StatLabel: FC<{ value: string; label: string }> = ({ value, label }) => (
  <div className="leading-tight">
    <p className="text-sm font-semibold tabular-nums text-white">{value}</p>
    <p className="text-[11px] text-navy-300">{label}</p>
  </div>
);

/**
 * Shiriti i sipërm: marka, shifrat kryesore të sistemit (vetëm për lexim),
 * statusi i API-së dhe veprimi kryesor.
 */
export const Topbar: FC<TopbarProps> = ({ stats, filtered, onNewDecision }) => {
  const [apiStatus, setApiStatus] = useState<ApiStatus>('checking');

  // Kontrollo nëse API-ja përgjigjet, tani dhe çdo 30 sekonda
  useEffect(() => {
    let cancelled = false;
    const check = () =>
      apiFetch('v1/decisions/health', { method: 'GET' })
        .then(() => !cancelled && setApiStatus('online'))
        .catch(() => !cancelled && setApiStatus('offline'));
    check();
    const timer = setInterval(check, 30000);
    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  }, []);

  const total = stats?.total ?? 0;
  const rate = (n: number) => (total === 0 ? '0.0%' : `${((n / total) * 100).toFixed(1)}%`);

  return (
    <header className="sticky top-0 z-30 bg-navy-900 text-white">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center gap-6">
        {/* Marka */}
        <div className="flex items-center gap-3 min-w-0">
          <div className="w-9 h-9 rounded-lg bg-white/10 ring-1 ring-white/15 flex items-center justify-center flex-shrink-0">
            <BarChart3 className="w-[18px] h-[18px] text-white" />
          </div>
          <div className="min-w-0">
            <p className="text-sm font-semibold tracking-tight truncate">Paneli i Vendimeve Bankare</p>
            <p className="text-[11px] text-navy-300 truncate hidden sm:block">
              Vendimmarrje hibride · AI + Rregulla Biznesi
            </p>
          </div>
        </div>

        {/* Shifrat kryesore */}
        <div className="ml-auto hidden md:flex items-center gap-5 whitespace-nowrap">
          <StatLabel
            value={total.toLocaleString('sq-AL')}
            label={filtered ? 'vendime që përputhen' : 'vendime gjithsej'}
          />
          <div className="h-8 w-px bg-white/10" />
          <StatLabel value={rate(stats?.approved ?? 0)} label="shkalla e miratimit" />
          <div className="h-8 w-px bg-white/10" />
          <StatLabel value={rate(stats?.overridden ?? 0)} label="anuluar nga rregullat" />
          <div className="hidden lg:block h-8 w-px bg-white/10" />
          <span className="hidden lg:inline-flex items-center gap-2 text-xs text-navy-200">
            <span
              className={cn(
                'h-2 w-2 rounded-full',
                apiStatus === 'online' && 'bg-success-600 shadow-[0_0_0_3px_rgb(22_163_74/0.25)]',
                apiStatus === 'offline' && 'bg-danger-600 shadow-[0_0_0_3px_rgb(220_38_38/0.25)]',
                apiStatus === 'checking' && 'bg-navy-400'
              )}
            />
            {apiStatus === 'online' ? 'API aktive' : apiStatus === 'offline' ? 'API jashtë linje' : 'Duke kontrolluar…'}
          </span>
        </div>

        <Button
          onClick={onNewDecision}
          className="ml-auto md:ml-2 bg-white text-navy-900 hover:bg-navy-50 shadow-none"
        >
          <UserPlus />
          <span className="hidden sm:inline">Vlerëso Klient të Ri</span>
        </Button>
      </div>
    </header>
  );
};

export default Topbar;
