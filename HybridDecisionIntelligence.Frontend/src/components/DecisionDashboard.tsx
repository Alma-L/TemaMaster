import { useState, useEffect } from 'react';
import {
  CheckCircle,
  XCircle,
  AlertCircle,
  TrendingUp,
  Zap,
  BarChart3,
  Info,
  ChevronRight,
  ChevronLeft,
  UserPlus,
} from 'lucide-react';
import { DecisionDetail } from './DecisionDetail';
import { NewDecisionForm } from './NewDecisionForm';
import { BusinessRulesPanel } from './BusinessRulesPanel';
import { useDecisions } from '../hooks/useDecisions';
import { DecisionRecord } from '../types';

interface DashboardMetrics {
  totalDecisions: number;
  approvedCount: number;
  rejectedCount: number;
  overriddenCount: number;
  automationRate: number;
  approvalRate: number;
  ruleEngineInterventionRate: number;
}

interface HoverContent {
  show: boolean;
  x: number;
  y: number;
  text: string;
}

/**
 * Paneli i Vendimeve Bankare (XAI Monitoring Dashboard)
 *
 * Shfaq vendimet e marra nga sistemi hibrid: parashikimi i modelit ML.NET
 * i kombinuar me rregullat e biznesit, së bashku me shpjegimin e plotë (XAI)
 * pas çdo vendimi.
 */
