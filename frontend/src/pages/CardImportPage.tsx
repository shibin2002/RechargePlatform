import React, { useState, useEffect, useRef } from 'react';
import {
  UploadCloud,
  FileSpreadsheet,
  AlertTriangle,
  Download,
  History,
  RefreshCw,
  AlertCircle
} from 'lucide-react';
import { rechargeApi } from '../services/api';
import type { CardBatchSummary, CardImportResult } from '../types';

export const CardImportPage: React.FC = () => {
  const [file, setFile] = useState<File | null>(null);
  const [importing, setImporting] = useState(false);
  const [importResult, setImportResult] = useState<CardImportResult | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [batches, setBatches] = useState<CardBatchSummary[]>([]);
  const [dragActive, setDragActive] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const fetchBatches = async () => {
    try {
      const response = await rechargeApi.getCardBatches();
      if (response.success && response.data) {
        setBatches(response.data);
      }
    } catch (err) {
      console.error('Failed to load card batches:', err);
    }
  };

  useEffect(() => {
    fetchBatches();
  }, []);

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      const dropped = e.dataTransfer.files[0];
      if (dropped.name.endsWith('.csv')) {
        setFile(dropped);
        setErrorMsg(null);
      } else {
        setErrorMsg('Only .csv files are supported.');
      }
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
      setErrorMsg(null);
    }
  };

  const handleUpload = async () => {
    if (!file) {
      setErrorMsg('Please select a CSV file to upload.');
      return;
    }

    setImporting(true);
    setErrorMsg(null);

    try {
      const response = await rechargeApi.importCardsCsv(file, 'POS_AGENT');
      if (response.success && response.data) {
        setImportResult(response.data);
        fetchBatches();
      } else {
        setErrorMsg(response.message || 'Import failed.');
      }
    } catch (err: any) {
      const msg = err.response?.data?.message || err.message;
      setErrorMsg(msg || 'An error occurred during CSV import.');
    } finally {
      setImporting(false);
    }
  };

  const handleDownloadSampleCsv = () => {
    const csvContent = `CardNumber,SerialNumber,Operator,Denomination,ExpiryDate
JIO-CARD-500-1001,SN-JIO-5001,Jio,500,2027-12-31
JIO-CARD-500-1002,SN-JIO-5002,Jio,500,2027-12-31
AIRTEL-CARD-299-1001,SN-AIR-5001,Airtel,299,2027-12-31
AIRTEL-CARD-299-1002,SN-AIR-5002,Airtel,299,2027-12-31
VI-CARD-100-1001,SN-VI-5001,Vi,100,2027-12-31
BSNL-CARD-299-1001,SN-BSNL-5001,BSNL,299,2027-12-31
INVALID-CARD-ROW-01,SN-INV-001,UnknownTelco,100,2027-12-31
INVALID-CARD-ROW-02,SN-INV-002,Jio,-50,2027-12-31
INVALID-CARD-ROW-03,SN-INV-003,Airtel,299,invalid-date
JIO-CARD-500-1001,SN-JIO-DUP1,Jio,500,2027-12-31`;

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', 'sample_vouchers_batch.csv');
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  return (
    <div>
      {/* Top Section: Upload Box & Actions */}
      <div className="card-panel" style={{ marginBottom: '1.5rem' }}>
        <div className="card-header">
          <div>
            <h2 className="card-title">
              <UploadCloud size={20} color="#3b82f6" />
              <span>Bulk Card / Voucher CSV Import</span>
            </h2>
            <p className="card-subtitle">
              High-throughput ingestion powered by <code>SqlBulkCopy</code> & set-based merge (10,000+ rows supported)
            </p>
          </div>
          <button className="btn-secondary" onClick={handleDownloadSampleCsv}>
            <Download size={14} />
            <span>Download Sample CSV</span>
          </button>
        </div>

        {/* Drag & Drop Zone */}
        <div
          className={`file-dropzone ${dragActive ? 'drag-active' : ''}`}
          onDragEnter={handleDrag}
          onDragLeave={handleDrag}
          onDragOver={handleDrag}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
        >
          <input
            type="file"
            ref={fileInputRef}
            onChange={handleFileChange}
            accept=".csv"
            style={{ display: 'none' }}
          />

          <FileSpreadsheet size={40} style={{ color: '#3b82f6', marginBottom: '0.75rem' }} />
          <h3 style={{ fontSize: '1rem', fontWeight: 600, color: '#f8fafc' }}>
            {file ? file.name : 'Click or Drag & Drop Voucher CSV File'}
          </h3>
          <p style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', marginTop: '0.25rem' }}>
            {file
              ? `${(file.size / 1024).toFixed(1)} KB ready for import`
              : 'Required columns: CardNumber, SerialNumber, Operator, Denomination, ExpiryDate'}
          </p>
        </div>

        {errorMsg && (
          <div className="duplicate-alert" style={{ marginTop: '1rem', background: 'rgba(239, 68, 68, 0.15)', borderColor: '#ef4444', color: '#f87171' }}>
            <AlertTriangle size={16} />
            <span>{errorMsg}</span>
          </div>
        )}

        <div style={{ marginTop: '1.25rem', display: 'flex', justifyContent: 'flex-end', gap: '0.75rem' }}>
          {file && (
            <button className="btn-secondary" onClick={() => { setFile(null); setImportResult(null); }}>
              Clear
            </button>
          )}
          <button
            className="btn-primary"
            style={{ width: 'auto' }}
            disabled={!file || importing}
            onClick={handleUpload}
          >
            {importing ? (
              <>
                <RefreshCw size={16} className="animate-spin" />
                <span>Bulk Ingesting & Merging...</span>
              </>
            ) : (
              <>
                <UploadCloud size={16} />
                <span>Start Batch Import</span>
              </>
            )}
          </button>
        </div>
      </div>

      {/* Import Result Metrics */}
      {importResult && (
        <div style={{ marginBottom: '1.5rem' }}>
          <h3 style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '0.75rem', color: '#f8fafc' }}>
            Batch Import Summary: {importResult.fileName} (Batch #{importResult.batchId})
          </h3>

          <div className="metrics-grid">
            <div className="metric-card">
              <div className="metric-label">Total Rows in File</div>
              <div className="metric-val">{importResult.totalRows}</div>
            </div>
            <div className="metric-card" style={{ borderColor: 'rgba(16, 185, 129, 0.4)' }}>
              <div className="metric-label" style={{ color: '#34d399' }}>Successfully Imported</div>
              <div className="metric-val" style={{ color: '#34d399' }}>{importResult.imported}</div>
            </div>
            <div className="metric-card" style={{ borderColor: 'rgba(245, 158, 11, 0.4)' }}>
              <div className="metric-label" style={{ color: '#fbbf24' }}>Duplicates Rejected</div>
              <div className="metric-val" style={{ color: '#fbbf24' }}>{importResult.duplicates}</div>
            </div>
            <div className="metric-card" style={{ borderColor: 'rgba(239, 68, 68, 0.4)' }}>
              <div className="metric-label" style={{ color: '#f87171' }}>Validation Failures</div>
              <div className="metric-val" style={{ color: '#f87171' }}>{importResult.failed}</div>
            </div>
          </div>

          {/* Failed Rows Table */}
          {importResult.failedRows && importResult.failedRows.length > 0 && (
            <div className="card-panel" style={{ marginBottom: '1.5rem' }}>
              <div className="card-header">
                <div>
                  <h3 className="card-title" style={{ color: '#f87171' }}>
                    <AlertCircle size={18} />
                    <span>Row-Level Rejection Breakdown ({importResult.failedRows.length} Rows)</span>
                  </h3>
                  <p className="card-subtitle">Partial success allowed; invalid & duplicate rows isolated with specific reasons</p>
                </div>
              </div>

              <div className="table-container">
                <table className="data-table">
                  <thead>
                    <tr>
                      <th>Line #</th>
                      <th>Card Number</th>
                      <th>Serial Number</th>
                      <th>Operator</th>
                      <th>Denomination</th>
                      <th>Expiry</th>
                      <th>Rejection Reason</th>
                    </tr>
                  </thead>
                  <tbody>
                    {importResult.failedRows.map((r, i) => (
                      <tr key={i}>
                        <td style={{ fontWeight: 700, color: '#f87171' }}>Row {r.rowNumber}</td>
                        <td><span className="code-pill">{r.cardNumber || '—'}</span></td>
                        <td><span className="code-pill">{r.serialNumber || '—'}</span></td>
                        <td>{r.operator || '—'}</td>
                        <td>{r.denomination ? `₹${r.denomination}` : '—'}</td>
                        <td>{r.expiryDate || '—'}</td>
                        <td style={{ color: '#fbbf24', fontWeight: 500 }}>{r.errorReason}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Past Batches Table */}
      <div className="card-panel">
        <div className="card-header">
          <div>
            <h3 className="card-title">
              <History size={18} color="#94a3b8" />
              <span>Card Import Batch History (CardImportBatches)</span>
            </h3>
            <p className="card-subtitle">Complete audit record of historical voucher uploads</p>
          </div>
          <button className="btn-secondary" onClick={fetchBatches}>
            <RefreshCw size={12} />
            <span>Refresh</span>
          </button>
        </div>

        <div className="table-container">
          <table className="data-table">
            <thead>
              <tr>
                <th>Batch ID</th>
                <th>File Name</th>
                <th>Total Rows</th>
                <th>Imported</th>
                <th>Duplicates</th>
                <th>Failed</th>
                <th>Status</th>
                <th>Imported Date</th>
              </tr>
            </thead>
            <tbody>
              {batches.length === 0 && (
                <tr>
                  <td colSpan={8} style={{ textAlign: 'center', padding: '2rem', color: 'var(--text-muted)' }}>
                    No import batches found.
                  </td>
                </tr>
              )}
              {batches.map((b) => (
                <tr key={b.batchId}>
                  <td><span className="code-pill">BATCH-{b.batchId}</span></td>
                  <td style={{ fontWeight: 600 }}>{b.fileName}</td>
                  <td>{b.totalRows}</td>
                  <td style={{ color: '#34d399', fontWeight: 600 }}>{b.successfulRows}</td>
                  <td style={{ color: '#fbbf24' }}>{b.duplicateRows}</td>
                  <td style={{ color: '#f87171' }}>{b.failedRows}</td>
                  <td>
                    <span className="status-badge success">{b.status}</span>
                  </td>
                  <td style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                    {new Date(b.importedDate).toLocaleString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
