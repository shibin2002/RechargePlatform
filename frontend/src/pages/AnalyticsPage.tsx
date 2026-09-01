import React, { useState, useEffect } from 'react';
import { BarChart3, Database } from 'lucide-react';
import { rechargeApi } from '../services/api';

interface QueryItem {
  id: string;
  name: string;
  category: 'Transactions' | 'Vouchers';
  description: string;
}

const QUERIES: QueryItem[] = [
  {
    id: 'successfultoday',
    name: 'Successful Transactions Today',
    category: 'Transactions',
    description: 'Returns all recharges completed successfully today with operator and provider reference.',
  },
  {
    id: 'failedtoday',
    name: 'Failed Transactions Today',
    category: 'Transactions',
    description: 'Lists all failed recharges today along with error reasons for operational triage.',
  },
  {
    id: 'pending',
    name: 'Pending Transactions (Requires Reconciliation)',
    category: 'Transactions',
    description: 'Shows all transactions currently in PENDING state with elapsed wait duration in minutes.',
  },
  {
    id: 'amountbyoperator',
    name: 'Total Recharge Amount & Volume by Operator',
    category: 'Transactions',
    description: 'Aggregates volume, success count, failure count, and total gross revenue by telco.',
  },
  {
    id: 'duplicatemobiles',
    name: 'Duplicate / Frequent Mobile Recharges',
    category: 'Transactions',
    description: 'Identifies subscribers recharged multiple times to detect retail patterns or anomalies.',
  },
  {
    id: 'top10mobiles',
    name: 'Top 10 Mobile Numbers by Total Amount',
    category: 'Transactions',
    description: 'Finds the highest spending customer accounts across all telcos.',
  },
  {
    id: 'daterange',
    name: 'Transactions Between Two Dates',
    category: 'Transactions',
    description: 'Extracts all transaction logs within a specific date range window.',
  },
  {
    id: 'cardsbyoperator',
    name: 'Available Cards by Operator',
    category: 'Vouchers',
    description: 'Inventory report of active available physical/e-vouchers per operator.',
  },
  {
    id: 'cardsbydenomination',
    name: 'Available Cards by Denomination',
    category: 'Vouchers',
    description: 'Voucher stock count and financial valuation grouped by face value.',
  },
  {
    id: 'usedcards',
    name: 'Used Cards History & Linked Transactions',
    category: 'Vouchers',
    description: 'Audit log linking redeemed vouchers to specific recharge transactions and subscribers.',
  },
  {
    id: 'expiredcards',
    name: 'Expired Voucher Cards',
    category: 'Vouchers',
    description: 'Finds all cards past their expiry date that are no longer redeemable.',
  },
  {
    id: 'cardsuseddaterange',
    name: 'Cards Used Between Two Dates',
    category: 'Vouchers',
    description: 'Redemption reporting within a specified audit period.',
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
      <div className="card-panel" style={{ height: 'fit-content', maxHeight: '600px', overflowY: 'auto' }}>
        <div className="card-header">
          <div>
            <h3 className="card-title">
              <Database size={18} color="#3b82f6" />
              <span>Analytics</span>
            </h3>
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

      {/* Right Column: Query Results */}
      <div style={{ overflow: 'hidden' }}>
        {/* Results Panel */}
        <div className="card-panel">
          <div className="card-header">
            <div>
              <h3 className="card-title">
                <BarChart3 size={18} color="#06b6d4" />
                <span>{selectedQuery.name} ({results.length} {results.length === 1 ? 'Row' : 'Rows'})</span>
              </h3>
              <p className="card-subtitle">{selectedQuery.description}</p>
            </div>
          </div>

          <div className="table-container" style={{ maxHeight: '470px', overflowY: 'auto', overflowX: 'auto' }}>
            <table className="data-table" style={{ minWidth: '100px', width: '100%' }}>
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
