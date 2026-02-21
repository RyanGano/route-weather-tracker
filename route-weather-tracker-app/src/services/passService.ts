import axios from 'axios';
import type { PassSummary } from '../types/passTypes';

// Aspire injects VITE_API_URL at runtime with the backend's service-discovered URL.
// Fallback to empty string so the Vite dev-server proxy can also be used.
const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? '';

const api = axios.create({ baseURL: BASE_URL });

export async function getAllPasses(): Promise<PassSummary[]> {
  const response = await api.get<PassSummary[]>('/api/passes');
  return response.data;
}

export async function getPassById(id: string): Promise<PassSummary> {
  const response = await api.get<PassSummary>(`/api/passes/${id}`);
  return response.data;
}
