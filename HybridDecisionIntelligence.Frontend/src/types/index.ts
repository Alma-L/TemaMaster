/**
 * Global type definitions for the application
 */

// API Response Types
export interface MLPredictionResult {
  id: number;
  customerId: number;
  predictedLabel: boolean;
  score: number;
  probability: number;
  createdAt: string;
}

export interface DecisionRecord {
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

export interface MakeDecisionRequest {
  customerId: number;
  age: number;
  job: string;
  marital: string;
  education: string;
  balance: number;
  housing: string;
  loan: string;
  default: string;
  duration: number;
  campaign: number;
  previous: number;
  contact: string;
  day: number;
  month: string;
  pDays: number;
  pOutcome: string;
}

export interface BankCustomer {
  id: number;
  age: number;
  job: string;
  marital: string;
  education: string;
  default: string;
  balance: number;
  housing: string;
  loan: string;
  contact: string;
  day: number;
  month: string;
  duration: number;
  campaign: number;
  pDays: number;
  previous: number;
  pOutcome: string;
  subscribedToTerm: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface MakeDecisionResponse {
  customerId: number;
  mlPrediction: boolean;
  mlConfidence: number;
  finalDecision: boolean;
  approvedInterestRate: number;
  wasOverridden: boolean;
  auditTrail: string;
  appliedRules: string[];
  decisionTime: string;
}

export interface BusinessRule {
  id: number;
  name: string;
  description: string;
  minBalance: number;
  minAge: number;
  maxAge: number;
  allowedJobs: string[];
  maxInterestRate: number;
  minInterestRate: number;
  isActive: boolean;
  createdAt: string;
}

export interface DashboardMetrics {
  totalDecisions: number;
  approvedCount: number;
  rejectedCount: number;
  overriddenCount: number;
  automationRate: number;
  aiPrecision: number;
  ruleEngineInterventionRate: number;
}

// UI Component Props
export interface DashboardProps {}

export interface DecisionDetailProps {
  decision: DecisionRecord;
  onClose: () => void;
}

// API Configuration
export interface ApiConfig {
  baseUrl: string;
  timeout: number;
  headers: Record<string, string>;
}

export const defaultApiConfig: ApiConfig = {
  baseUrl: process.env.REACT_APP_API_URL || '/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
};
