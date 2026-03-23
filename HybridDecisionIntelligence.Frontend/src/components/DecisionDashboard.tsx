import { useState, useEffect } from 'react';
import {
  CheckCircle,
  XCircle,
  AlertCircle,
  TrendingUp,
  Zap,
  BarChart3,
  Info,
  ChevronDown,
} from 'lucide-react';
import { DecisionDetail } from './DecisionDetail';
import { useDecisions } from '../hooks/useDecisions';
import { DecisionRecord } from '../types';

/**
 * Local component type definitions
 */
interface DashboardMetrics {
  totalDecisions: number;
  approvedCount: number;
  rejectedCount: number;
  overriddenCount: number;
  automationRate: number;
  aiPrecision: number;
  ruleEngineInterventionRate: number;
}

interface HoverContent {
  show: boolean;
  x: number;
  y: number;
  text: string;
}

/**
 * XAI Monitoring Dashboard Component
 * 
 * Phase 6 Implementation: Explainable AI Dashboard
 * Visualizes hybrid decision-making with ML predictions & business rule overrides
 */
export const DecisionDashboard: React.FC = () => {
  const { decisions, loading, error } = useDecisions();
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [selectedDecision, setSelectedDecision] = useState<DecisionRecord | null>(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [hoverContent, setHoverContent] = useState<HoverContent>({
    show: false,
    x: 0,
    y: 0,
    text: '',
  });

  // Calculate metrics from decisions
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
        aiPrecision: (approvedCount / totalDecisions) * 100,
        ruleEngineInterventionRate: (overriddenCount / totalDecisions) * 100,
      });
    }
  }, [decisions]);

  const getStatusColor = (decision: DecisionRecord): string => {
    if (decision.wasOverridden) {
      return 'bg-yellow-100 border-yellow-300 text-yellow-800';
    }
    return decision.finalDecision
      ? 'bg-green-100 border-green-300 text-green-800'
      : 'bg-red-100 border-red-300 text-red-800';
  };

  const getStatusIcon = (decision: DecisionRecord): React.ReactNode => {
    if (decision.wasOverridden) {
      return <AlertCircle className="w-4 h-4" />;
    }
    return decision.finalDecision ? (
      <CheckCircle className="w-4 h-4" />
    ) : (
      <XCircle className="w-4 h-4" />
    );
  };

  const getStatusLabel = (decision: DecisionRecord): string => {
    if (decision.wasOverridden) {
      return 'Overridden';
    }
    return decision.finalDecision ? 'Approved' : 'Rejected';
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

  // Loading state
  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto mb-4"></div>
          <p className="text-gray-600 font-medium">Loading decisions...</p>
        </div>
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <div className="flex items-center justify-center h-screen bg-gradient-to-br from-red-50 to-pink-100">
        <div className="bg-white rounded-lg shadow-lg p-8 max-w-md">
          <XCircle className="w-12 h-12 text-red-600 mx-auto mb-4" />
          <h2 className="text-xl font-bold text-gray-800 mb-2">Connection Error</h2>
          <p className="text-gray-600 mb-4">{error}</p>
          <button
            onClick={() => window.location.reload()}
            className="w-full bg-red-600 text-white py-2 rounded-lg hover:bg-red-700 transition"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-50 to-gray-100">
      {/* Header */}
      <div className="bg-white border-b border-gray-200 shadow-sm sticky top-0 z-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900">
                XAI Decision Dashboard
              </h1>
              <p className="text-gray-600 mt-1">
                Explainable AI Hybrid Decision Monitoring System
              </p>
            </div>
            <div className="text-right">
              <div className="text-4xl font-bold text-indigo-600">
                {metrics?.totalDecisions ?? 0}
              </div>
              <p className="text-gray-600 text-sm">Total Decisions</p>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Metrics Summary Cards */}
        {metrics && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            {/* Automation Rate Card */}
            <div className="bg-white rounded-lg shadow-md p-6 border-l-4 border-blue-500 hover:shadow-lg transition">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-gray-600 text-sm font-medium">Automation Rate</p>
                  <p className="text-3xl font-bold text-blue-600 mt-2">
                    {metrics.automationRate.toFixed(1)}%
                  </p>
                  <p className="text-gray-500 text-xs mt-2">
                    {metrics.totalDecisions - metrics.overriddenCount} automated decisions
                  </p>
                </div>
                <Zap className="w-12 h-12 text-blue-500 opacity-20" />
              </div>
            </div>

            {/* AI Precision Card */}
            <div className="bg-white rounded-lg shadow-md p-6 border-l-4 border-green-500 hover:shadow-lg transition">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-gray-600 text-sm font-medium">AI Precision</p>
                  <p className="text-3xl font-bold text-green-600 mt-2">
                    {metrics.aiPrecision.toFixed(1)}%
                  </p>
                  <p className="text-gray-500 text-xs mt-2">
                    {metrics.approvedCount} approvals
                  </p>
                </div>
                <TrendingUp className="w-12 h-12 text-green-500 opacity-20" />
              </div>
            </div>

            {/* Rule-Engine Intervention Rate Card */}
            <div className="bg-white rounded-lg shadow-md p-6 border-l-4 border-yellow-500 hover:shadow-lg transition">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-gray-600 text-sm font-medium">Rule Intervention %</p>
                  <p className="text-3xl font-bold text-yellow-600 mt-2">
                    {metrics.ruleEngineInterventionRate.toFixed(1)}%
                  </p>
                  <p className="text-gray-500 text-xs mt-2">
                    {metrics.overriddenCount} overridden decisions
                  </p>
                </div>
                <AlertCircle className="w-12 h-12 text-yellow-500 opacity-20" />
              </div>
            </div>
          </div>
        )}

        {/* Decision Table */}
        <div className="bg-white rounded-lg shadow-md overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center gap-2">
            <BarChart3 className="w-5 h-5 text-indigo-600" />
            <h2 className="text-xl font-bold text-gray-900">Decision Records</h2>
            <span className="ml-auto bg-indigo-100 text-indigo-700 text-xs font-medium px-3 py-1 rounded-full">
              {decisions?.length ?? 0} records
            </span>
          </div>

          {decisions && decisions.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="bg-gray-50 border-b border-gray-200">
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                      Customer ID
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                      ML Score
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                      Decision
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                      Interest Rate
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200">
                  {decisions.map((decision) => (
                    <tr
                      key={decision.id}
                      className="hover:bg-gray-50 transition cursor-pointer"
                      onClick={() => handleDecisionClick(decision)}
                    >
                      {/* Customer ID */}
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-sm font-medium bg-indigo-100 text-indigo-800">
                          #{decision.customerId}
                        </span>
                      </td>

                      {/* ML Score */}
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-2">
                          <div className="w-16 bg-gray-200 rounded-full h-2">
                            <div
                              className="bg-indigo-600 h-2 rounded-full transition-all"
                              style={{ width: `${decision.mlConfidence * 100}%` }}
                            ></div>
                          </div>
                          <span className="font-medium text-gray-900">
                            {(decision.mlConfidence * 100).toFixed(1)}%
                          </span>
                        </div>
                      </td>

                      {/* Final Decision */}
                      <td className="px-6 py-4 whitespace-nowrap">
                        <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${
                          decision.finalDecision
                            ? 'bg-green-100 text-green-800'
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {decision.finalDecision ? 'Approved' : 'Rejected'}
                        </span>
                      </td>

                      {/* Interest Rate */}
                      <td className="px-6 py-4 whitespace-nowrap text-gray-900 font-medium">
                        {decision.approvedInterestRate > 0
                          ? `${(decision.approvedInterestRate * 100).toFixed(2)}%`
                          : 'N/A'}
                      </td>

                      {/* Status with Override Badge */}
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-2">
                          <span
                            className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm font-medium border ${getStatusColor(
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
                                  decision.overrideReason || 'Business rule triggered'
                                )
                              }
                              onMouseLeave={handleTooltipLeave}
                              className="inline-block"
                            >
                              <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-semibold bg-yellow-200 text-yellow-900 cursor-help hover:bg-yellow-300 transition">
                                <Info className="w-3 h-3" />
                                Rule Triggered
                              </span>
                            </span>
                          )}
                        </div>
                      </td>

                      {/* Actions */}
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <button
                          className="text-indigo-600 hover:text-indigo-900 font-medium text-sm flex items-center gap-1 ml-auto hover:underline"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDecisionClick(decision);
                          }}
                        >
                          View Details
                          <ChevronDown className="w-4 h-4" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="px-6 py-12 text-center">
              <p className="text-gray-500">No decisions found. Run the API to generate data.</p>
            </div>
          )}
        </div>
      </div>

      {/* Tooltip */}
      {hoverContent.show && (
        <div
          className="fixed z-50 max-w-xs bg-gray-900 text-white text-sm rounded-lg shadow-lg p-3 pointer-events-none"
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
    </div>
  );
};

export default DecisionDashboard;
