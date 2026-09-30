import { useState, useEffect, useCallback } from 'react';
import { DecisionRecord } from '../types';
import { apiFetch } from '../api/apiClient';

export const DEFAULT_PAGE_SIZE = 15;

/**
 * Filters for the decision register, applied server-side so they cover all
 * decisions, not only the current page. Percent values are 0-100 as typed by
 * the user; they are converted to fractions for the API.
 */
export interface DecisionFilters {
  customerId: string;
  finalDecision: '' | 'true' | 'false';
  wasOverridden: '' | 'true' | 'false';
  minProbability: string;
  maxProbability: string;
  minInterestRate: string;
  maxInterestRate: string;
}

export const EMPTY_FILTERS: DecisionFilters = {
  customerId: '',
  finalDecision: '',
  wasOverridden: '',
  minProbability: '',
  maxProbability: '',
  minInterestRate: '',
  maxInterestRate: '',
};

/** Totals over ALL decisions matching the filters (GET /api/v1/decisions/stats) */
export interface DecisionStats {
  total: number;
  mlApproved: number;
  approved: number;
  overridden: number;
  averageProbability: number;
  averageInterestRate: number;
  /** Overridden decisions per failed rule (a decision can fail several rules) */
  overridesByRule: { rule: string; count: number }[];
}

const toQuery = (filters: DecisionFilters): string => {
  const params = new URLSearchParams();
  const percent = (key: string, value: string) => {
    const n = parseFloat(value);
    if (!Number.isNaN(n)) params.set(key, String(n / 100));
  };

  if (/^\d+$/.test(filters.customerId.trim())) params.set('customerId', filters.customerId.trim());
  if (filters.finalDecision) params.set('finalDecision', filters.finalDecision);
  if (filters.wasOverridden) params.set('wasOverridden', filters.wasOverridden);
  percent('minProbability', filters.minProbability);
  percent('maxProbability', filters.maxProbability);
  percent('minInterestRate', filters.minInterestRate);
  percent('maxInterestRate', filters.maxInterestRate);

  const query = params.toString();
  return query ? `&${query}` : '';
};

/**
 * Return type for useDecisions hook
 */
interface UseDecisionsReturn {
  decisions: DecisionRecord[] | null;
  stats: DecisionStats | null;
  loading: boolean;
  error: string | null;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
  setPage: (page: number) => void;
  refetch: () => Promise<void>;
}

/**
 * Custom hook for fetching a page of decisions from the .NET Web API
 *
 * API Endpoint: GET /api/v1/decisions?page={page}&pageSize={pageSize}[&filters]
 * Handles loading, error states, and pagination
 */
export const useDecisions = (
  filters: DecisionFilters = EMPTY_FILTERS,
  pageSize: number = DEFAULT_PAGE_SIZE
): UseDecisionsReturn => {
  const [decisions, setDecisions] = useState<DecisionRecord[] | null>(null);
  const [stats, setStats] = useState<DecisionStats | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState<number>(1);
  const filterQuery = toQuery(filters);

  const fetchDecisions = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const [data, totals] = await Promise.all([
        apiFetch<DecisionRecord[]>(
          `v1/decisions?page=${page}&pageSize=${pageSize}${filterQuery}`,
          { method: 'GET' }
        ),
        apiFetch<DecisionStats>(`v1/decisions/stats?${filterQuery.slice(1)}`, { method: 'GET' }),
      ]);

      setDecisions(data);
      setStats(totals);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to fetch decisions';
      setError(errorMessage);
      console.error('Error fetching decisions:', err);
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, filterQuery]);

  useEffect(() => {
    fetchDecisions();
  }, [fetchDecisions]);

  return {
    decisions,
    stats,
    loading,
    error,
    page,
    pageSize,
    hasNextPage: (decisions?.length ?? 0) === pageSize,
    setPage,
    refetch: fetchDecisions,
  };
};

export default useDecisions;
