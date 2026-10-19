'use client';
import React, { useState, useEffect, useMemo } from 'react';
import {
  LayoutDashboard,
  Receipt,
  Users,
  Activity,
  Settings,
  Zap,
  CheckCircle2,
  AlertTriangle,
  Clock,
  XCircle,
  TrendingUp,
  RefreshCw,
  Search,
  Filter,
  ShieldAlert,
  DollarSign,
  ArrowUpRight,
  ChevronRight,
  X,
  Server,
  Lock,
  BarChart3,
  Sparkles,
  FileSpreadsheet,
  Layers,
  ArrowRight
} from 'lucide-react';

// --- MOCK DATA FOR DEMO MODE ---
const MOCK_SUMMARY = {
  totalActiveSubscriptions: 1420,
  totalRevenueThisMonth: 94850.00,
  totalOverdueAmount: 2850.00,
  paidInvoicesCount: 1042,
  pendingInvoicesCount: 68,
  overdueInvoicesCount: 14,
  failedInvoicesCount: 5
};

const MOCK_MONTHLY_REVENUE = [
  { month: 'May', amount: 68000, invoices: 740 },
  { month: 'Jun', amount: 73500, invoices: 810 },
  { month: 'Jul', amount: 81000, invoices: 880 },
  { month: 'Aug', amount: 86500, invoices: 930 },
  { month: 'Sep', amount: 90200, invoices: 980 },
  { month: 'Oct', amount: 94850, invoices: 1042 }
];

const MOCK_INVOICES = [
  { id: 'INV-2026-0091', customerId: 'CUST-102', customerName: 'Acme Corporate Technologies', planName: 'Enterprise Scale', amount: 1850.00, dueDate: '2026-10-15', status: 'Overdue', createdAt: '2026-09-15', failureReason: null },
  { id: 'INV-2026-0092', customerId: 'CUST-105', customerName: 'TechFlow Cloud Systems', planName: 'Pro Tier', amount: 399.00, dueDate: '2026-10-25', status: 'Pending', createdAt: '2026-09-25', failureReason: null },
  { id: 'INV-2026-0093', customerId: 'CUST-108', customerName: 'Global Logistics Group', planName: 'Enterprise Scale', amount: 3200.00, dueDate: '2026-10-10', status: 'Paid', createdAt: '2026-09-10', failureReason: null },
  { id: 'INV-2026-0094', customerId: 'CUST-112', customerName: 'Nexus AI Laboratories', planName: 'Starter Growth', amount: 149.00, dueDate: '2026-09-30', status: 'Failed', createdAt: '2026-08-30', failureReason: 'Payment gateway timeout (Max Retries Exceeded - Poison Queue Enqueued)' },
  { id: 'INV-2026-0095', customerId: 'CUST-115', customerName: 'Starlight Interactive Engine', planName: 'Pro Tier', amount: 399.00, dueDate: '2026-10-18', status: 'Pending', createdAt: '2026-09-18', failureReason: null },
  { id: 'INV-2026-0096', customerId: 'CUST-120', customerName: 'CyberData Defense Works', planName: 'Enterprise Scale', amount: 2450.00, dueDate: '2026-10-01', status: 'Paid', createdAt: '2026-09-01', failureReason: null },
  { id: 'INV-2026-0097', customerId: 'CUST-124', customerName: 'Omni Commerce Retail', planName: 'Starter Growth', amount: 149.00, dueDate: '2026-10-05', status: 'Overdue', createdAt: '2026-09-05', failureReason: null }
];

const MOCK_CUSTOMERS = [
  { id: 'CUST-102', name: 'Acme Corporate Technologies', email: 'billing@acme.com', plan: 'Enterprise Scale', amount: 1850.00, billingCycle: 'Monthly', status: 'Active', nextBillingDate: '2026-11-15' },
  { id: 'CUST-105', name: 'TechFlow Cloud Systems', email: 'finance@techflow.io', plan: 'Pro Tier', amount: 399.00, billingCycle: 'Monthly', status: 'Active', nextBillingDate: '2026-11-25' },
  { id: 'CUST-108', name: 'Global Logistics Group', email: 'accounts@globallogistics.com', plan: 'Enterprise Scale', amount: 3200.00, billingCycle: 'Monthly', status: 'Active', nextBillingDate: '2026-11-10' },
  { id: 'CUST-112', name: 'Nexus AI Laboratories', email: 'ops@nexusai.dev', plan: 'Starter Growth', amount: 149.00, billingCycle: 'Monthly', status: 'Paused', nextBillingDate: '2026-11-01' },
  { id: 'CUST-115', name: 'Starlight Interactive Engine', email: 'pay@starlight.co', plan: 'Pro Tier', amount: 399.00, billingCycle: 'Monthly', status: 'Active', nextBillingDate: '2026-11-18' }
];

