import axios from 'axios';
import type {
  ApiResponse,
  CardBatchSummary,
  CardImportResult,
  CardInventory,
  PagedResult,
  RechargeRequest,
  RechargeResponse,
  ReconcileResponse,
} from '../types';

const API_BASE_KEY = 'recharge_api_base_url';
const API_SECRET_KEY = 'recharge_api_secret_key';

export const getApiBaseUrl = (): string => {
  return localStorage.getItem(API_BASE_KEY) || 'http://localhost:5000/api';
};

export const setApiBaseUrl = (url: string): void => {
  localStorage.setItem(API_BASE_KEY, url);
};

export const getApiKey = (): string => {
  return localStorage.getItem(API_SECRET_KEY) || 'pos_super_secret_api_key_2026';
};

export const setApiKey = (key: string): void => {
  localStorage.setItem(API_SECRET_KEY, key);
};

const createApiClient = () => {
  const client = axios.create({
    baseURL: getApiBaseUrl(),
    headers: {
      'Content-Type': 'application/json',
    },
  });

  client.interceptors.request.use((config) => {
    config.baseURL = getApiBaseUrl();
    config.headers['X-Api-Key'] = getApiKey();
    return config;
  });

  return client;
};

const apiClient = createApiClient();

export const rechargeApi = {
  // Recharge endpoints
  processRecharge: async (data: RechargeRequest): Promise<ApiResponse<RechargeResponse>> => {
    const response = await apiClient.post<ApiResponse<RechargeResponse>>('/recharge', data);
    return response.data;
  },

  getTransaction: async (transactionId: string): Promise<ApiResponse<RechargeResponse>> => {
    const response = await apiClient.get<ApiResponse<RechargeResponse>>(`/recharge/${transactionId}`);
    return response.data;
  },

  getTransactions: async (params: {
    status?: string;
    operator?: string;
    mobileNumber?: string;
    fromDate?: string;
    toDate?: string;
    pageNumber?: number;
    pageSize?: number;
  }): Promise<ApiResponse<PagedResult<RechargeResponse>>> => {
    const response = await apiClient.get<ApiResponse<PagedResult<RechargeResponse>>>('/recharge', { params });
    return response.data;
  },

  reconcileTransaction: async (transactionId: string): Promise<ApiResponse<ReconcileResponse>> => {
    const response = await apiClient.post<ApiResponse<ReconcileResponse>>(`/recharge/${transactionId}/reconcile`);
    return response.data;
  },

  // Card endpoints
  importCardsCsv: async (file: File, importedBy = 'POS_AGENT'): Promise<ApiResponse<CardImportResult>> => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('importedBy', importedBy);

    const response = await apiClient.post<ApiResponse<CardImportResult>>('/cards/import', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  getCardInventory: async (): Promise<ApiResponse<CardInventory[]>> => {
    const response = await apiClient.get<ApiResponse<CardInventory[]>>('/cards/inventory');
    return response.data;
  },

  getCardBatches: async (): Promise<ApiResponse<CardBatchSummary[]>> => {
    const response = await apiClient.get<ApiResponse<CardBatchSummary[]>>('/cards/batches');
    return response.data;
  },

  // Analytics queries runner
  runAnalyticsQuery: async (queryName: string, startDate?: string, endDate?: string): Promise<ApiResponse<any>> => {
    const response = await apiClient.get<ApiResponse<any>>(`/analytics/queries/${queryName}`, {
      params: { startDate, endDate },
    });
    return response.data;
  },

  getAnalyticsSummary: async (): Promise<ApiResponse<any>> => {
    const response = await apiClient.get<ApiResponse<any>>('/analytics/summary');
    return response.data;
  },

  // Health check
  checkHealth: async (): Promise<boolean> => {
    try {
      const response = await axios.get('http://localhost:5000/', { timeout: 3000 });
      return response.status === 200;
    } catch {
      return false;
    }
  },
};