export const DecisionDashboard: React.FC = () => {
  const { decisions, loading, error, page, setPage, hasNextPage, refetch } = useDecisions();
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [selectedDecision, setSelectedDecision] = useState<DecisionRecord | null>(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [showNewDecisionForm, setShowNewDecisionForm] = useState(false);
  const [hoverContent, setHoverContent] = useState<HoverContent>({
    show: false,
    x: 0,
    y: 0,
    text: '',
  });

  // Llogarit metrikat nga faqja aktuale e vendimeve
  useEffect(() => {
    if (decisions && decisions.length > 0) {
      const totalDecisions = decisions.length;
      const approvedCount = decisions.filter(d => d.finalDecision).length;
      const rejectedCount = totalDecisions - approvedCount;
      const overriddenCount = decisions.filter(d => d.wasOverridden).length;

      setMetrics({
        totalDecisions,
        approvedCount,
        rejectedCount,
        overriddenCount,
        automationRate: ((totalDecisions - overriddenCount) / totalDecisions) * 100,
        approvalRate: (approvedCount / totalDecisions) * 100,
        ruleEngineInterventionRate: (overriddenCount / totalDecisions) * 100,
      });
    } else {
      setMetrics(null);
    }
  }, [decisions]);

  const getStatusColor = (decision: DecisionRecord): string => {
    if (decision.wasOverridden) {
      return 'bg-warning-50 border-warning-200 text-warning-700';
    }
    return decision.finalDecision
      ? 'bg-success-50 border-success-200 text-success-700'
      : 'bg-danger-50 border-danger-200 text-danger-700';
  };

  const getStatusIcon = (decision: DecisionRecord): React.ReactNode => {
    if (decision.wasOverridden) {
      return <AlertCircle className="w-3.5 h-3.5" />;
    }
    return decision.finalDecision ? (
      <CheckCircle className="w-3.5 h-3.5" />
    ) : (
      <XCircle className="w-3.5 h-3.5" />
    );
  };

  const getStatusLabel = (decision: DecisionRecord): string => {
    if (decision.wasOverridden) {
      return 'Anuluar nga Rregullat';
    }
    return decision.finalDecision ? 'Miratuar' : 'Refuzuar';
  };

  const handleDecisionClick = (decision: DecisionRecord) => {
    setSelectedDecision(decision);
    setShowDetailModal(true);
  };

  const handleTooltipHover = (
    e: React.MouseEvent<HTMLSpanElement>,
    text: string
  ) => {
    const rect = e.currentTarget.getBoundingClientRect();
    setHoverContent({
      show: true,
      x: rect.left,
      y: rect.bottom + 5,
      text,
    });
  };

  const handleTooltipLeave = () => {
    setHoverContent({ ...hoverContent, show: false });
  };

  // Gjendja e ngarkimit
  if (loading && !decisions) {
    return (
      <div className="flex items-center justify-center h-screen bg-slate-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-10 w-10 border-2 border-navy-100 border-t-navy-700 mx-auto mb-4"></div>
          <p className="text-slate-500 font-medium">Duke ngarkuar vendimet...</p>
        </div>
      </div>
    );
  }

  // Gjendja e gabimit
  if (error) {
    return (
      <div className="flex items-center justify-center h-screen bg-slate-50">
        <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-8 max-w-md text-center">
          <XCircle className="w-10 h-10 text-danger-600 mx-auto mb-4" />
          <h2 className="text-lg font-semibold text-navy-900 mb-2">Gabim në Lidhje</h2>
          <p className="text-slate-500 text-sm mb-6">{error}</p>
          <button
            onClick={() => window.location.reload()}
            className="w-full bg-navy-700 text-white py-2.5 rounded-lg font-medium hover:bg-navy-800 transition"
          >
            Provo Përsëri
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Header */}
      <div className="bg-white border-b border-slate-200 sticky top-0 z-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-5">
          <div className="flex items-center justify-between flex-wrap gap-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-lg bg-navy-800 flex items-center justify-center flex-shrink-0">
                <BarChart3 className="w-5 h-5 text-white" />
              </div>
              <div>
                <h1 className="text-xl font-semibold text-navy-900 tracking-tight">
                  Paneli i Vendimeve Bankare
                </h1>
                <p className="text-slate-500 text-sm">
                  Sistemi Hibrid i Vendimmarrjes me Inteligjencë Artificiale të Shpjegueshme
                </p>
              </div>
            </div>
            <div className="flex items-center gap-5">
              <div className="text-right">
                <div className="text-2xl font-bold text-navy-900 tabular-nums">
                  {decisions?.length ?? 0}
                  {hasNextPage && '+'}
                </div>
                <p className="text-slate-500 text-xs">Vendime në Faqen {page}</p>
              </div>
              <button
                onClick={() => setShowNewDecisionForm(true)}
                className="flex items-center gap-2 bg-navy-700 text-white px-4 py-2.5 rounded-lg text-sm font-medium hover:bg-navy-800 transition"
              >
                <UserPlus className="w-4 h-4" />
                Vlerëso Klient të Ri
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Rregullat reale të biznesit, marrë nga API-ja */}
        <BusinessRulesPanel />

        {/* Metrics Summary Cards */}
        {metrics && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-8">
            {/* Automation Rate Card */}
            <div className="bg-white rounded-xl border border-slate-200 p-5 border-l-[3px] border-l-navy-600">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-slate-500 text-xs font-medium uppercase tracking-wide">Shkalla e Automatizimit</p>
                  <p className="text-2xl font-bold text-navy-900 mt-1.5 tabular-nums">
                    {metrics.automationRate.toFixed(1)}%
                  </p>
                  <p className="text-slate-400 text-xs mt-1.5">
                    {metrics.totalDecisions - metrics.overriddenCount} vendime pa ndërhyrje
                  </p>
                </div>
                <div className="w-9 h-9 rounded-lg bg-navy-50 flex items-center justify-center flex-shrink-0">
                  <Zap className="w-4.5 h-4.5 text-navy-600" />
                </div>
              </div>
            </div>

            {/* Approval Rate Card */}
            <div className="bg-white rounded-xl border border-slate-200 p-5 border-l-[3px] border-l-success-600">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-slate-500 text-xs font-medium uppercase tracking-wide">Shkalla e Miratimit</p>
                  <p className="text-2xl font-bold text-navy-900 mt-1.5 tabular-nums">
                    {metrics.approvalRate.toFixed(1)}%
                  </p>
                  <p className="text-slate-400 text-xs mt-1.5">
                    {metrics.approvedCount} nga {metrics.totalDecisions} kërkesa
                  </p>
                </div>
                <div className="w-9 h-9 rounded-lg bg-success-50 flex items-center justify-center flex-shrink-0">
                  <TrendingUp className="w-4.5 h-4.5 text-success-600" />
                </div>
              </div>
            </div>

            {/* Rule-Engine Intervention Rate Card */}
            <div className="bg-white rounded-xl border border-slate-200 p-5 border-l-[3px] border-l-warning-600">
              <div className="flex items-start justify-between">
                <div>
                  <p className="text-slate-500 text-xs font-medium uppercase tracking-wide">Ndërhyrja e Rregullave</p>
                  <p className="text-2xl font-bold text-navy-900 mt-1.5 tabular-nums">
                    {metrics.ruleEngineInterventionRate.toFixed(1)}%
                  </p>
                  <p className="text-slate-400 text-xs mt-1.5">
                    {metrics.overriddenCount} vendime ndryshuan nga rregullat
                  </p>
                </div>
                <div className="w-9 h-9 rounded-lg bg-warning-50 flex items-center justify-center flex-shrink-0">
                  <AlertCircle className="w-4.5 h-4.5 text-warning-600" />
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Decision Table */}
        <div className="bg-white rounded-xl border border-slate-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center gap-2">
            <h2 className="text-base font-semibold text-navy-900">Regjistri i Vendimeve</h2>
            <span className="ml-auto bg-slate-100 text-slate-600 text-xs font-medium px-2.5 py-1 rounded-md">
              Faqja {page}
            </span>
          </div>

          {decisions && decisions.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200">
                    <th className="px-6 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      ID e Klientit
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      Probabiliteti i AI-së
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      Vendimi
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      Norma e Interesit
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      Statusi
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">
                      Veprime
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {decisions.map((decision) => (
                    <tr
                      key={decision.id}
                      className="hover:bg-slate-50 transition cursor-pointer"
                      onClick={() => handleDecisionClick(decision)}
                    >
                      {/* Customer ID */}
                      <td className="px-6 py-3.5 whitespace-nowrap">
                        <span className="inline-flex items-center px-2 py-0.5 rounded-md text-sm font-medium bg-slate-100 text-navy-800 tabular-nums">
                          #{decision.customerId}
                        </span>
                      </td>

                      {/* ML Score */}
                      <td className="px-6 py-3.5 whitespace-nowrap">
                        <div className="flex items-center gap-2">
                          <div className="w-16 bg-slate-200 rounded-full h-1.5">
                            <div
                              className="bg-navy-600 h-1.5 rounded-full transition-all"
                              style={{ width: `${decision.mlConfidence * 100}%` }}
                            ></div>
                          </div>
                          <span className="text-sm text-navy-900 tabular-nums">
                            {(decision.mlConfidence * 100).toFixed(1)}%
                          </span>
                        </div>
                      </td>

                      {/* Final Decision */}
                      <td className="px-6 py-3.5 whitespace-nowrap">
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-md text-sm font-medium ${
                          decision.finalDecision
                            ? 'bg-success-50 text-success-700'
                            : 'bg-danger-50 text-danger-700'
                        }`}>
                          {decision.finalDecision ? 'Miratuar' : 'Refuzuar'}
                        </span>
                      </td>

                      {/* Interest Rate */}
                      <td className="px-6 py-3.5 whitespace-nowrap text-navy-900 text-sm tabular-nums">
                        {decision.approvedInterestRate > 0
                          ? `${(decision.approvedInterestRate * 100).toFixed(2)}%`
                          : 'N/A'}
                      </td>

                      {/* Status with Override Badge */}
                      <td className="px-6 py-3.5 whitespace-nowrap">
                        <div className="flex items-center gap-2">
                          <span
                            className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-md text-xs font-medium border ${getStatusColor(
                              decision
                            )}`}
                          >
                            {getStatusIcon(decision)}
                            {getStatusLabel(decision)}
                          </span>
                          {decision.wasOverridden && (
                            <span
                              onMouseEnter={(e) =>
                                handleTooltipHover(
                                  e,
                                  decision.overrideReason || 'Është aktivizuar një rregull biznesi'
                                )
                              }
                              onMouseLeave={handleTooltipLeave}
                              className="inline-block"
                            >
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-medium bg-warning-100 text-warning-700 cursor-help hover:bg-warning-200 transition">
                                <Info className="w-3 h-3" />
                                Rregull i Aktivizuar
                              </span>
                            </span>
                          )}
                        </div>
                      </td>

                      {/* Actions */}
                      <td className="px-6 py-3.5 whitespace-nowrap text-right">
                        <button
                          className="text-navy-700 hover:text-navy-900 font-medium text-sm flex items-center gap-1 ml-auto hover:underline"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDecisionClick(decision);
                          }}
                        >
                          Shiko Detajet
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="px-6 py-12 text-center">
              <p className="text-slate-500 text-sm">Nuk u gjetën vendime. Ekzekuto API-në për të gjeneruar të dhëna.</p>
            </div>
          )}

          {/* Pagination */}
          {decisions && decisions.length > 0 && (
            <div className="px-6 py-3.5 border-t border-slate-200 flex items-center justify-between">
              <button
                onClick={() => setPage(Math.max(1, page - 1))}
                disabled={page <= 1 || loading}
                className="flex items-center gap-1 text-sm font-medium text-slate-600 hover:text-navy-900 disabled:opacity-40 disabled:cursor-not-allowed transition"
              >
                <ChevronLeft className="w-4 h-4" />
                Faqja Paraardhëse
              </button>
              <span className="text-sm text-slate-500">Faqja {page}</span>
              <button
                onClick={() => setPage(page + 1)}
                disabled={!hasNextPage || loading}
                className="flex items-center gap-1 text-sm font-medium text-slate-600 hover:text-navy-900 disabled:opacity-40 disabled:cursor-not-allowed transition"
              >
                Faqja Tjetër
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Tooltip */}
      {hoverContent.show && (
        <div
          className="fixed z-50 max-w-xs bg-navy-900 text-white text-sm rounded-lg shadow-lg p-3 pointer-events-none"
          style={{
            left: `${hoverContent.x}px`,
            top: `${hoverContent.y}px`,
          }}
        >
          <p>{hoverContent.text}</p>
        </div>
      )}

      {/* Decision Detail Modal */}
      {showDetailModal && selectedDecision && (
        <DecisionDetail
          decision={selectedDecision}
          onClose={() => setShowDetailModal(false)}
        />
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