const MOCK_AUDIT_LOGS = [
  { id: 2091, entityName: 'Invoice', entityId: 'INV-2026-0093', action: 'InvoiceStatusChanged', performedBy: 'SystemWorker', correlationId: 'a3819201-9210-48bf-b891-c192019a001', occurredAt: '2026-10-19 21:10:05', details: 'Status transitioned from Pending -> Paid via Webhook callback' },
  { id: 2090, entityName: 'InvoiceJob', entityId: 'JOB-9921-M3', action: 'InvoiceJobDeadLettered', performedBy: 'QueueConsumer', correlationId: '7e12c019-11aa-42b8-ba90-f0012921b44', occurredAt: '2026-10-19 20:45:12', details: 'Max retry count (5) exceeded. Wrapped in PoisonEnvelope & published to poison queue (BR-21, BR-28).' },
  { id: 2089, entityName: 'Invoice', entityId: 'INV-2026-0091', action: 'InvoiceStatusChanged', performedBy: 'OverdueSweeperService', correlationId: '90a12b34-44cc-4e89-88dd-aa011299c10', occurredAt: '2026-10-19 18:00:00', details: 'Batch processing: Status transitioned Pending -> Overdue (DueDate < BusinessDate)' },
  { id: 2088, entityName: 'BillingCycle', entityId: 'BATCH-20261019', action: 'BillingCycleProcessed', performedBy: 'BillingEngine', correlationId: '11b22c33-44dd-55ee-66ff-77aa88bb99cc', occurredAt: '2026-10-19 00:05:00', details: 'Automated billing scan executed for 142 subscriptions with strict Idempotency guarantees' }
];

// --- BR-50 INVOICE STATE MACHINE MATRIX ---
const STATE_TRANSITIONS = {
  Draft: ['Pending', 'Canceled'],
  Pending: ['Paid', 'Overdue', 'Canceled'],
  Overdue: ['Paid', 'Failed'],
  Paid: [], // Terminal & Immutable (BR-51)
  Failed: [], // Terminal
  Canceled: []
};

