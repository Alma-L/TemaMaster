import { useState, useEffect, useCallback } from 'react';
import { DecisionRecord } from '../types';
import { apiFetch } from '../api/apiClient';

export const DEFAULT_PAGE_SIZE = 15;

/**
 * Return type for useDecisions hook
 */
interface UseDecisionsReturn {
  decisions: DecisionRecord[] | null;
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
 * API Endpoint: GET /api/v1/decisions?page={page}&pageSize={pageSize}
 * Handles loading, error states, and pagination
 */
export const useDecisions = (pageSize: number = DEFAULT_PAGE_SIZE): UseDecisionsReturn => {
  const [decisions, setDecisions] = useState<DecisionRecord[] | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState<number>(1);

  const fetchDecisions = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await apiFetch<DecisionRecord[]>(
        `v1/decisions?page=${page}&pageSize=${pageSize}`,
        { method: 'GET' }
      );

      setDecisions(data);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to fetch decisions';
      setError(errorMessage);
      console.error('Error fetching decisions:', err);
    } finally {
      setLoading(false);
    }
  }, [page, pageSize]);

  useEffect(() => {
    fetchDecisions();
  }, [fetchDecisions]);

  return {
    decisions,
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
