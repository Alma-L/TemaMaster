import { useState, useEffect } from 'react';
import {
  CheckCircle,
  XCircle,
  AlertCircle,
  TrendingUp,
  Brain,
  Shield,
  ChevronDown,
  ChevronUp,
  Lightbulb,
} from 'lucide-react';
import { useDecisions } from '../hooks/useDecisions';
import { DecisionRecord } from '../types';

/**
 * XAI Dashboard Component
 * 
 * Phase 6: Explainable AI - Thought-Action-Observation Cycle
 * Displays hybrid decision-making with ML predictions & business rule overrides
 */
export const XaiDashboard: React.FC = () => {
  const { decisions, loading, error } = useDecisions();
  const [expandedRow, setExpandedRow] = useState<string | null>(null);
  const [filterStatus, setFilterStatus] = useState<'all' | 'approved' | 'rejected'>('all');

  // Filter decisions based on status
  const filteredDecisions = decisions.filter((decision) => {
    if (filterStatus === 'all') return true;
    if (filterStatus === 'approved') return decision.finalDecision === 'Approved';
    if (filterStatus === 'rejected') return decision.finalDecision === 'Rejected';
    return true;
  });

  // Calculate metrics
  const metrics = {
    total: decisions.length,
    approved: decisions.filter((d) => d.finalDecision === 'Approved').length,
    rejected: decisions.filter((d) => d.finalDecision === 'Rejected').length,
    overridden: decisions.filter((d) => d.wasOverridden).length,
  };

  const overrideRate = metrics.total > 0 ? ((metrics.overridden / metrics.total) * 100).toFixed(2) : '0.00';

  if (error) {
    return (
      <div className="flex items-center justify-center h-screen bg-red-50">
        <div className="text-center">
          <AlertCircle className="mx-auto text-red-500 mb-4" size={48} />
          <h1 className="text-2xl font-bold text-red-800">Error Loading Dashboard</h1>
          <p className="text-red-600 mt-2">{error}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 p-8">
      {/* Header */}
      <div className="max-w-7xl mx-auto">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h1 className="text-4xl font-bold text-white flex items-center gap-3">
              <Brain className="text-blue-400" size={40} />
              Explainable AI Decision Dashboard
            </h1>
            <p className="text-slate-400 mt-2">Thought-Action-Observation Cycle: AI Predictions & Expert Override Rules</p>
          </div>
        </div>

        {/* Metrics Cards */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
          {/* Total Decisions */}
          <div className="bg-slate-700 rounded-lg p-6 border border-slate-600">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-slate-400 text-sm uppercase tracking-wide">Total Decisions</p>
                <p className="text-3xl font-bold text-white mt-2">{metrics.total}</p>
              </div>
              <TrendingUp className="text-blue-400" size={36} />
            </div>
          </div>

          {/* Approved */}
          <div className="bg-slate-700 rounded-lg p-6 border border-green-500/30">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-slate-400 text-sm uppercase tracking-wide">Approved</p>
                <p className="text-3xl font-bold text-green-400 mt-2">{metrics.approved}</p>
              </div>
              <CheckCircle className="text-green-400" size={36} />
            </div>
          </div>

          {/* Rejected */}
          <div className="bg-slate-700 rounded-lg p-6 border border-red-500/30">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-slate-400 text-sm uppercase tracking-wide">Rejected</p>
                <p className="text-3xl font-bold text-red-400 mt-2">{metrics.rejected}</p>
              </div>
              <XCircle className="text-red-400" size={36} />
            </div>
          </div>

          {/* Override Rate */}
          <div className="bg-slate-700 rounded-lg p-6 border border-yellow-500/30">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-slate-400 text-sm uppercase tracking-wide">Override Rate</p>
                <p className="text-3xl font-bold text-yellow-400 mt-2">{overrideRate}%</p>
              </div>
              <Shield className="text-yellow-400" size={36} />
            </div>
          </div>
        </div>

        {/* Filter Buttons */}
        <div className="flex gap-3 mb-6">
          <button
            onClick={() => setFilterStatus('all')}
            className={`px-4 py-2 rounded-lg font-medium transition-all ${
              filterStatus === 'all'
                ? 'bg-blue-500 text-white'
                : 'bg-slate-700 text-slate-400 hover:bg-slate-600'
            }`}
          >
            All Decisions
          </button>
          <button
            onClick={() => setFilterStatus('approved')}
            className={`px-4 py-2 rounded-lg font-medium transition-all ${
              filterStatus === 'approved'
                ? 'bg-green-500 text-white'
                : 'bg-slate-700 text-slate-400 hover:bg-slate-600'
            }`}
          >
            Approved
          </button>
          <button
            onClick={() => setFilterStatus('rejected')}
            className={`px-4 py-2 rounded-lg font-medium transition-all ${
              filterStatus === 'rejected'
                ? 'bg-red-500 text-white'
                : 'bg-slate-700 text-slate-400 hover:bg-slate-600'
            }`}
          >
            Rejected
          </button>
        </div>

        {/* Decision History Table */}
        <div className="bg-slate-700 rounded-lg border border-slate-600 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-800 border-b border-slate-600">
                <tr>
                  <th className="px-6 py-4 text-left text-sm font-semibold text-slate-300 uppercase tracking-wide">
                    Customer ID
                  </th>
                  <th className="px-6 py-4 text-left text-sm font-semibold text-slate-300 uppercase tracking-wide">
                    ML Probability
                  </th>
                  <th className="px-6 py-4 text-left text-sm font-semibold text-slate-300 uppercase tracking-wide">
                    AI Decision
                  </th>
                  <th className="px-6 py-4 text-left text-sm font-semibold text-slate-300 uppercase tracking-wide">
                    Final Decision
                  </th>
                  <th className="px-6 py-4 text-left text-sm font-semibold text-slate-300 uppercase tracking-wide">
                    Status
                  </th>
                  <th className="px-6 py-4 text-center text-sm font-semibold text-slate-300 uppercase tracking-wide">
                    Details
                  </th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr>
                    <td colSpan={6} className="px-6 py-8 text-center text-slate-400">
                      <div className="flex items-center justify-center gap-3">
                        <div className="animate-spin rounded-full h-6 w-6 border border-blue-400 border-t-blue-200"></div>
                        Loading decisions...
                      </div>
                    </td>
                  </tr>
                ) : filteredDecisions.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-6 py-8 text-center text-slate-400">
                      No decisions found.
                    </td>
                  </tr>
                ) : (
                  filteredDecisions.map((decision) => (
                    <tbody key={decision.id}>
                      <tr className="border-t border-slate-600 hover:bg-slate-600/50 transition-colors">
                        <td className="px-6 py-4 text-sm font-medium text-white">{decision.customerId}</td>
                        <td className="px-6 py-4 text-sm text-slate-300">
                          <div className="flex items-center gap-2">
                            <div className="flex-1">
                              <div className="w-16 h-2 bg-slate-600 rounded-full overflow-hidden">
                                <div
                                  className="h-full bg-gradient-to-r from-blue-500 to-blue-400"
                                  style={{ width: `${(decision.mlProbability || 0) * 100}%` }}
                                ></div>
                              </div>
                            </div>
                            <span className="font-mono">{((decision.mlProbability || 0) * 100).toFixed(1)}%</span>
                          </div>
                        </td>
                        <td className="px-6 py-4 text-sm">
                          <span
                            className={`px-3 py-1 rounded-full text-xs font-semibold ${
                              decision.aiDecision === 'Approved'
                                ? 'bg-green-500/20 text-green-300'
                                : 'bg-red-500/20 text-red-300'
                            }`}
                          >
                            {decision.aiDecision}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-sm">
                          <div className="flex items-center gap-2">
                            {decision.finalDecision === 'Approved' ? (
                              <CheckCircle className="text-green-400" size={18} />
                            ) : (
                              <XCircle className="text-red-400" size={18} />
                            )}
                            <span
                              className={`font-semibold ${
                                decision.finalDecision === 'Approved'
                                  ? 'text-green-400'
                                  : 'text-red-400'
                              }`}
                            >
                              {decision.finalDecision}
                            </span>
                          </div>
                        </td>
                        <td className="px-6 py-4 text-sm">
                          {decision.wasOverridden ? (
                            <span className="px-3 py-1 rounded-full text-xs font-semibold bg-yellow-500/20 text-yellow-300 flex items-center gap-1 w-fit">
                              <AlertCircle size={14} />
                              Rule Override
                            </span>
                          ) : (
                            <span className="px-3 py-1 rounded-full text-xs font-semibold bg-slate-600 text-slate-300">
                              Auto Approved
                            </span>
                          )}
                        </td>
                        <td className="px-6 py-4 text-center">
                          {decision.wasOverridden && (
                            <button
                              onClick={() =>
                                setExpandedRow(expandedRow === decision.id ? null : decision.id)
                              }
                              className="inline-flex items-center justify-center p-2 rounded-lg hover:bg-slate-600 transition-colors text-slate-400 hover:text-slate-200"
                            >
                              {expandedRow === decision.id ? (
                                <ChevronUp size={20} />
                              ) : (
                                <ChevronDown size={20} />
                              )}
                            </button>
                          )}
                        </td>
                      </tr>

                      {/* XAI Explanation Row */}
                      {expandedRow === decision.id && decision.wasOverridden && (
                        <tr className="border-t border-slate-600 bg-slate-600/30">
                          <td colSpan={6} className="px-6 py-6">
                            <div className="bg-slate-700 rounded-lg p-6 border border-yellow-500/50">
                              {/* Header */}
                              <div className="flex items-start gap-3 mb-4">
                                <Lightbulb className="text-yellow-400 flex-shrink-0 mt-1" size={24} />
                                <div>
                                  <h3 className="text-lg font-bold text-white mb-1">Expert Rule Override</h3>
                                  <p className="text-slate-400 text-sm">
                                    Explanation of the Thought-Action-Observation Cycle
                                  </p>
                                </div>
                              </div>

                              {/* Thought - AI Decision */}
                              <div className="mb-4 p-4 bg-slate-800 rounded-lg border border-blue-500/30">
                                <h4 className="text-blue-300 font-semibold text-sm uppercase tracking-wide mb-2">
                                  🧠 Thought (AI Analysis)
                                </h4>
                                <p className="text-slate-300">
                                  Machine Learning model evaluated the customer with{' '}
                                  <span className="font-bold text-blue-400">
                                    {((decision.mlProbability || 0) * 100).toFixed(1)}% confidence
                                  </span>
                                  . Recommendation: <span className="font-bold text-blue-400">{decision.aiDecision}</span>
                                </p>
                              </div>

                              {/* Action - Rule Engine Override */}
                              <div className="mb-4 p-4 bg-slate-800 rounded-lg border border-yellow-500/30">
                                <h4 className="text-yellow-300 font-semibold text-sm uppercase tracking-wide mb-2">
                                  ⚡ Action (Rule Engine Override)
                                </h4>
                                <p className="text-slate-300">
                                  Business rule engine evaluated specialized policies and determined the final decision.{' '}
                                  <span className="font-bold text-yellow-400">
                                    {decision.overrideReason || 'Interest rate rules or regulatory compliance policies were applied'}
                                  </span>
                                </p>
                              </div>

                              {/* Observation - Final Verdict */}
                              <div className="p-4 bg-slate-800 rounded-lg border border-green-500/30">
                                <h4 className="text-green-300 font-semibold text-sm uppercase tracking-wide mb-2">
                                  ✓ Observation (Final Verdict)
                                </h4>
                                <p className="text-slate-300">
                                  Final Decision: <span className="font-bold text-green-400">{decision.finalDecision}</span>
                                </p>
                                <p className="text-slate-400 text-sm mt-2">
                                  This decision was overridden due to specialized business rules that supersede the ML recommendation to ensure compliance and risk management.
                                </p>
                              </div>
                            </div>
                          </td>
                        </tr>
                      )}
                    </tbody>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Footer */}
        <div className="mt-8 text-center text-slate-400 text-sm">
          <p>
            Phase 6: Explainable AI Dashboard • {metrics.total} total decisions analyzed •{' '}
            {((metrics.approved / (metrics.total || 1)) * 100).toFixed(1)}% approval rate
          </p>
        </div>
      </div>
    </div>
  );
};
