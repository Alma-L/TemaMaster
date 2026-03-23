import React, { useState } from 'react';
import {
  X,
  Lightbulb,
  Zap,
  CheckCircle2,
  AlertCircle,
  Clock,
  Layers,
} from 'lucide-react';

/**
 * Type for Decision Detail
 */
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
 * Decision Detail Modal Component
 * 
 * Visualizes the Thought-Action-Observation (TAO) framework
 * for explainable AI decision-making
 */
export const DecisionDetail: React.FC<DecisionDetailProps> = ({
  decision,
  onClose,
}) => {
  const [expandedSection, setExpandedSection] = useState<string | null>('thought');

  // Parse audit trail to extract decision components
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

  const toggleSection = (section: string) => {
    setExpandedSection(expandedSection === section ? null : section);
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-gradient-to-r from-indigo-600 to-blue-600 px-6 py-6 flex items-center justify-between border-b border-indigo-700">
          <div className="flex items-center gap-3">
            <Layers className="w-6 h-6 text-white" />
            <div>
              <h2 className="text-2xl font-bold text-white">Decision Analysis</h2>
              <p className="text-indigo-100 text-sm">
                Customer #{decision.customerId} • {new Date(decision.createdAt).toLocaleString()}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="text-white hover:bg-white hover:bg-opacity-20 p-2 rounded-lg transition"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Decision Outcome Badge */}
        <div className="px-6 py-4 bg-gray-50 border-b border-gray-200 flex items-center justify-between">
          <div>
            <p className="text-sm text-gray-600 font-medium">Final Decision</p>
            <p className="text-2xl font-bold text-gray-900 mt-1">
              {decision.finalDecision ? '✓ Approved' : '✗ Rejected'}
            </p>
          </div>
          <div className="text-right">
            <div className={`inline-flex items-center gap-2 px-4 py-2 rounded-lg font-semibold ${
              decision.finalDecision
                ? 'bg-green-100 text-green-800'
                : 'bg-red-100 text-red-800'
            }`}>
              {decision.finalDecision ? (
                <>
                  <CheckCircle2 className="w-5 h-5" />
                  APPROVED
                </>
              ) : (
                <>
                  <AlertCircle className="w-5 h-5" />
                  REJECTED
                </>
              )}
            </div>
            {decision.wasOverridden && (
              <div className="mt-2 inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-semibold bg-yellow-100 text-yellow-900">
                <AlertCircle className="w-3 h-3" />
                Rule Override
              </div>
            )}
          </div>
        </div>

        {/* TAO Framework Visualization */}
        <div className="px-6 py-6 space-y-4">
          {/* THOUGHT Section */}
          <div className="border border-gray-200 rounded-lg overflow-hidden">
            <button
              onClick={() => toggleSection('thought')}
              className="w-full px-4 py-4 bg-blue-50 hover:bg-blue-100 transition flex items-center justify-between"
            >
              <div className="flex items-center gap-3">
                <Lightbulb className="w-5 h-5 text-blue-600" />
                <div className="text-left">
                  <h3 className="font-bold text-gray-900">Thought</h3>
                  <p className="text-xs text-gray-600">ML.NET Prediction Analysis</p>
                </div>
              </div>
              <span className={`transform transition ${expandedSection === 'thought' ? 'rotate-180' : ''}`}>
                ▼
              </span>
            </button>
            {expandedSection === 'thought' && (
              <div className="px-4 py-4 bg-white border-t border-gray-200 space-y-3">
                <div className="bg-blue-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600 font-medium mb-2">ML Prediction</p>
                  <p className="text-lg font-bold text-blue-600">
                    {decision.mlPredicted ? '✓ APPROVE' : '✗ REJECT'}
                  </p>
                  <p className="text-xs text-gray-500 mt-1">
                    Based on ML.NET Binary Classification Model
                  </p>
                </div>

                <div className="bg-gradient-to-r from-blue-50 to-indigo-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600 font-medium mb-2">Confidence Score</p>
                  <div className="flex items-center gap-3">
                    <div className="flex-1">
                      <div className="w-full bg-gray-300 rounded-full h-3">
                        <div
                          className="bg-gradient-to-r from-blue-500 to-indigo-600 h-3 rounded-full transition-all"
                          style={{ width: `${decision.mlConfidence * 100}%` }}
                        ></div>
                      </div>
                    </div>
                    <span className="font-bold text-lg text-indigo-600 min-w-fit">
                      {(decision.mlConfidence * 100).toFixed(2)}%
                    </span>
                  </div>
                </div>

                <div className="text-xs text-gray-500 bg-gray-50 p-3 rounded border border-gray-200">
                  <p className="font-medium text-gray-700 mb-1">Raw Prediction Details</p>
                  <p>{auditComponents.mlPrediction || 'ML prediction component'}</p>
                </div>
              </div>
            )}
          </div>

          {/* ACTION Section */}
          <div className="border border-gray-200 rounded-lg overflow-hidden">
            <button
              onClick={() => toggleSection('action')}
              className="w-full px-4 py-4 bg-yellow-50 hover:bg-yellow-100 transition flex items-center justify-between"
            >
              <div className="flex items-center gap-3">
                <Zap className="w-5 h-5 text-yellow-600" />
                <div className="text-left">
                  <h3 className="font-bold text-gray-900">Action</h3>
                  <p className="text-xs text-gray-600">Expert Rule Engine Processing</p>
                </div>
              </div>
              <span className={`transform transition ${expandedSection === 'action' ? 'rotate-180' : ''}`}>
                ▼
              </span>
            </button>
            {expandedSection === 'action' && (
              <div className="px-4 py-4 bg-white border-t border-gray-200 space-y-3">
                <div className="bg-yellow-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600 font-medium mb-2">Business Rules Evaluation</p>
                  <p className="text-lg font-bold text-yellow-600">
                    {auditComponents.businessRules.includes('PASS') ? '✓ PASSED' : '✗ FAILED'}
                  </p>
                  <p className="text-xs text-gray-500 mt-1">
                    {auditComponents.businessRules || 'Business rules evaluation'}
                  </p>
                </div>

                <div className="bg-gradient-to-r from-yellow-50 to-orange-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600 font-medium mb-3">Applied Rules</p>
                  <div className="space-y-2">
                    {decision.rulesApplied.split(',').map((rule, idx) => (
                      <div
                        key={idx}
                        className="flex items-center gap-2 text-sm text-gray-700 bg-white px-3 py-2 rounded border border-yellow-200"
                      >
                        <span className="w-2 h-2 rounded-full bg-yellow-600"></span>
                        {rule.trim()}
                      </div>
                    ))}
                  </div>
                </div>

                {decision.wasOverridden && (
                  <div className="bg-red-50 p-4 rounded-lg border border-red-200">
                    <p className="text-sm text-red-700 font-medium mb-2">⚠️ Override Triggered</p>
                    <p className="text-sm text-red-600">
                      {decision.overrideReason || 'Business rule override applied'}
                    </p>
                  </div>
                )}

                <div className="text-xs text-gray-500 bg-gray-50 p-3 rounded border border-gray-200">
                  <p className="font-medium text-gray-700 mb-1">Rule Engine Details</p>
                  <p>{auditComponents.override || 'Rule evaluation details'}</p>
                </div>
              </div>
            )}
          </div>

          {/* OBSERVATION Section */}
          <div className="border border-gray-200 rounded-lg overflow-hidden">
            <button
              onClick={() => toggleSection('observation')}
              className="w-full px-4 py-4 bg-green-50 hover:bg-green-100 transition flex items-center justify-between"
            >
              <div className="flex items-center gap-3">
                <CheckCircle2 className="w-5 h-5 text-green-600" />
                <div className="text-left">
                  <h3 className="font-bold text-gray-900">Observation</h3>
                  <p className="text-xs text-gray-600">Final Decision & Logging</p>
                </div>
              </div>
              <span className={`transform transition ${expandedSection === 'observation' ? 'rotate-180' : ''}`}>
                ▼
              </span>
            </button>
            {expandedSection === 'observation' && (
              <div className="px-4 py-4 bg-white border-t border-gray-200 space-y-3">
                <div className="bg-green-50 p-4 rounded-lg">
                  <p className="text-sm text-gray-600 font-medium mb-2">Final Decision</p>
                  <p className="text-lg font-bold text-green-600">
                    {decision.finalDecision ? '✓ DECISION: APPROVED' : '✗ DECISION: REJECTED'}
                  </p>
                  <p className="text-xs text-gray-500 mt-1">
                    Integrated ML + Business Rules verdict
                  </p>
                </div>

                {decision.approvedInterestRate > 0 && (
                  <div className="bg-gradient-to-r from-green-50 to-emerald-50 p-4 rounded-lg">
                    <p className="text-sm text-gray-600 font-medium mb-2">Approved Interest Rate</p>
                    <p className="text-3xl font-bold text-green-600">
                      {(decision.approvedInterestRate * 100).toFixed(3)}%
                    </p>
                    <p className="text-xs text-gray-500 mt-1">
                      {auditComponents.interestRate || 'Interest rate calculation'}
                    </p>
                  </div>
                )}

                <div className="bg-gray-50 p-4 rounded-lg border border-gray-200 flex items-center gap-3">
                  <Clock className="w-5 h-5 text-gray-500" />
                  <div>
                    <p className="text-xs text-gray-600 font-medium">Decision Timestamp</p>
                    <p className="text-sm font-mono text-gray-800">
                      {new Date(decision.createdAt).toLocaleString()}
                    </p>
                  </div>
                </div>

                <div className="text-xs text-gray-600 bg-gray-50 p-3 rounded border border-gray-200">
                  <p className="font-medium text-gray-700 mb-2">Complete Audit Trail</p>
                  <p className="whitespace-pre-wrap font-mono text-gray-700">
                    {decision.auditTrail}
                  </p>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="sticky bottom-0 bg-gray-50 border-t border-gray-200 px-6 py-4 flex justify-end gap-3">
          <button
            onClick={onClose}
            className="px-4 py-2 bg-gray-200 text-gray-800 rounded-lg hover:bg-gray-300 transition font-medium"
          >
            Close
          </button>
          <button
            onClick={() => {
              const text = `Decision #${decision.id}\n\n${decision.auditTrail}`;
              navigator.clipboard.writeText(text);
              alert('Audit trail copied to clipboard!');
            }}
            className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition font-medium"
          >
            Copy Audit Trail
          </button>
        </div>
      </div>
    </div>
  );
};

export default DecisionDetail;
