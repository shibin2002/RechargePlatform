import React from 'react';
import { CheckCircle2, XCircle, Clock, Loader2, Sparkles, Ban } from 'lucide-react';

interface StatusBadgeProps {
  status: string;
  showIcon?: boolean;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status, showIcon = true }) => {
  const normStatus = (status || '').toUpperCase();

  const getBadgeContent = () => {
    switch (normStatus) {
      case 'SUCCESS':
        return {
          icon: <CheckCircle2 size={13} />,
          className: 'status-badge success',
          label: 'Success',
        };
      case 'FAILED':
        return {
          icon: <XCircle size={13} />,
          className: 'status-badge failed',
          label: 'Failed',
        };
      case 'PENDING':
        return {
          icon: <span className="pulse-dot" />,
          className: 'status-badge pending',
          label: 'Pending',
        };
      case 'PROCESSING':
        return {
          icon: <Loader2 size={13} className="animate-spin" />,
          className: 'status-badge processing',
          label: 'Processing',
        };
      case 'NEW':
        return {
          icon: <Sparkles size={13} />,
          className: 'status-badge new',
          label: 'New',
        };
      case 'AVAILABLE':
        return {
          icon: <CheckCircle2 size={13} />,
          className: 'status-badge available',
          label: 'Available',
        };
      case 'RESERVED':
        return {
          icon: <Clock size={13} />,
          className: 'status-badge reserved',
          label: 'Reserved',
        };
      case 'USED':
        return {
          icon: <CheckCircle2 size={13} />,
          className: 'status-badge used',
          label: 'Used',
        };
      case 'EXPIRED':
        return {
          icon: <Ban size={13} />,
          className: 'status-badge expired',
          label: 'Expired',
        };
      case 'BLOCKED':
        return {
          icon: <Ban size={13} />,
          className: 'status-badge failed',
          label: 'Blocked',
        };
      default:
        return {
          icon: null,
          className: 'status-badge',
          label: status,
        };
    }
  };

  const { icon, className, label } = getBadgeContent();

  return (
    <span className={className}>
      {showIcon && icon}
      <span>{label}</span>
    </span>
  );
};