export default function App() {
  const [activeTab, setActiveTab] = useState('overview');
  const [isDemoMode, setIsDemoMode] = useState(true);
  const [apiUrl, setApiUrl] = useState('https://localhost:7123');
  const [apiHealth, setApiHealth] = useState({ live: false, ready: false, checking: false });
  
  // Data States
  const [summary, setSummary] = useState(MOCK_SUMMARY);
  const [invoices, setInvoices] = useState(MOCK_INVOICES);
  const [customers, setCustomers] = useState(MOCK_CUSTOMERS);
  const [auditLogs, setAuditLogs] = useState(MOCK_AUDIT_LOGS);
  
  // Filter & Modal States
  const [invoiceSearch, setInvoiceSearch] = useState('');
  const [invoiceStatusFilter, setInvoiceStatusFilter] = useState('All');
  const [selectedInvoice, setSelectedInvoice] = useState(null);
  const [statusChangeTarget, setStatusChangeTarget] = useState('');
  const [statusChangeReason, setStatusChangeReason] = useState('');
  const [isTriggeringBilling, setIsTriggeringBilling] = useState(false);
  const [isSweepingOverdue, setIsSweepingOverdue] = useState(false);
  const [toast, setToast] = useState(null);

  // Helper Toast Notification
  const showToast = (message, type = 'success') => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  // Check Health Endpoint when in Live Mode
  const checkHealth = async () => {
    if (isDemoMode) return;
    setApiHealth(prev => ({ ...prev, checking: true }));
    try {
      const resLive = await fetch(`${apiUrl}/health/live`).catch(() => null);
      const resReady = await fetch(`${apiUrl}/health/ready`).catch(() => null);
      setApiHealth({
        live: resLive?.ok || false,
        ready: resReady?.ok || false,
        checking: false
      });
      if (resLive?.ok) {
        showToast('Successfully connected to .NET 8 Backend API!', 'success');
        fetchLiveApiData();
      } else {
        showToast('Unable to reach .NET 8 API. Check CORS settings or port 7123.', 'error');
      }
    } catch {
      setApiHealth({ live: false, ready: false, checking: false });
    }
  };

  // Fetch Live Data
  const fetchLiveApiData = async () => {
    try {
      const resSum = await fetch(`${apiUrl}/api/v1/dashboard/summary`);
      if (resSum.ok) setSummary(await resSum.json());

      const resInv = await fetch(`${apiUrl}/api/v1/invoices`);
      if (resInv.ok) {
        const data = await resInv.json();
        setInvoices(data.items || data);
      }

      const resAudit = await fetch(`${apiUrl}/api/v1/audit-logs`);
      if (resAudit.ok) {
        const data = await resAudit.json();
        setAuditLogs(data.items || data);
      }
    } catch (e) {
      console.error('Live API Fetching Error:', e);
    }
  };

  useEffect(() => {
    if (!isDemoMode) {
      checkHealth();
    } else {
      setSummary(MOCK_SUMMARY);
      setInvoices(MOCK_INVOICES);
      setAuditLogs(MOCK_AUDIT_LOGS);
    }
  }, [isDemoMode]);

  // Execute State Transition (BR-50, BR-51, BR-52)
  const handleExecuteStatusChange = async () => {
    if (!selectedInvoice || !statusChangeTarget) return;

    if (statusChangeTarget === 'Failed' && !statusChangeReason.trim()) {
      showToast('Failure Reason is MANDATORY for transitioning to Failed (BR-52)', 'error');
      return;
    }

    if (isDemoMode) {
      // Local Demo Update
      setInvoices(prev => prev.map(inv => {
        if (inv.id === selectedInvoice.id) {
          return {
            ...inv,
            status: statusChangeTarget,
            failureReason: statusChangeTarget === 'Failed' ? statusChangeReason : inv.failureReason
          };
        }
        return inv;
      }));

      // Add Audit Log Entry
      const newLog = {
        id: Date.now(),
        entityName: 'Invoice',
        entityId: selectedInvoice.id,
        action: 'InvoiceStatusChanged',
        performedBy: 'AdminUser (Manual UI)',
        correlationId: crypto.randomUUID ? crypto.randomUUID() : 'b8192a-manual-corr-id',
        occurredAt: new Date().toISOString().replace('T', ' ').substring(0, 19),
        details: `Status transitioned from ${selectedInvoice.status} to ${statusChangeTarget}. Reason: ${statusChangeReason || 'Manual override'}`
      };
      setAuditLogs(prev => [newLog, ...prev]);

      showToast(`Invoice ${selectedInvoice.id} transitioned to ${statusChangeTarget}!`);
      setSelectedInvoice(null);
      setStatusChangeTarget('');
      setStatusChangeReason('');
    } else {
      // Call .NET 8 Backend API
      try {
        const res = await fetch(`${apiUrl}/api/v1/invoices/${selectedInvoice.id}/status`, {
          method: 'PATCH',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            status: statusChangeTarget,
            reason: statusChangeReason
          })
        });

        if (res.ok) {
          showToast(`Invoice ${selectedInvoice.id} updated successfully via .NET API!`);
          fetchLiveApiData();
          setSelectedInvoice(null);
          setStatusChangeTarget('');
          setStatusChangeReason('');
        } else {
          const err = await res.json().catch(() => ({}));
          showToast(err.detail || 'State transition rejected by Domain State Machine (BR-50 Violation)', 'error');
        }
      } catch {
        showToast('Failed to communicate with API server.', 'error');
      }
    }
  };

  // Trigger Billing Run
  const handleTriggerBilling = async () => {
    setIsTriggeringBilling(true);
    if (isDemoMode) {
      setTimeout(() => {
        setIsTriggeringBilling(false);
        showToast('Billing Engine triggered! Published scan request to Azure Queue Storage.');
        setAuditLogs(prev => [{
          id: Date.now(),
          entityName: 'BillingEngine',
          entityId: 'TRIGGER-MANUAL',
          action: 'ScanRequestPublished',
          performedBy: 'AdminUser',
          correlationId: 'b82910a-demo-trigger',
          occurredAt: new Date().toISOString().replace('T', ' ').substring(0, 19),
          details: 'Manual scan request triggered via Admin Dashboard.'
        }, ...prev]);
      }, 1000);
    } else {
      try {
        const res = await fetch(`${apiUrl}/api/v1/billing/trigger`, { method: 'POST' });
        setIsTriggeringBilling(false);
        if (res.ok) {
          showToast('Billing cycle message queued in Azure Queue Storage!');
          fetchLiveApiData();
        } else {
          showToast('Failed to trigger billing cycle.', 'error');
        }
      } catch {
        setIsTriggeringBilling(false);
        showToast('Network error triggering billing.', 'error');
      }
    }
  };

  // Run Overdue Sweeper
  const handleRunSweeper = async () => {
    setIsSweepingOverdue(true);
    setTimeout(() => {
      setIsSweepingOverdue(false);
      showToast('Overdue Sweeper background execution complete! Evaluated 500 records in 1 transaction.');
    }, 1200);
  };

  // Filtered Invoices
  const filteredInvoices = useMemo(() => {
    return invoices.filter(inv => {
      const matchesSearch = inv.id.toLowerCase().includes(invoiceSearch.toLowerCase()) ||
                            inv.customerName.toLowerCase().includes(invoiceSearch.toLowerCase());
      const matchesStatus = invoiceStatusFilter === 'All' || inv.status === invoiceStatusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [invoices, invoiceSearch, invoiceStatusFilter]);

  // Custom Badge for Statuses
  const StatusBadge = ({ status }) => {
    const config = {
      Paid: 'bg-[#acdf00]/20 text-[#1c454e] border-[#acdf00] font-bold',
      Pending: 'bg-amber-100 text-amber-900 border-amber-300 font-medium',
      Overdue: 'bg-rose-100 text-rose-900 border-rose-300 font-semibold',
      Failed: 'bg-red-900 text-white border-red-950 font-bold',
      Canceled: 'bg-slate-200 text-slate-700 border-slate-300'
    };

    const icons = {
      Paid: <CheckCircle2 className="w-3.5 h-3.5 mr-1 text-[#1c454e]" />,
      Pending: <Clock className="w-3.5 h-3.5 mr-1 text-amber-800" />,
      Overdue: <AlertTriangle className="w-3.5 h-3.5 mr-1 text-rose-800" />,
      Failed: <XCircle className="w-3.5 h-3.5 mr-1 text-white" />
    };

    return (
      <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs border ${config[status] || config.Canceled}`}>
        {icons[status]}
        {status}
      </span>
    );
  };

  return (
    <div className="min-h-screen bg-[#fbfefa] text-slate-800 flex font-sans antialiased selection:bg-[#acdf00] selection:text-[#1c454e]">

      {/* Floating Toast Notification */}
      {toast && (
        <div className={`fixed bottom-6 right-6 z-50 flex items-center gap-3 px-5 py-3.5 rounded-2xl shadow-2xl border transition-all animate-bounce ${
          toast.type === 'error'
            ? 'bg-rose-900 text-white border-rose-700'
            : 'bg-[#1c454e] text-white border-[#acdf00]'
        }`}>
          {toast.type === 'error' ? <AlertTriangle className="w-5 h-5 text-rose-300 shrink-0" /> : <Sparkles className="w-5 h-5 text-[#acdf00] shrink-0" />}
          <span className="text-xs font-medium tracking-wide">{toast.message}</span>
        </div>
      )}

      {/* --- SIDEBAR NAVIGATION (Deep Teal Theme) --- */}
      <aside className="w-68 bg-[#1c454e] text-white flex flex-col justify-between shrink-0 shadow-xl border-r border-[#1c454e]/20">
        <div>
          {/* Logo Brand Header */}
          <div className="p-6 border-b border-white/10 flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-2xl bg-[#acdf00] flex items-center justify-center shadow-lg shadow-[#acdf00]/20">
                <Zap className="w-6 h-6 text-[#1c454e] fill-[#1c454e]" />
              </div>
              <div>
                <h1 className="font-extrabold text-base tracking-tight text-white">AutoBilling</h1>
                <p className="text-[11px] text-[#acdf00] font-mono font-semibold">.NET 8 Clean Engine</p>
              </div>
            </div>
          </div>

          {/* Nav Links */}
          <nav className="p-4 space-y-1.5">
            {[
              { id: 'overview', label: 'Dashboard Overview', icon: LayoutDashboard },
              { id: 'invoices', label: 'Invoices & Lifecycle', icon: Receipt, count: summary.pendingInvoicesCount + summary.overdueInvoicesCount },
              { id: 'customers', label: 'Customers & Plans', icon: Users },
              { id: 'audit-logs', label: 'Audit Logs & Queue', icon: Activity },
              { id: 'settings', label: 'API Connection Settings', icon: Settings }
            ].map((item) => {
              const Icon = item.icon;
              const active = activeTab === item.id;
              return (
                <button
                  key={item.id}
                  onClick={() => setActiveTab(item.id)}
                  className={`w-full flex items-center justify-between px-4 py-3 rounded-xl text-xs font-semibold transition-all ${
                    active
                      ? 'bg-[#acdf00] text-[#1c454e] shadow-md shadow-[#acdf00]/20 font-bold'
                      : 'text-slate-200 hover:bg-white/10 hover:text-white'
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <Icon className={`w-4 h-4 ${active ? 'text-[#1c454e]' : 'text-[#acdf00]'}`} />
                    <span>{item.label}</span>
                  </div>
                  {item.count > 0 && (
                    <span className={`px-2 py-0.5 text-[10px] font-extrabold rounded-full ${
                      active ? 'bg-[#1c454e] text-[#acdf00]' : 'bg-[#acdf00] text-[#1c454e]'
                    }`}>
                      {item.count}
                    </span>
                  )}
                </button>
              );
            })}
          </nav>
        </div>

        {/* Sidebar Footer Mode Status */}
        <div className="p-4 m-4 bg-white/10 rounded-2xl border border-white/10 space-y-3">
          <div className="flex items-center justify-between text-xs">
            <span className="text-slate-300 font-medium">Provider Mode</span>
            <span className={`px-2.5 py-0.5 rounded-md text-[10px] font-mono font-bold uppercase ${
              isDemoMode ? 'bg-[#acdf00] text-[#1c454e]' : 'bg-emerald-400 text-[#1c454e]'
            }`}>
              {isDemoMode ? 'DEMO SIMULATION' : 'LIVE .NET API'}
            </span>
          </div>

          <button
            onClick={() => setIsDemoMode(!isDemoMode)}
            className="w-full flex items-center justify-center gap-2 px-3 py-2 bg-[#acdf00] hover:bg-[#bbf000] text-[#1c454e] text-xs font-bold rounded-xl transition-all shadow"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            Switch to {isDemoMode ? 'Live API' : 'Demo Mode'}
          </button>
        </div>
      </aside>

      {/* --- MAIN CONTENT AREA --- */}
      <main className="flex-1 flex flex-col min-w-0 overflow-y-auto">

        {/* Top Header Navigation */}
        <header className="h-18 border-b border-slate-200 bg-white/80 backdrop-blur-md px-8 flex items-center justify-between shrink-0 sticky top-0 z-10">
          <div className="flex items-center gap-3">
            <h2 className="text-base font-extrabold text-[#1c454e] capitalize tracking-tight">
              {activeTab.replace('-', ' ')}
            </h2>
            <div className="h-4 w-px bg-slate-300" />
            <span className="text-xs text-slate-500 font-mono font-medium">
              Docker SQL Server (1433) • Azure Queue (10001)
            </span>
          </div>

          {/* Quick Header Actions */}
          <div className="flex items-center gap-3">
            <button
              onClick={handleTriggerBilling}
              disabled={isTriggeringBilling}
              className="flex items-center gap-2 px-4 py-2 bg-[#acdf00] hover:bg-[#bbf000] text-[#1c454e] text-xs font-extrabold rounded-xl shadow-md shadow-[#acdf00]/20 transition-all disabled:opacity-50"
            >
              <Zap className={`w-3.5 h-3.5 fill-[#1c454e] ${isTriggeringBilling ? 'animate-spin' : ''}`} />
              {isTriggeringBilling ? 'Triggering...' : 'Trigger Billing Run'}
            </button>

            <button
              onClick={handleRunSweeper}
              disabled={isSweepingOverdue}
              className="flex items-center gap-2 px-4 py-2 bg-[#1c454e] hover:bg-[#255863] text-white text-xs font-semibold rounded-xl transition-all disabled:opacity-50"
            >
              <Clock className={`w-3.5 h-3.5 text-[#acdf00] ${isSweepingOverdue ? 'animate-spin' : ''}`} />
              {isSweepingOverdue ? 'Sweeping...' : 'Run Overdue Sweeper'}
            </button>
          </div>
        </header>

        {/* Main Body Dashboard Container */}
        <div className="p-8 space-y-8 max-w-7xl w-full mx-auto">

          {/* TAB 1: OVERVIEW DASHBOARD */}
          {activeTab === 'overview' && (
            <div className="space-y-8 animate-fadeIn">

              {/* Top Stats Cards Grid */}
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
                {[
                  {
                    title: 'Active Subscriptions',
                    value: summary.totalActiveSubscriptions?.toLocaleString(),
                    sub: '+14% active growth',
                    icon: Users,
                    badge: 'BR-01 Active'
                  },
                  {
                    title: 'Revenue This Month',
                    value: `$${(summary.totalRevenueThisMonth || 0).toLocaleString(undefined, { minimumFractionDigits: 2 })}`,
                    sub: 'Processed & settled',
                    icon: DollarSign,
                    badge: 'Paid State'
                  },
                  {
                    title: 'Overdue Pending Sweeper',
                    value: `$${(summary.totalOverdueAmount || 0).toLocaleString(undefined, { minimumFractionDigits: 2 })}`,
                    sub: `${summary.overdueInvoicesCount} invoices overdue`,
                    icon: AlertTriangle,
                    badge: '30d Grace'
                  },
                  {
                    title: 'Settled Invoices',
                    value: summary.paidInvoicesCount?.toLocaleString(),
                    sub: 'BR-51 Terminal Immutable',
                    icon: CheckCircle2,
                    badge: 'Immutable'
                  }
                ].map((card, idx) => {
                  const CardIcon = card.icon;
                  return (
                    <div key={idx} className="bg-white border border-slate-200 p-6 rounded-2xl shadow-sm relative overflow-hidden group hover:border-[#1c454e] transition-all">
                      <div className="flex items-center justify-between mb-4">
                        <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">{card.title}</span>
                        <div className="p-2.5 rounded-xl bg-[#1c454e] text-[#acdf00]">
                          <CardIcon className="w-4 h-4" />
                        </div>
                      </div>
                      <div className="text-2xl font-black text-[#1c454e] tracking-tight">{card.value}</div>
                      <div className="flex items-center justify-between mt-3">
                        <span className="text-[11px] text-slate-500">{card.sub}</span>
                        <span className="text-[10px] font-mono font-bold px-2 py-0.5 rounded bg-[#acdf00]/30 text-[#1c454e]">
                          {card.badge}
                        </span>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Charts Section */}
              <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

                {/* Revenue Bar Chart (2 Columns) */}
                <div className="lg:col-span-2 bg-white border border-slate-200 p-6 rounded-2xl shadow-sm">
                  <div className="flex items-center justify-between mb-6">
                    <div>
                      <h3 className="text-sm font-extrabold text-[#1c454e] flex items-center gap-2">
                        <TrendingUp className="w-4 h-4 text-[#1c454e]" /> Monthly Billing & Revenue Pipeline
                      </h3>
                      <p className="text-xs text-slate-500">Gross processed volume per billing cycle</p>
                    </div>
                    <span className="text-xs font-mono font-bold text-[#1c454e] bg-[#acdf00]/30 px-3 py-1 rounded-full border border-[#acdf00]">
                      +22.1% MoM
                    </span>
                  </div>

                  {/* Custom SVG Chart Bar Area */}
                  <div className="h-64 w-full relative flex items-end justify-between pt-8 px-4 border-b border-slate-200">
                    {MOCK_MONTHLY_REVENUE.map((item, index) => {
                      const heightPercent = (item.amount / 100000) * 100;
                      return (
                        <div key={index} className="flex flex-col items-center gap-2 h-full justify-end group cursor-pointer relative">
                          {/* Tooltip Hover */}
                          <div className="opacity-0 group-hover:opacity-100 transition-opacity absolute -top-10 bg-[#1c454e] text-white text-[11px] font-mono px-3 py-1 rounded-lg shadow-xl whitespace-nowrap z-20 pointer-events-none">
                            ${item.amount.toLocaleString()} ({item.invoices} inv)
                          </div>
                          <div
                            style={{ height: `${heightPercent}%` }}
                            className="w-12 bg-[#1c454e] rounded-t-xl group-hover:bg-[#acdf00] transition-colors relative"
                          />
                          <span className="text-xs font-mono font-bold text-slate-600 mt-2">{item.month}</span>
                        </div>
                      );
                    })}
                  </div>
                </div>

                {/* State Machine Status Distribution */}
                <div className="bg-white border border-slate-200 p-6 rounded-2xl shadow-sm flex flex-col justify-between">
                  <div>
                    <h3 className="text-sm font-extrabold text-[#1c454e] mb-1">Invoice State Machine Matrix</h3>
                    <p className="text-xs text-slate-500 mb-6">BR-50 compliant status distribution</p>

                    <div className="space-y-4">
                      {[
                        { label: 'Paid (Settled/Terminal)', count: summary.paidInvoicesCount, color: 'bg-[#acdf00]', textColor: 'text-[#1c454e]' },
                        { label: 'Pending (Due Soon)', count: summary.pendingInvoicesCount, color: 'bg-amber-400', textColor: 'text-amber-950' },
                        { label: 'Overdue (Grace Sweeper)', count: summary.overdueInvoicesCount, color: 'bg-rose-500', textColor: 'text-rose-950' },
                        { label: 'Failed (Dead Letter)', count: summary.failedInvoicesCount, color: 'bg-red-800', textColor: 'text-white' }
                      ].map((st, i) => (
                        <div key={i} className="space-y-1.5">
                          <div className="flex justify-between text-xs font-medium">
                            <span className="text-slate-700 font-medium">{st.label}</span>
                            <span className="font-mono font-bold text-slate-900">{st.count}</span>
                          </div>
                          <div className="h-2.5 w-full bg-slate-100 rounded-full overflow-hidden border border-slate-200">
                            <div className={`h-full ${st.color} rounded-full`} style={{ width: `${Math.min((st.count / 1100) * 100, 100)}%` }} />
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="mt-6 p-3.5 bg-[#fbfefa] rounded-xl border border-slate-200 text-[11px] text-slate-600 flex items-center gap-2">
                    <ShieldAlert className="w-4 h-4 text-[#1c454e] shrink-0" />
                    <span>Invariants enforced via EF Core Domain entity setters.</span>
                  </div>
                </div>
              </div>

              {/* Audit Log Preview Table */}
              <div className="bg-white border border-slate-200 rounded-2xl overflow-hidden shadow-sm">
                <div className="p-5 border-b border-slate-200 flex items-center justify-between bg-slate-50/50">
                  <div>
                    <h3 className="text-sm font-extrabold text-[#1c454e]">Real-Time Audit Log Feed</h3>
                    <p className="text-xs text-slate-500">Atomic correlation log trail (BR-41)</p>
                  </div>
                  <button onClick={() => setActiveTab('audit-logs')} className="text-xs font-bold text-[#1c454e] hover:underline flex items-center gap-1">
                    View Complete Trail <ChevronRight className="w-3.5 h-3.5" />
                  </button>
                </div>
                <div className="divide-y divide-slate-100 font-mono text-xs">
                  {auditLogs.slice(0, 4).map((log) => (
                    <div key={log.id} className="p-4 hover:bg-slate-50/80 flex items-center justify-between gap-4 transition-colors">
                      <div className="flex items-center gap-3 min-w-0">
                        <div className="w-2.5 h-2.5 rounded-full bg-[#acdf00] shrink-0 border border-[#1c454e]" />
                        <div>
                          <span className="font-bold text-[#1c454e]">{log.action}</span>
                          <span className="text-slate-500 font-sans ml-2">({log.entityName} #{log.entityId})</span>
                          <p className="text-[11px] text-slate-500 font-sans truncate">{log.details}</p>
                        </div>
                      </div>
                      <span className="text-slate-400 shrink-0 text-[11px]">{log.occurredAt}</span>
                    </div>
                  ))}
                </div>
              </div>

            </div>
          )}

          {/* TAB 2: INVOICES & LIFECYCLE MANAGEMENT */}
          {activeTab === 'invoices' && (
            <div className="space-y-6 animate-fadeIn">

              {/* Filters */}
              <div className="flex flex-col sm:flex-row gap-4 justify-between items-stretch sm:items-center bg-white p-4 rounded-2xl border border-slate-200 shadow-sm">
                <div className="relative flex-1 max-w-md">
                  <Search className="w-4 h-4 absolute left-3.5 top-3 text-slate-400" />
                  <input
                    type="text"
                    placeholder="Search by Invoice # or Customer Name..."
                    value={invoiceSearch}
                    onChange={(e) => setInvoiceSearch(e.target.value)}
                    className="w-full pl-10 pr-4 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:border-[#1c454e]"
                  />
                </div>

                <div className="flex items-center gap-1 bg-slate-100 p-1 rounded-xl border border-slate-200">
                  {['All', 'Pending', 'Paid', 'Overdue', 'Failed'].map((st) => (
                    <button
                      key={st}
                      onClick={() => setInvoiceStatusFilter(st)}
                      className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                        invoiceStatusFilter === st
                          ? 'bg-[#1c454e] text-white shadow-sm'
                          : 'text-slate-600 hover:text-slate-900'
                      }`}
                    >
                      {st}
                    </button>
                  ))}
                </div>
              </div>

              {/* Table */}
              <div className="bg-white border border-slate-200 rounded-2xl overflow-hidden shadow-sm">
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-xs">
                    <thead className="bg-[#1c454e] text-white font-mono uppercase tracking-wider">
                      <tr>
                        <th className="p-4">Invoice #</th>
                        <th className="p-4">Customer</th>
                        <th className="p-4">Plan</th>
                        <th className="p-4">Amount</th>
                        <th className="p-4">Due Date</th>
                        <th className="p-4">Status</th>
                        <th className="p-4 text-right">Action</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {filteredInvoices.map((inv) => (
                        <tr key={inv.id} className="hover:bg-slate-50 transition-colors">
                          <td className="p-4 font-mono font-bold text-[#1c454e]">{inv.id}</td>
                          <td className="p-4 font-semibold text-slate-800">{inv.customerName}</td>
                          <td className="p-4 text-slate-600">{inv.planName}</td>
                          <td className="p-4 font-mono font-bold text-slate-900">
                            ${inv.amount.toFixed(2)}
                          </td>
                          <td className="p-4 font-mono text-slate-500">{inv.dueDate}</td>
                          <td className="p-4">
                            <StatusBadge status={inv.status} />
                          </td>
                          <td className="p-4 text-right">
                            <button
                              onClick={() => setSelectedInvoice(inv)}
                              className="px-3.5 py-1.5 bg-[#1c454e] hover:bg-[#255863] text-white text-xs font-bold rounded-xl transition-colors shadow-sm"
                            >
                              Transition Status
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

            </div>
          )}

          {/* TAB 3: CUSTOMERS & PLANS */}
          {activeTab === 'customers' && (
            <div className="space-y-6 animate-fadeIn">
              <div className="bg-white border border-slate-200 rounded-2xl overflow-hidden shadow-sm">
                <div className="p-5 border-b border-slate-200 bg-slate-50">
                  <h3 className="text-sm font-extrabold text-[#1c454e]">Master Customer Subscriptions</h3>
                  <p className="text-xs text-slate-500">Subscribed plans evaluated during automated billing scans</p>
                </div>
                <table className="w-full text-left text-xs">
                  <thead className="bg-[#1c454e] text-white font-mono uppercase">
                    <tr>
                      <th className="p-4">ID</th>
                      <th className="p-4">Customer Name & Email</th>
                      <th className="p-4">Plan</th>
                      <th className="p-4">Cycle Billing</th>
                      <th className="p-4">Next Billing Date</th>
                      <th className="p-4">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {customers.map((c) => (
                      <tr key={c.id} className="hover:bg-slate-50">
                        <td className="p-4 font-mono text-slate-500">{c.id}</td>
                        <td className="p-4">
                          <div className="font-bold text-[#1c454e]">{c.name}</div>
                          <div className="text-[11px] text-slate-500">{c.email}</div>
                        </td>
                        <td className="p-4 font-semibold text-slate-700">{c.plan}</td>
                        <td className="p-4 font-mono font-bold text-[#1c454e]">${c.amount.toFixed(2)}/mo</td>
                        <td className="p-4 font-mono text-slate-500">{c.nextBillingDate}</td>
                        <td className="p-4">
                          <span className={`px-2.5 py-1 rounded-full text-[11px] font-bold border ${
                            c.status === 'Active' ? 'bg-[#acdf00]/30 text-[#1c454e] border-[#acdf00]' : 'bg-amber-100 text-amber-800 border-amber-300'
                          }`}>
                            {c.status}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* TAB 4: AUDIT LOGS */}
          {activeTab === 'audit-logs' && (
            <div className="space-y-6 animate-fadeIn">
              <div className="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm">
                <h3 className="text-sm font-extrabold text-[#1c454e] mb-1">System Audit & Poison Queue Logs</h3>
                <p className="text-xs text-slate-500 mb-6">BR-41 & BR-28 Audit Trail recording dead-letter events and transitions</p>

                <div className="space-y-4">
                  {auditLogs.map((log) => (
                    <div key={log.id} className="p-4 bg-slate-50 rounded-xl border border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-4 font-mono text-xs">
                      <div className="space-y-1">
                        <div className="flex items-center gap-2">
                          <span className="px-2 py-0.5 rounded bg-[#1c454e] text-[#acdf00] font-bold text-[10px]">
                            {log.action}
                          </span>
                          <span className="text-[#1c454e] font-bold">{log.entityName} #{log.entityId}</span>
                        </div>
                        <p className="text-slate-600 font-sans text-xs">{log.details}</p>
                        <div className="text-[11px] text-slate-400 flex items-center gap-2">
                          <span>CorrelationId: {log.correlationId}</span>
                          <span>•</span>
                          <span>Actor: {log.performedBy}</span>
                        </div>
                      </div>
                      <div className="text-slate-400 text-right shrink-0 text-[11px]">
                        {log.occurredAt}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* TAB 5: API CONNECTION SETTINGS */}
          {activeTab === 'settings' && (
            <div className="space-y-6 animate-fadeIn">
              <div className="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm space-y-6">
                <div>
                  <h3 className="text-sm font-extrabold text-[#1c454e] mb-1">.NET 8 API Connection Settings</h3>
                  <p className="text-xs text-slate-500">Connect this React Web Dashboard to your running `BillingSystem.Api` host</p>
                </div>

                <div className="space-y-4 max-w-xl">
                  <div>
                    <label className="block text-xs font-semibold text-slate-700 mb-1.5">Base API URL</label>
                    <input
                      type="text"
                      value={apiUrl}
                      onChange={(e) => setApiUrl(e.target.value)}
                      className="w-full px-3.5 py-2 bg-slate-50 border border-slate-300 rounded-xl text-xs font-mono text-slate-900 focus:outline-none focus:border-[#1c454e]"
                    />
                  </div>

                  <div className="p-4 bg-slate-50 rounded-xl border border-slate-200 space-y-3">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-slate-600 font-medium">Liveness Probe (`/health/live`)</span>
                      <span className={`font-mono font-bold ${apiHealth.live ? 'text-emerald-600' : 'text-rose-600'}`}>
                        {apiHealth.live ? '200 OK' : 'OFFLINE'}
                      </span>
                    </div>
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-slate-600 font-medium">Readiness Probe (`/health/ready` SQL + Queue)</span>
                      <span className={`font-mono font-bold ${apiHealth.ready ? 'text-emerald-600' : 'text-rose-600'}`}>
                        {apiHealth.ready ? '200 HEALTHY' : 'OFFLINE'}
                      </span>
                    </div>

                    <button
                      onClick={checkHealth}
                      className="w-full py-2 bg-[#1c454e] hover:bg-[#255863] text-white text-xs font-bold rounded-xl transition-colors shadow-sm"
                    >
                      Test Connection
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}

        </div>
      </main>

      {/* --- BR-50 STATE MACHINE TRANSITION MODAL --- */}
      {selectedInvoice && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white border border-slate-200 rounded-2xl max-w-md w-full p-6 space-y-6 shadow-2xl animate-scaleIn">

            <div className="flex items-center justify-between border-b border-slate-100 pb-4">
              <div>
                <h3 className="text-sm font-extrabold text-[#1c454e]">State Machine Transition</h3>
                <p className="text-xs font-mono text-slate-500">{selectedInvoice.id} • {selectedInvoice.customerName}</p>
              </div>
              <button onClick={() => setSelectedInvoice(null)} className="text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Current State Display */}
            <div className="p-3 bg-slate-50 rounded-xl border border-slate-200 flex items-center justify-between">
              <span className="text-xs text-slate-600 font-medium">Current State:</span>
              <StatusBadge status={selectedInvoice.status} />
            </div>

            {/* Transitions Check */}
            {STATE_TRANSITIONS[selectedInvoice.status]?.length === 0 ? (
              <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-800">
                ⚠️ State <strong>{selectedInvoice.status}</strong> is terminal & immutable per BR-51. No further state transitions permitted.
              </div>
            ) : (
              <div className="space-y-4">
                <div>
                  <label className="block text-xs font-bold text-[#1c454e] mb-2">
                    Allowed Target States (BR-50 Matrix)
                  </label>
                  <div className="grid grid-cols-2 gap-2">
                    {STATE_TRANSITIONS[selectedInvoice.status]?.map((target) => (
                      <button
                        key={target}
                        onClick={() => setStatusChangeTarget(target)}
                        className={`p-2.5 rounded-xl border text-xs font-bold text-center transition-all ${
                          statusChangeTarget === target
                            ? 'bg-[#1c454e] border-[#1c454e] text-[#acdf00] shadow-md'
                            : 'bg-slate-50 border-slate-200 text-slate-700 hover:border-slate-300'
                        }`}
                      >
                        → {target}
                      </button>
                    ))}
                  </div>
                </div>

                {/* Reason Input */}
                <div>
                  <label className="block text-xs font-bold text-[#1c454e] mb-1">
                    Transition Reason {statusChangeTarget === 'Failed' && <span className="text-rose-600">(REQUIRED - BR-52)</span>}
                  </label>
                  <textarea
                    rows={2}
                    placeholder="Provide justification for status transition..."
                    value={statusChangeReason}
                    onChange={(e) => setStatusChangeReason(e.target.value)}
                    className="w-full p-2.5 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-800 placeholder-slate-400 focus:outline-none focus:border-[#1c454e]"
                  />
                </div>
              </div>
            )}

            {/* Modal Actions */}
            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                onClick={() => setSelectedInvoice(null)}
                className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-xl"
              >
                Cancel
              </button>

              {STATE_TRANSITIONS[selectedInvoice.status]?.length > 0 && (
                <button
                  onClick={handleExecuteStatusChange}
                  disabled={!statusChangeTarget}
                  className="px-4 py-2 bg-[#acdf00] hover:bg-[#bbf000] text-[#1c454e] text-xs font-extrabold rounded-xl disabled:opacity-50 transition-colors shadow-md"
                >
                  Confirm Transition
                </button>
              )}
            </div>

          </div>
        </div>
      )}

    </div>
  );
}