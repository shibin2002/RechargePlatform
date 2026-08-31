import React, { useEffect, useState } from 'react';
import { Layers, RefreshCw, ShieldCheck, Zap, AlertCircle, CheckCircle2 } from 'lucide-react';
import axios from 'axios';
import { getApiBaseUrl, getApiKey, rechargeApi } from '../services/api';
import type { CardInventory } from '../types';

export const CardInventoryPage: React.FC = () => {
  const [inventory, setInventory] = useState<CardInventory[]>([]);
  const [loading, setLoading] = useState(false);

  // Atomic reservation testing state
  const [reserveCardNumber, setReserveCardNumber] = useState('');
  const [reserveTxnId, setReserveTxnId] = useState(() => `RSV${Date.now().toString().slice(-6)}`);
  const [reserveResult, setReserveResult] = useState<any | null>(null);
  const [reserveLoading, setReserveLoading] = useState(false);
  const [reserveError, setReserveError] = useState<string | null>(null);

  const fetchInventory = async () => {
    setLoading(true);
    try {
      const response = await rechargeApi.getCardInventory();
      if (response.success && response.data) {
        setInventory(response.data);
      }
    } catch (err) {
      console.error('Failed to load inventory:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchInventory();
  }, []);

  const handleAtomicReserve = async (e: React.FormEvent) => {
    e.preventDefault();
    setReserveLoading(true);
    setReserveError(null);
    setReserveResult(null);

    try {
      const baseUrl = getApiBaseUrl();
      const apiKey = getApiKey();

      const reserveResp = await axios.post(
        `${baseUrl}/cards/reserve`,
        {
          cardNumber: reserveCardNumber.trim(),
          transactionId: reserveTxnId.trim(),
        },
        {
          headers: { 'X-Api-Key': apiKey },
        }
      );

      if (reserveResp.data?.success) {
        setReserveResult(reserveResp.data.data);
        fetchInventory();
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || err.message;
      setReserveError(msg || 'Card reservation failed (Card not available or already claimed).');
    } finally {
      setReserveLoading(false);
    }
  };

  const totalAvailable = inventory.reduce((acc, i) => acc + i.availableCount, 0);
  const totalReserved = inventory.reduce((acc, i) => acc + i.reservedCount, 0);
  const totalUsed = inventory.reduce((acc, i) => acc + i.usedCount, 0);
  const totalStock = inventory.reduce((acc, i) => acc + i.totalCount, 0);

  return (
    <div>
      {/* Top Metrics */}
      <div className="metrics-grid">
        <div className="metric-card" style={{ borderColor: 'rgba(16, 185, 129, 0.4)' }}>
          <div className="metric-label" style={{ color: '#34d399' }}>Available Stock</div>
          <div className="metric-val" style={{ color: '#34d399' }}>{totalAvailable}</div>
        </div>
        <div className="metric-card" style={{ borderColor: 'rgba(245, 158, 11, 0.4)' }}>
          <div className="metric-label" style={{ color: '#fbbf24' }}>Reserved Cards</div>
          <div className="metric-val" style={{ color: '#fbbf24' }}>{totalReserved}</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Used / Redeemed</div>
          <div className="metric-val">{totalUsed}</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Total Voucher Inventory</div>
          <div className="metric-val">{totalStock}</div>
        </div>
      </div>

      <div className="grid-two-col">
        {/* Left Column: Inventory Matrix */}
        <div className="card-panel">
          <div className="card-header">
            <div>
              <h2 className="card-title">
                <Layers size={20} color="#3b82f6" />
                <span>Card Stock Matrix (Operator + Denomination)</span>
              </h2>
              <p className="card-subtitle">Aggregated stock availability across all active telcos</p>
            </div>
            <button className="btn-secondary" onClick={fetchInventory} disabled={loading}>
              <RefreshCw size={12} className={loading ? 'animate-spin' : ''} />
              <span>Refresh</span>
            </button>
          </div>

          <div className="table-container">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Operator</th>
                  <th>Denomination</th>
                  <th style={{ color: '#34d399' }}>Available</th>
                  <th style={{ color: '#fbbf24' }}>Reserved</th>
                  <th>Used</th>
                  <th>Expired</th>
                  <th>Total</th>
                </tr>
              </thead>
              <tbody>
                {inventory.length === 0 && (
                  <tr>
                    <td colSpan={7} style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-muted)' }}>
                      No inventory records found.
                    </td>
                  </tr>
                )}
                {inventory.map((item, idx) => (
                  <tr key={idx}>
                    <td style={{ fontWeight: 600 }}>{item.operatorName || item.operatorCode}</td>
                    <td style={{ fontWeight: 700 }}>₹{item.denomination}</td>
                    <td style={{ color: '#34d399', fontWeight: 700 }}>{item.availableCount}</td>
                    <td style={{ color: '#fbbf24' }}>{item.reservedCount}</td>
                    <td>{item.usedCount}</td>
                    <td>{item.expiredCount}</td>
                    <td style={{ fontWeight: 600 }}>{item.totalCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Right Column: Atomic Reservation Test Tool */}
        <div className="card-panel">
          <div className="card-header">
            <div>
              <h2 className="card-title">
                <ShieldCheck size={20} color="#06b6d4" />
                <span>Atomic Card Reservation Test</span>
              </h2>
              <p className="card-subtitle">
                Demonstrates single-statement <code>UPDATE...OUTPUT INSERTED.Id WHERE Status='AVAILABLE'</code>
              </p>
            </div>
          </div>

          <p style={{ fontSize: '0.825rem', color: 'var(--text-secondary)', marginBottom: '1rem' }}>
            Prevents double-spending race conditions. If two requests attempt to reserve the same card concurrently, exactly one will update 1 row and succeed; the second will affect 0 rows and return <code>409 Conflict</code>.
          </p>

          <form onSubmit={handleAtomicReserve}>
            <div className="form-group">
              <label className="form-label">
                <span>Card Number to Claim</span>
                <span style={{ fontSize: '0.75rem', color: '#94a3b8' }}>e.g. JIO-CARD-100-001</span>
              </label>
              <input
                type="text"
                className="form-input"
                placeholder="Enter Card Number..."
                value={reserveCardNumber}
                onChange={(e) => setReserveCardNumber(e.target.value)}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label">
                <span>Claiming Transaction ID</span>
              </label>
              <input
                type="text"
                className="form-input"
                value={reserveTxnId}
                onChange={(e) => setReserveTxnId(e.target.value)}
                required
              />
            </div>

            {reserveError && (
              <div className="duplicate-alert" style={{ background: 'rgba(239, 68, 68, 0.15)', borderColor: '#ef4444', color: '#f87171' }}>
                <AlertCircle size={16} />
                <span>{reserveError}</span>
              </div>
            )}

            {reserveResult && (
              <div className="duplicate-alert" style={{ background: 'rgba(16, 185, 129, 0.15)', borderColor: '#10b981', color: '#34d399' }}>
                <CheckCircle2 size={16} />
                <div>
                  <strong>Card Successfully Reserved!</strong>
                  <div style={{ fontSize: '0.75rem' }}>
                    Card <code>{reserveResult.cardNumber}</code> (₹{reserveResult.denomination}) claimed by <code>{reserveResult.usedTransactionId}</code>.
                  </div>
                </div>
              </div>
            )}

            <button type="submit" className="btn-primary" disabled={reserveLoading}>
              {reserveLoading ? (
                <>
                  <RefreshCw size={16} className="animate-spin" />
                  <span>Executing Atomic Reservation...</span>
                </>
              ) : (
                <>
                  <Zap size={16} />
                  <span>Reserve Card Atomically</span>
                </>
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};
