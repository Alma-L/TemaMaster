import { useState, useEffect } from 'react';
import { DecisionRecord } from '../types';

/**
 * Return type for useDecisions hook
 */
interface UseDecisionsReturn {
  decisions: DecisionRecord[] | null;
  loading: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}

/**
 * Custom hook for fetching decisions from the .NET Web API
 * 
 * API Endpoint: GET /api/v1/decisions
 * Handles loading, error states, and data transformation
 */
export const useDecisions = (): UseDecisionsReturn => {
  const [decisions, setDecisions] = useState<DecisionRecord[] | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const API_BASE_URL =
    process.env.REACT_APP_API_URL || 'https://localhost:7074/api';

  /**
   * Fetch decisions from backend API
   */
  const fetchDecisions = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_BASE_URL}/v1/decisions`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(
          `API Error: ${response.status} ${response.statusText}`
        );
      }

      const data: DecisionRecord[] = await response.json();
      setDecisions(data);
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : 'Failed to fetch decisions';
      setError(errorMessage);
      console.error('Error fetching decisions:', err);
    } finally {
      setLoading(false);
    }
  };

  // Fetch data on component mount
  useEffect(() => {
    fetchDecisions();

    // Optional: Poll for updated data every 10 seconds
    // Uncomment if you want real-time updates
    // const interval = setInterval(fetchDecisions, 10000);
    // return () => clearInterval(interval);
  }, []);

  return {
    decisions,
    loading,
    error,
    refetch: fetchDecisions,
  };
};

export default useDecisions;
