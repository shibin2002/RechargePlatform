import React, { useState, useEffect } from 'react';
import { BarChart3, Play, RefreshCw, Database, Code } from 'lucide-react';
import { rechargeApi } from '../services/api';

interface QueryItem {
  id: string;
  name: string;
  category: 'Transactions' | 'Vouchers';
  description: string;
  sql: string;
}

const QUERIES: QueryItem[] = [
  {
    id: 'successfultoday',
    name: 'Successful Transactions Today',
    category: 'Transactions',
    description: 'Returns all recharges completed successfully today with operator and provider reference.',
    sql: `SELECT t.TransactionId, t.MobileNumber, o.Code AS Operator, t.Amount, t.ProviderReference, t.CreatedDate\nFROM dbo.RechargeTransactions t\nINNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id\nWHERE t.Status = 'SUCCESS' AND CAST(t.CreatedDate AS DATE) = CAST(SYSUTCDATETIME() AS DATE)\nORDER BY t.CreatedDate DESC;`,
  },
  {
    id: 'failedtoday',
    name: 'Failed Transactions Today',
    category: 'Transactions',
    description: 'Lists all failed recharges today along with error reasons for operational triage.',
    sql: `SELECT t.TransactionId, t.MobileNumber, o.Code AS Operator, t.Amount, t.ErrorMessage, t.CreatedDate\nFROM dbo.RechargeTransactions t\nINNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id\nWHERE t.Status = 'FAILED' AND CAST(t.CreatedDate AS DATE) = CAST(SYSUTCDATETIME() AS DATE)\nORDER BY t.CreatedDate DESC;`,
  },
  {
    id: 'pending',
    name: 'Pending Transactions (Requires Reconciliation)',
    category: 'Transactions',
    description: 'Shows all transactions currently in PENDING state with elapsed wait duration in minutes.',
    sql: `SELECT t.TransactionId, t.MobileNumber, o.Code AS Operator, t.Amount, t.ProviderReference, t.CreatedDate,\n       DATEDIFF(MINUTE, t.UpdatedDate, SYSUTCDATETIME()) AS MinutesPending\nFROM dbo.RechargeTransactions t\nINNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id\nWHERE t.Status = 'PENDING'\nORDER BY t.CreatedDate ASC;`,
  },
  {
    id: 'amountbyoperator',
    name: 'Total Recharge Amount & Volume by Operator',
    category: 'Transactions',
    description: 'Aggregates volume, success count, failure count, and total gross revenue by telco.',
    sql: `SELECT o.Code AS OperatorCode, o.Name AS OperatorName,\n       COUNT(CASE WHEN t.Status = 'SUCCESS' THEN 1 END) AS SuccessfulCount,\n       COALESCE(SUM(CASE WHEN t.Status = 'SUCCESS' THEN t.Amount ELSE 0 END), 0) AS TotalSuccessfulAmount,\n       COUNT(CASE WHEN t.Status = 'FAILED' THEN 1 END) AS FailedCount,\n       COUNT(CASE WHEN t.Status = 'PENDING' THEN 1 END) AS PendingCount,\n       COUNT(1) AS TotalAttemptedCount\nFROM dbo.TelecomOperators o\nLEFT JOIN dbo.RechargeTransactions t ON o.Id = t.OperatorId\nGROUP BY o.Code, o.Name\nORDER BY TotalSuccessfulAmount DESC;`,
  },
  {
    id: 'duplicatemobiles',
    name: 'Duplicate / Frequent Mobile Recharges',
    category: 'Transactions',
    description: 'Identifies subscribers recharged multiple times to detect retail patterns or anomalies.',
    sql: `SELECT t.MobileNumber, COUNT(1) AS TotalRechargeAttempts,\n       COUNT(CASE WHEN t.Status = 'SUCCESS' THEN 1 END) AS SuccessfulRecharges,\n       SUM(CASE WHEN t.Status = 'SUCCESS' THEN t.Amount ELSE 0 END) AS TotalRechargedAmount,\n       MIN(t.CreatedDate) AS FirstRechargeDate, MAX(t.CreatedDate) AS LatestRechargeDate\nFROM dbo.RechargeTransactions t\nGROUP BY t.MobileNumber\nHAVING COUNT(1) > 1\nORDER BY TotalRechargeAttempts DESC;`,
  },
  {
    id: 'top10mobiles',
    name: 'Top 10 Mobile Numbers by Total Amount',
    category: 'Transactions',
    description: 'Finds the highest spending customer accounts across all telcos.',
    sql: `SELECT TOP 10 t.MobileNumber, COUNT(1) AS RechargeCount, SUM(t.Amount) AS TotalAmountSpent,\n       MAX(t.CreatedDate) AS LastRechargedAt\nFROM dbo.RechargeTransactions t\nWHERE t.Status = 'SUCCESS'\nGROUP BY t.MobileNumber\nORDER BY TotalAmountSpent DESC;`,
  },
  {
    id: 'daterange',
    name: 'Transactions Between Two Dates',
    category: 'Transactions',
    description: 'Extracts all transaction logs within a specific date range window.',
    sql: `SELECT t.TransactionId, t.MobileNumber, o.Code AS Operator, t.Amount, t.Status, t.ProviderReference, t.ErrorMessage, t.CreatedDate\nFROM dbo.RechargeTransactions t\nINNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id\nWHERE t.CreatedDate >= @StartDate AND t.CreatedDate <= @EndDate\nORDER BY t.CreatedDate DESC;`,
  },
  {
    id: 'cardsbyoperator',
    name: 'Available Cards by Operator',
    category: 'Vouchers',
    description: 'Inventory report of active available physical/e-vouchers per operator.',
    sql: `SELECT o.Code AS OperatorCode, o.Name AS OperatorName, COUNT(1) AS AvailableCardsCount,\n       SUM(c.Denomination) AS TotalInventoryValue\nFROM dbo.RechargeCards c\nINNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id\nWHERE c.Status = 'AVAILABLE'\nGROUP BY o.Code, o.Name\nORDER BY AvailableCardsCount DESC;`,
  },
  {
    id: 'cardsbydenomination',
    name: 'Available Cards by Denomination',
    category: 'Vouchers',
    description: 'Voucher stock count and financial valuation grouped by face value.',
    sql: `SELECT c.Denomination, COUNT(1) AS AvailableCardsCount, SUM(c.Denomination) AS TotalStockValue\nFROM dbo.RechargeCards c\nWHERE c.Status = 'AVAILABLE'\nGROUP BY c.Denomination\nORDER BY c.Denomination ASC;`,
  },
  {
    id: 'usedcards',
    name: 'Used Cards History & Linked Transactions',
    category: 'Vouchers',
    description: 'Audit log linking redeemed vouchers to specific recharge transactions and subscribers.',
    sql: `SELECT c.CardNumber, c.SerialNumber, o.Code AS Operator, c.Denomination, c.UsedTransactionId, c.UsedDate,\n       t.MobileNumber AS RechargedMobile, t.Status AS TransactionStatus\nFROM dbo.RechargeCards c\nINNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id\nLEFT JOIN dbo.RechargeTransactions t ON c.UsedTransactionId = t.TransactionId\nWHERE c.Status = 'USED'\nORDER BY c.UsedDate DESC;`,
  },
  {
    id: 'expiredcards',
    name: 'Expired Voucher Cards',
    category: 'Vouchers',
    description: 'Finds all cards past their expiry date that are no longer redeemable.',
    sql: `SELECT c.CardNumber, c.SerialNumber, o.Code AS Operator, c.Denomination, c.ExpiryDate, c.Status\nFROM dbo.RechargeCards c\nINNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id\nWHERE c.Status = 'EXPIRED' OR (c.Status = 'AVAILABLE' AND c.ExpiryDate < CAST(SYSUTCDATETIME() AS DATE))\nORDER BY c.ExpiryDate ASC;`,
  },
  {
    id: 'cardsuseddaterange',
    name: 'Cards Used Between Two Dates',
    category: 'Vouchers',
    description: 'Redemption reporting within a specified audit period.',
    sql: `SELECT c.CardNumber, c.SerialNumber, o.Code AS Operator, c.Denomination, c.UsedTransactionId, c.UsedDate\nFROM dbo.RechargeCards c\nINNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id\nWHERE c.UsedDate >= @StartDate AND c.UsedDate <= @EndDate\nORDER BY c.UsedDate DESC;`,
  },
];

