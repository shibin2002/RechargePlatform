import React, { useState } from 'react';
import {
  Smartphone,
  RefreshCw,
  HelpCircle,
  Clock,
  ShieldCheck,
  Send,
  Layers,
  AlertTriangle,
  Zap
} from 'lucide-react';
import { rechargeApi } from '../services/api';
import type { RechargeResponse } from '../types';
import { StatusBadge } from '../components/StatusBadge';

export const RechargePage: React.FC = () => {
  const [operator, setOperator] = useState('Airtel');
  const [mobileNumber, setMobileNumber] = useState('9876543210');
  const [amount, setAmount] = useState<number>(299);
  const [transactionId, setTransactionId] = useState(
    () => `TXN${Date.now().toString().slice(-6)}`
  );

  const [loading, setLoading] = useState(false);
  const [reconciling, setReconciling] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [result, setResult] = useState<RechargeResponse | null>(null);

  const quickAmounts = [100, 200, 300, 400, 500, 299, 499, 719];
  const operators = [
    { code: 'Airtel', name: 'Bharti Airtel', sub: 'Prepaid Plans' },
    { code: 'Jio', name: 'Reliance Jio', sub: 'True 5G Plans' },
    { code: 'Vi', name: 'Vodafone Idea', sub: 'Hero Unlimited' },
    { code: 'BSNL', name: 'BSNL Mobile', sub: 'National GSM' },
  ];

  const handleGenerateTxnId = () => {
    setTransactionId(`TXN${Date.now().toString().slice(-6)}`);
  };

  const handleRecharge = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    setLoading(true);

    try {
      const response = await rechargeApi.processRecharge({
        transactionId: transactionId.trim(),
        mobileNumber: mobileNumber.trim(),
        operator,
        amount: Number(amount),
      });

      if (response.success && response.data) {
        setResult(response.data);
      } else {
        setErrorMsg(response.message || 'Recharge processing failed.');
      }
    } catch (err: any) {
      const serverMsg = err.response?.data?.message || err.message;
      setErrorMsg(serverMsg || 'An error occurred during recharge submission.');
    } finally {
      setLoading(false);
    }
  };

  const handleReconcile = async () => {
    if (!result) return;
    setReconciling(true);
    setErrorMsg(null);
    try {
      const res = await rechargeApi.reconcileTransaction(result.transactionId);
      if (res.success) {
        // Refresh transaction
        const updated = await rechargeApi.getTransaction(result.transactionId);
        if (updated.success && updated.data) {
          setResult(updated.data);
        }
      }
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message || 'Reconciliation failed.');
    } finally {
      setReconciling(false);
    }
  };

  return (
    <div className="grid-two-col">
      {/* Left Column: POS Recharge Form */}
      <div className="card-panel">
        <div className="card-header">
          <div>
            <h2 className="card-title">
              <Smartphone size={20} color="#3b82f6" />
              <span>Mobile Recharge POS</span>
            </h2>
            <p className="card-subtitle">Retail terminal for prepaid plans & voucher activations</p>
          </div>
        </div>

        <form onSubmit={handleRecharge}>
          {/* Operator Selection */}
          <div className="form-group">
            <label className="form-label">
              <span>Select Telecom Operator</span>
              <span style={{ fontSize: '0.75rem', color: '#60a5fa' }}>4 Active Telcos</span>
            </label>
            <div className="operator-grid">
              {operators.map((op) => (
                <div
                  key={op.code}
                  className={`operator-card ${operator === op.code ? 'selected' : ''}`}
                  onClick={() => setOperator(op.code)}
                >
                  <span className="operator-name">{op.code}</span>
                  <span className="operator-sub">{op.sub}</span>
                </div>
              ))}
            </div>
          </div>

          {/* Mobile Number */}
          <div className="form-group">
            <label className="form-label">
              <span>Subscriber Mobile Number (10 Digits)</span>
              <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>+91 (India)</span>
            </label>
            <input
              type="tel"
              className="form-input"
              maxLength={10}
              placeholder="e.g. 9876543210"
              value={mobileNumber}
              onChange={(e) => setMobileNumber(e.target.value.replace(/\D/g, ''))}
              required
            />
          </div>

          {/* Amount Selection */}
          <div className="form-group">
            <label className="form-label">
              <span>Recharge Amount (INR)</span>
            </label>
            <div className="amount-chips">
              {quickAmounts.map((amt) => (
                <button
                  type="button"
                  key={amt}
                  className={`amount-chip ${amount === amt ? 'active' : ''}`}
                  onClick={() => setAmount(amt)}
                >
                  ₹{amt}
                </button>
              ))}
            </div>
            <input
              type="number"
              className="form-input"
              min={1}
              max={50000}
              placeholder="Custom amount in INR"
              value={amount}
              onChange={(e) => setAmount(Number(e.target.value))}
              required
            />
          </div>

          {/* Transaction ID with Override */}
          <div className="form-group">
            <label className="form-label">
              <span>Transaction Reference ID</span>
            </label>
            <div className="input-with-action">
              <input
                type="text"
                className="form-input"
                style={{ fontFamily: 'var(--font-mono)' }}
                value={transactionId}
                onChange={(e) => setTransactionId(e.target.value)}
                required
              />
              <button
                type="button"
                className="input-action-btn"
                onClick={handleGenerateTxnId}
                title="Generate new Transaction ID"
              >
                <RefreshCw size={14} />
              </button>
              
            </div>
          </div>

          {/* Error Message */}
          {errorMsg && (
            <div className="duplicate-alert" style={{ background: 'rgba(239, 68, 68, 0.15)', borderColor: '#ef4444', color: '#f87171' }}>
              <AlertTriangle size={16} />
              <span>{errorMsg}</span>
            </div>
          )}

          {/* Submit Button */}
          <button type="submit" className="btn-primary" disabled={loading}>
            {loading ? (
              <>
                <RefreshCw size={16} className="animate-spin" />
                <span>Processing Transaction & Provider Call...</span>
              </>
            ) : (
              <>
                <Send size={16} />
                <span>Execute Prepaid Recharge (₹{amount})</span>
              </>
            )}
          </button>
        </form>

        {/* Provider Simulation Rules Box */}
        <div className="simulation-box">
          <div className="simulation-title">
            <HelpCircle size={14} />
            <span>Mock Provider Simulation Matrix</span>
          </div>
          <div className="simulation-list">
            <div><span className="sim-chip">₹100</span> Instant SUCCESS</div>
            <div><span className="sim-chip">₹200</span> Instant FAILED</div>
            <div><span className="sim-chip">₹300</span> 15s Delay (Times out to PENDING)</div>
            <div><span className="sim-chip">₹400</span> Connection Drop (Forces PENDING)</div>
            <div><span className="sim-chip">₹500</span> Provider 500 Server Error</div>
            <div><span className="sim-chip">Other</span> Default Happy Path (SUCCESS)</div>
          </div>
        </div>
      </div>

      {/* Right Column: Transaction Outcome & Live Status Timeline */}
      <div className="card-panel">
        <div className="card-header">
          <div>
            <h2 className="card-title">
              <Zap size={20} color="#06b6d4" />
              <span>Transaction Result & History</span>
            </h2>
            <p className="card-subtitle">Real-time state verification and audit trail</p>
          </div>
          {result && <StatusBadge status={result.status} />}
        </div>

        {!result && !loading && (
          <div style={{ textAlign: 'center', padding: '3rem 1rem', color: 'var(--text-muted)' }}>
            <Layers size={40} style={{ opacity: 0.3, marginBottom: '0.75rem' }} />
            <p>No transaction submitted yet.</p>
            <p style={{ fontSize: '0.8rem', marginTop: '0.25rem' }}>
              Fill the POS form on the left and click <strong>Execute Prepaid Recharge</strong>.
            </p>
          </div>
        )}

        {loading && (
          <div style={{ textAlign: 'center', padding: '3rem 1rem' }}>
            <RefreshCw size={36} className="animate-spin" style={{ color: '#3b82f6', marginBottom: '1rem' }} />
            <p style={{ fontWeight: 600, color: 'var(--text-primary)' }}>DB Transaction Committed (PROCESSING)</p>
            <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.3rem' }}>
              Calling Mock Provider at <code>http://localhost:5005</code> with 10s timeout...
            </p>
          </div>
        )}

        {result && (
          <div>
            {/* Duplicate Flag Alert */}
            {result.isDuplicate && (
              <div className="duplicate-alert">
                <ShieldCheck size={18} />
                <div>
                  <strong>Duplicate Transaction Detected!</strong>
                  <div style={{ fontSize: '0.75rem' }}>
                    Re-submission of <code>{result.transactionId}</code> was caught by SQL Unique Constraint. Existing state returned without a second provider call.
                  </div>
                </div>
              </div>
            )}

            {/* Main Result Card */}
            <div className={`result-card ${result.status.toLowerCase()}`}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.75rem' }}>
                <div>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>TRANSACTION STATUS</span>
                  <div style={{ fontSize: '1.4rem', fontWeight: 800, marginTop: '2px' }}>
                    {result.status}
                  </div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>AMOUNT</span>
                  <div style={{ fontSize: '1.4rem', fontWeight: 800, color: 'var(--text-primary)', marginTop: '2px' }}>
                    ₹{result.amount}
                  </div>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.75rem', fontSize: '0.825rem' }}>
                <div>
                  <span style={{ color: 'var(--text-secondary)' }}>Transaction ID:</span>
                  <div className="code-pill" style={{ marginTop: '2px' }}>{result.transactionId}</div>
                </div>
                <div>
                  <span style={{ color: 'var(--text-secondary)' }}>Mobile Number:</span>
                  <div style={{ fontWeight: 600, marginTop: '2px' }}>+91 {result.mobileNumber}</div>
                </div>
                <div>
                  <span style={{ color: 'var(--text-secondary)' }}>Operator:</span>
                  <div style={{ fontWeight: 600, marginTop: '2px' }}>{result.operatorName || result.operator}</div>
                </div>
                <div>
                  <span style={{ color: 'var(--text-secondary)' }}>Provider Reference:</span>
                  <div className="code-pill" style={{ marginTop: '2px' }}>{result.providerReference || 'N/A'}</div>
                </div>
              </div>

              {result.errorMessage && (
                <div style={{ marginTop: '0.75rem', padding: '0.5rem', background: 'rgba(0,0,0,0.3)', borderRadius: '4px', fontSize: '0.8rem', color: '#f87171' }}>
                  <strong>Error / Reason:</strong> {result.errorMessage}
                </div>
              )}

              {/* Action for PENDING: Reconcile */}
              {result.status === 'PENDING' && (
                <div style={{ marginTop: '1rem', paddingTop: '0.75rem', borderTop: '1px solid var(--border-color)', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <span style={{ fontSize: '0.8rem', color: '#fbbf24' }}>
                    Transaction timed out or disconnected. Status enquiry can confirm result.
                  </span>
                  <button
                    className="btn-reconcile"
                    onClick={handleReconcile}
                    disabled={reconciling}
                  >
                    {reconciling ? <RefreshCw size={12} className="animate-spin" /> : <Clock size={12} />}
                    <span>{reconciling ? 'Reconciling...' : 'Recheck with Provider'}</span>
                  </button>
                </div>
              )}
            </div>

            {/* Status Transition History Stepper */}
            <div style={{ marginTop: '1.5rem' }}>
              <h3 style={{ fontSize: '0.9rem', fontWeight: 600, marginBottom: '0.5rem', color: 'var(--text-secondary)' }}>
                Status Transition Audit Trail (TransactionStatusHistory)
              </h3>
              
              {result.history && result.history.length > 0 ? (
                <div className="timeline">
                  {result.history.map((h, idx) => (
                    <div key={h.id || idx} className="timeline-item">
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                        <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{h.oldStatus ? `${h.oldStatus} → ${h.newStatus}` : h.newStatus}</span>
                        <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
                          {new Date(h.createdDate).toLocaleTimeString()}
                        </span>
                      </div>
                      {h.remarks && (
                        <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)', marginTop: '1px' }}>
                          {h.remarks}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                  History recorded: NEW → PROCESSING → {result.status}
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
