export interface RechargeRequest {
  transactionId: string;
  mobileNumber: string;
  operator: string;
  amount: number;
}

export interface StatusHistory {
  id: number;
  transactionId: string;
  oldStatus?: string;
  newStatus: string;
  remarks?: string;
  createdDate: string;
}

export interface RechargeResponse {
  id: number;
  transactionId: string;
  mobileNumber: string;
  operator: string;
  operatorName: string;
  amount: number;
  status: 'NEW' | 'PROCESSING' | 'SUCCESS' | 'FAILED' | 'PENDING';
  providerReference?: string;
  errorMessage?: string;
  createdDate: string;
  updatedDate: string;
  isDuplicate: boolean;
  history?: StatusHistory[];
}

export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data: T;
  errors?: any;
}

export interface PagedResult<T> {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  items: T[];
}

export interface ReconcileResponse {
  transactionId: string;
  previousStatus: string;
  currentStatus: string;
  providerReference?: string;
  message: string;
  reconciledAt: string;
}

export interface FailedCardRow {
  rowNumber: number;
  cardNumber?: string;
  serialNumber?: string;
  operator?: string;
  denomination?: string;
  expiryDate?: string;
  validationStatus: string;
  errorReason: string;
}

export interface CardImportResult {
  batchId: number;
  batchGuid: string;
  fileName: string;
  totalRows: number;
  imported: number;
  failed: number;
  duplicates: number;
  status: string;
  failedRows: FailedCardRow[];
}

export interface CardInventory {
  operatorCode: string;
  operatorName: string;
  denomination: number;
  availableCount: number;
  reservedCount: number;
  usedCount: number;
  expiredCount: number;
  blockedCount: number;
  totalCount: number;
}

export interface CardBatchSummary {
  batchId: number;
  batchGuid: string;
  fileName: string;
  totalRows: number;
  successfulRows: number;
  failedRows: number;
  duplicateRows: number;
  status: string;
  importedDate: string;
  importedBy: string;
}
