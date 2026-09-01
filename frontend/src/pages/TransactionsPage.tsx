import React, { useEffect, useState, useCallback } from 'react';
import {
  History,
  RefreshCw,
  Eye,
  X
} from 'lucide-react';
import { rechargeApi } from '../services/api';
import type { RechargeResponse } from '../types';
import { StatusBadge } from '../components/StatusBadge';

export const TransactionsPage: React.FC = () => {
  const [transactions, setTransactions] = useState<RechargeResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [autoRefresh, setAutoRefresh] = useState(false);

  // Filters
  const [statusFilter, setStatusFilter] = useState('');
  const [operatorFilter, setOperatorFilter] = useState('');
  const [mobileFilter, setMobileFilter] = useState('');
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 20;

  // Selected for inspection modal
  const [selectedTxn, setSelectedTxn] = useState<RechargeResponse | null>(null);
  const [reconcileLoadingId, setReconcileLoadingId] = useState<string | null>(null);

  const fetchTransactions = useCallback(async () => {
    setLoading(true);
    try {
      const response = await rechargeApi.getTransactions({
        status: statusFilter || undefined,
        operator: operatorFilter || undefined,
        mobileNumber: mobileFilter.trim() || undefined,
        pageNumber,
        pageSize,
      });

      if (response.success && response.data) {
        setTransactions(response.data.items);
        setTotalCount(response.data.totalCount);
      }
    } catch (err) {
      console.error('Failed to load transactions:', err);
    } finally {
      setLoading(false);
    }
  }, [statusFilter, operatorFilter, mobileFilter, pageNumber]);

  useEffect(() => {
    fetchTransactions();
  }, [fetchTransactions]);

  // Auto-refresh interval
  useEffect(() => {
    if (!autoRefresh) return;
    const interval = setInterval(() => {
      fetchTransactions();
    }, 8000);
    return () => clearInterval(interval);
  }, [autoRefresh, fetchTransactions]);

  const handleReconcileRow = async (transactionId: string) => {
    setReconcileLoadingId(transactionId);
    try {
      await rechargeApi.reconcileTransaction(transactionId);
      await fetchTransactions();
    } catch (err) {
      console.error('Reconciliation error:', err);
    } finally {
      setReconcileLoadingId(null);
    }
  };

  const handleInspect = async (transactionId: string) => {
    try {
      const response = await rechargeApi.getTransaction(transactionId);
      if (response.success && response.data) {
        setSelectedTxn(response.data);
      }
    } catch (err) {
      console.error('Failed to inspect transaction:', err);
    }
  };

  return (
    <div>
      {/* Header & Controls */}
      <div className="card-panel" style={{ marginBottom: '1.25rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <h2 className="card-title">
              <History size={20} color="#3b82f6" />
              <span>Recharge Transaction Log</span>
            </h2>
            <p className="card-subtitle">
              Live audit table of all processed, pending, and reconciled recharges ({totalCount} Total)
            </p>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            

            <button className="btn-secondary" onClick={() => fetchTransactions()} disabled={loading}>
              <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
              <span>Refresh</span>
            </button>
          </div>
        </div>

        {/* Filter Bar */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '0.75rem', marginTop: '1rem' }}>
          <div>
            <label className="form-label" style={{ marginBottom: '2px', fontSize: '0.75rem' }}>Status Filter</label>
            <select
              className="form-select"
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); setPageNumber(1); }}
            >
              <option value="">All Statuses</option>
              <option value="SUCCESS">SUCCESS</option>
              <option value="FAILED">FAILED</option>
              <option value="PENDING">PENDING (Timeout/Dropped)</option>
              <option value="PROCESSING">PROCESSING</option>
            </select>
          </div>

          <div>
            <label className="form-label" style={{ marginBottom: '2px', fontSize: '0.75rem' }}>Operator Filter</label>
            <select
              className="form-select"
              value={operatorFilter}
              onChange={(e) => { setOperatorFilter(e.target.value); setPageNumber(1); }}
            >
              <option value="">All Operators</option>
              <option value="Airtel">Airtel</option>
              <option value="Jio">Jio</option>
              <option value="Vi">Vi</option>
              <option value="BSNL">BSNL</option>
            </select>
          </div>

          <div>
            <label className="form-label" style={{ marginBottom: '2px', fontSize: '0.75rem' }}>Mobile Number Search</label>
            <div className="input-with-action">
              <input
                type="text"
                className="form-input"
                placeholder="Search 10-digit mobile..."
                value={mobileFilter}
                onChange={(e) => { setMobileFilter(e.target.value); setPageNumber(1); }}
              />
            </div>
          </div>
        </div>
      </div>

      {/* Transactions Table */}
      <div className="table-container">
        <table className="data-table">
          <thead>
            <tr>
              <th>Txn Reference ID</th>
              <th>Mobile</th>
              <th>Operator</th>
              <th>Amount</th>
              <th>Status</th>
              <th>Provider Reference</th>
              <th>Date (UTC)</th>
              <th style={{ textAlign: 'right' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {transactions.length === 0 && !loading && (
              <tr>
                <td colSpan={8} style={{ textAlign: 'center', padding: '2.5rem', color: 'var(--text-muted)' }}>
                  No recharge transactions found matching current filters.
                </td>
              </tr>
            )}

            {transactions.map((t) => (
              <tr key={t.id}>
                <td>
                  <span className="code-pill">{t.transactionId}</span>
                </td>
                <td style={{ fontWeight: 600 }}>+91 {t.mobileNumber}</td>
                <td>{t.operatorName || t.operator}</td>
                <td style={{ fontWeight: 700 }}>₹{t.amount}</td>
                <td>
                  <StatusBadge status={t.status} />
                </td>
                <td>
                  <span style={{ fontSize: '0.8rem', color: t.providerReference ? 'var(--text-primary)' : 'var(--text-muted)' }}>
                    {t.providerReference || '—'}
                  </span>
                </td>
                <td style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                  {new Date(t.createdDate).toLocaleString()}
                </td>
                <td style={{ textAlign: 'right' }}>
                  <div style={{ display: 'inline-flex', gap: '6px' }}>
                    {t.status === 'PENDING' && (
                      <button
                        className="btn-reconcile"
                        onClick={() => handleReconcileRow(t.transactionId)}
                        disabled={reconcileLoadingId === t.transactionId}
                        title="Query provider status enquiry"
                      >
                        <RefreshCw size={11} className={reconcileLoadingId === t.transactionId ? 'animate-spin' : ''} />
                        <span>{reconcileLoadingId === t.transactionId ? 'Rechecking...' : 'Recheck'}</span>
                      </button>
                    )}

                    <button
                      className="btn-action-small"
                      onClick={() => handleInspect(t.transactionId)}
                      title="View audit trail"
                    >
                      <Eye size={11} />
                      <span>History</span>
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalCount > pageSize && (
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '1rem' }}>
          <span style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
            Showing {((pageNumber - 1) * pageSize) + 1} to {Math.min(pageNumber * pageSize, totalCount)} of {totalCount} transactions
          </span>
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button
              className="btn-secondary"
              disabled={pageNumber === 1}
              onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
            >
              Previous
            </button>
            <button
              className="btn-secondary"
              disabled={pageNumber * pageSize >= totalCount}
              onClick={() => setPageNumber((p) => p + 1)}
            >
              Next
            </button>
          </div>
        </div>
      )}

      {/* Transaction Details & History Modal */}
      {selectedTxn && (
        <div className="modal-backdrop" onClick={() => setSelectedTxn(null)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <h3 style={{ fontSize: '1.1rem', fontWeight: 700 }}>Transaction Audit Trail</h3>
                <StatusBadge status={selectedTxn.status} />
              </div>
              <button
                onClick={() => setSelectedTxn(null)}
                style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
              >
                <X size={18} />
              </button>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', marginBottom: '1.25rem', fontSize: '0.85rem' }}>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Transaction ID:</span>
                <div className="code-pill" style={{ marginTop: '2px' }}>{selectedTxn.transactionId}</div>
              </div>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Mobile Number:</span>
                <div style={{ fontWeight: 600, marginTop: '2px' }}>+91 {selectedTxn.mobileNumber}</div>
              </div>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Operator:</span>
                <div style={{ fontWeight: 600, marginTop: '2px' }}>{selectedTxn.operatorName || selectedTxn.operator}</div>
              </div>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Amount:</span>
                <div style={{ fontWeight: 700, color: '#ffffff', marginTop: '2px' }}>₹{selectedTxn.amount}</div>
              </div>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Provider Reference:</span>
                <div className="code-pill" style={{ marginTop: '2px' }}>{selectedTxn.providerReference || 'N/A'}</div>
              </div>
              <div>
                <span style={{ color: 'var(--text-secondary)' }}>Created:</span>
                <div style={{ fontSize: '0.75rem', marginTop: '2px' }}>{new Date(selectedTxn.createdDate).toLocaleString()}</div>
              </div>
            </div>

            {selectedTxn.errorMessage && (
              <div style={{ marginBottom: '1.25rem', padding: '0.6rem', background: 'rgba(239, 68, 68, 0.1)', border: '1px solid #ef4444', borderRadius: '6px', fontSize: '0.8rem', color: '#f87171' }}>
                <strong>Error Details:</strong> {selectedTxn.errorMessage}
              </div>
            )}

            <div>
              <h4 style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginBottom: '0.5rem' }}>
                State Transition History (TransactionStatusHistory Table)
              </h4>
              <div className="timeline">
                {selectedTxn.history?.map((h) => (
                  <div key={h.id} className="timeline-item">
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <span style={{ fontWeight: 700 }}>{h.oldStatus ? `${h.oldStatus} → ${h.newStatus}` : h.newStatus}</span>
                      <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
                        {new Date(h.createdDate).toLocaleTimeString()}
                      </span>
                    </div>
                    {h.remarks && (
                      <p style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                        {h.remarks}
                      </p>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <div style={{ marginTop: '1.5rem', textAlign: 'right' }}>
              <button className="btn-secondary" onClick={() => setSelectedTxn(null)}>
                Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