export const AnalyticsPage: React.FC = () => {
  const [selectedQueryId, setSelectedQueryId] = useState<string>('amountbyoperator');
  const [results, setResults] = useState<any[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [executionTimeMs, setExecutionTimeMs] = useState<number | null>(null);

  const selectedQuery = QUERIES.find((q) => q.id === selectedQueryId) || QUERIES[0];

  const handleRunQuery = async (queryId: string) => {
    setSelectedQueryId(queryId);
    setLoading(true);
    const start = performance.now();

    try {
      const response = await rechargeApi.runAnalyticsQuery(queryId);
      const elapsed = Math.round(performance.now() - start);
      setExecutionTimeMs(elapsed);

      if (response.success && Array.isArray(response.data)) {
        setResults(response.data);
      } else {
        setResults([]);
      }
    } catch (err) {
      console.error('Query execution error:', err);
      setResults([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    handleRunQuery('amountbyoperator');
  }, []);

  const columns = results.length > 0 ? Object.keys(results[0]) : [];

  return (
    <div className="grid-two-col" style={{ gridTemplateColumns: '320px 1fr' }}>
      {/* Left Sidebar: Query Selector */}
      <div className="card-panel" style={{ height: 'fit-content' }}>
        <div className="card-header">
          <div>
            <h3 className="card-title">
              <Database size={18} color="#3b82f6" />
              <span>Analytical T-SQL Queries</span>
            </h3>
            <p className="card-subtitle">Required production operational queries</p>
          </div>
        </div>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.4rem' }}>
          {QUERIES.map((q) => (
            <button
              key={q.id}
              onClick={() => handleRunQuery(q.id)}
              style={{
                textAlign: 'left',
                background: selectedQueryId === q.id ? 'rgba(37, 99, 235, 0.2)' : 'var(--bg-card)',
                borderColor: selectedQueryId === q.id ? '#3b82f6' : 'var(--border-color)',
                borderWidth: '1px',
                borderStyle: 'solid',
                borderRadius: '6px',
                padding: '0.6rem 0.75rem',
                color: selectedQueryId === q.id ? '#60a5fa' : 'var(--text-primary)',
                cursor: 'pointer',
                fontSize: '0.825rem',
                fontWeight: selectedQueryId === q.id ? 700 : 500,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                transition: 'all 0.15s',
              }}
            >
              <span>{q.name}</span>
              <span style={{ fontSize: '0.65rem', background: 'rgba(0,0,0,0.3)', padding: '2px 5px', borderRadius: '3px' }}>
                {q.category}
              </span>
            </button>
          ))}
        </div>
      </div>

      {/* Right Column: Query Details & Execution Output */}
      <div>
        {/* Query Info Card */}
        <div className="card-panel" style={{ marginBottom: '1.25rem' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '0.75rem' }}>
            <div>
              <h2 style={{ fontSize: '1.15rem', fontWeight: 700, color: 'var(--text-primary)' }}>{selectedQuery.name}</h2>
              <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', marginTop: '2px' }}>
                {selectedQuery.description}
              </p>
            </div>
            <button
              className="btn-primary"
              style={{ width: 'auto', padding: '0.5rem 1rem' }}
              onClick={() => handleRunQuery(selectedQuery.id)}
              disabled={loading}
            >
              {loading ? <RefreshCw size={14} className="animate-spin" /> : <Play size={14} />}
              <span>{loading ? 'Executing...' : 'Run Query'}</span>
            </button>
          </div>

          {/* Raw SQL Preview */}
          <div style={{ background: 'var(--bg-card)', border: '1px solid var(--border-color)', borderRadius: '6px', padding: '0.75rem' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.7rem', color: 'var(--text-muted)', marginBottom: '0.4rem' }}>
              <Code size={12} />
              <span>RAW T-SQL QUERY</span>
            </div>
            <pre style={{ fontFamily: 'var(--font-mono)', fontSize: '0.775rem', color: 'var(--accent-cyan)', overflowX: 'auto', whiteSpace: 'pre-wrap' }}>
              {selectedQuery.sql}
            </pre>
          </div>
        </div>

        {/* Results Panel */}
        <div className="card-panel">
          <div className="card-header">
            <div>
              <h3 className="card-title">
                <BarChart3 size={18} color="#06b6d4" />
                <span>Query Results ({results.length} Rows)</span>
              </h3>
              {executionTimeMs !== null && (
                <p className="card-subtitle">Executed against SQL Server in {executionTimeMs} ms</p>
              )}
            </div>
          </div>

          <div className="table-container">
            <table className="data-table">
              <thead>
                <tr>
                  {columns.map((col) => (
                    <th key={col}>{col}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {results.length === 0 && !loading && (
                  <tr>
                    <td colSpan={columns.length || 1} style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-muted)' }}>
                      Query returned 0 rows.
                    </td>
                  </tr>
                )}
                {results.map((row, idx) => (
                  <tr key={idx}>
                    {columns.map((col) => (
                      <td key={col}>
                        {typeof row[col] === 'boolean'
                          ? row[col] ? 'TRUE' : 'FALSE'
                          : row[col] !== null && row[col] !== undefined
                          ? String(row[col])
                          : '—'}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};
