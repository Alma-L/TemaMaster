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
  baseUrl: process.env.REACT_APP_API_URL || 'https://localhost:7074/api',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
};
