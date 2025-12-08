import React, { useMemo, useState } from "react";

import AssignmentIcon from "@mui/icons-material/Assignment";
import ErrorIcon from "@mui/icons-material/Error";
import FilterListIcon from "@mui/icons-material/FilterList";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import PlayCircleIcon from "@mui/icons-material/PlayCircle";
import RefreshIcon from "@mui/icons-material/Refresh";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import {
  DataGrid,
  GridColDef,
  GridRenderCellParams,
  GridRowParams,
} from "@mui/x-data-grid";
import { formatDistanceToNow } from "date-fns";
import { useNavigate } from "react-router-dom";

import type {
  ServiceCategory,
  ServiceRequestSummaryDto,
  ServiceStatus,
} from "@/types/api";

import { PageHeader } from "@/components/layout";
import {
  DataGridNoRowsOverlay,
  MonospaceIdCell,
  StatusBadge,
} from "@/components/patterns";

// ============================================================================
// Mock Data - Replace with real API when backend is ready
// ============================================================================

// This is mock data for demonstration. In production, you'd use useQuery
// with a fulfillment queue endpoint like /services/queue
const mockFulfillmentQueue: ServiceRequestSummaryDto[] = [
  {
    id: "01HXN2ABCD1234567890ABCD",
    orderId: "01HXN2EFGH1234567890EFGH",
    customerId: "01HXN2IJKL1234567890IJKL",
    serviceTypeCode: "SSN_TRACE",
    category: "Identity",
    tier: 1,
    status: "Pending",
    attemptCount: 0,
    maxAttempts: 3,
    createdAt: new Date(Date.now() - 3600000).toISOString(),
  },
  {
    id: "01HXN2MNOP1234567890MNOP",
    orderId: "01HXN2QRST1234567890QRST",
    customerId: "01HXN2IJKL1234567890IJKL",
    serviceTypeCode: "STATE_CRIM",
    category: "Criminal",
    tier: 2,
    status: "InProgress",
    vendorCode: "CHECKR",
    vendorReferenceId: "CHK-123456",
    attemptCount: 1,
    maxAttempts: 3,
    createdAt: new Date(Date.now() - 7200000).toISOString(),
    dispatchedAt: new Date(Date.now() - 3600000).toISOString(),
    scopeType: "State",
    scopeValue: "CA",
  },
  {
    id: "01HXN2UVWX1234567890UVWX",
    orderId: "01HXN2YZ121234567890YZ12",
    customerId: "01HXN3ABCD1234567890ABCD",
    serviceTypeCode: "FED_CRIM",
    category: "Criminal",
    tier: 2,
    status: "Failed",
    vendorCode: "CHECKR",
    attemptCount: 2,
    maxAttempts: 3,
    lastError: "Vendor timeout after 30 seconds",
    createdAt: new Date(Date.now() - 86400000).toISOString(),
    dispatchedAt: new Date(Date.now() - 82800000).toISOString(),
    failedAt: new Date(Date.now() - 79200000).toISOString(),
  },
  {
    id: "01HXN3EFGH1234567890EFGH",
    orderId: "01HXN3IJKL1234567890IJKL",
    customerId: "01HXN2IJKL1234567890IJKL",
    serviceTypeCode: "TWN_EMP",
    category: "Employment",
    tier: 3,
    status: "Dispatched",
    vendorCode: "TWN",
    vendorReferenceId: "TWN-789012",
    attemptCount: 1,
    maxAttempts: 3,
    createdAt: new Date(Date.now() - 172800000).toISOString(),
    dispatchedAt: new Date(Date.now() - 86400000).toISOString(),
  },
];

// ============================================================================
// Stats Cards Component
// ============================================================================

interface StatCardProps {
  label: string;
  value: number;
  icon: React.ReactElement;
  color?: string;
}

const StatCard = ({ label, value, icon, color }: StatCardProps) => (
  <Card variant="outlined" sx={{ flex: 1 }}>
    <CardContent sx={{ py: 2 }}>
      <Stack direction="row" spacing={2} alignItems="center">
        <Box sx={{ color: color ?? "primary.main" }}>{icon}</Box>
        <Box>
          <Typography variant="h4" fontWeight={600}>
            {value}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {label}
          </Typography>
        </Box>
      </Stack>
    </CardContent>
  </Card>
);

// ============================================================================
// Category Chip Component
// ============================================================================

const categoryColors: Record<
  ServiceCategory,
  "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"
> = {
  Criminal: "error",
  Identity: "primary",
  Employment: "info",
  Education: "success",
  Driving: "warning",
  Credit: "secondary",
  Drug: "error",
  Civil: "default",
  Reference: "info",
  Healthcare: "success",
  Custom: "default",
};

// ============================================================================
// Fulfillment Dashboard Page
// ============================================================================

const FulfillmentDashboardPage = () => {
  const navigate = useNavigate();
  const [statusFilter, setStatusFilter] = useState<ServiceStatus | "all">(
    "all",
  );
  const [categoryFilter, setCategoryFilter] = useState<ServiceCategory | "all">(
    "all",
  );
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 25,
  });

  // In production, this would be a useQuery hook
  const fulfillmentQueue = mockFulfillmentQueue;
  const isLoading = false;
  const error = null;

  // Filter data
  const filteredQueue = useMemo(() => {
    return fulfillmentQueue.filter((item) => {
      if (statusFilter !== "all" && item.status !== statusFilter) return false;
      if (categoryFilter !== "all" && item.category !== categoryFilter)
        return false;
      return true;
    });
  }, [fulfillmentQueue, statusFilter, categoryFilter]);

  // Calculate stats
  const stats = useMemo(() => {
    return {
      pending: fulfillmentQueue.filter((s) => s.status === "Pending").length,
      inProgress: fulfillmentQueue.filter(
        (s) => s.status === "InProgress" || s.status === "Dispatched",
      ).length,
      failed: fulfillmentQueue.filter((s) => s.status === "Failed").length,
      total: fulfillmentQueue.length,
    };
  }, [fulfillmentQueue]);

  // Get unique categories for filter
  const availableCategories = useMemo(() => {
    const categories = new Set(fulfillmentQueue.map((s) => s.category));
    return Array.from(categories).sort();
  }, [fulfillmentQueue]);

  const handleRowClick = (params: GridRowParams<ServiceRequestSummaryDto>) => {
    navigate(`/orders/${params.row.orderId}`);
  };

  const columns: GridColDef<ServiceRequestSummaryDto>[] = [
    {
      field: "serviceTypeCode",
      headerName: "Service",
      width: 150,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" fontWeight={500}>
          {params.value}
        </Typography>
      ),
    },
    {
      field: "category",
      headerName: "Category",
      width: 120,
      renderCell: (params: GridRenderCellParams<ServiceRequestSummaryDto>) => (
        <Chip
          label={params.value}
          size="small"
          color={categoryColors[params.value as ServiceCategory]}
          variant="outlined"
        />
      ),
    },
    {
      field: "status",
      headerName: "Status",
      width: 120,
      renderCell: (params: GridRenderCellParams) => (
        <StatusBadge type="service" status={params.value} />
      ),
    },
    {
      field: "tier",
      headerName: "Tier",
      width: 80,
      renderCell: (params: GridRenderCellParams) => (
        <Chip
          label={`T${params.value}`}
          size="small"
          color="primary"
          variant="outlined"
        />
      ),
    },
    {
      field: "orderId",
      headerName: "Order",
      width: 140,
      renderCell: (params: GridRenderCellParams) => (
        <MonospaceIdCell id={params.value} />
      ),
    },
    {
      field: "vendorCode",
      headerName: "Vendor",
      width: 100,
      renderCell: (params: GridRenderCellParams) =>
        params.value ? (
          <Typography variant="body2">{params.value}</Typography>
        ) : (
          <Typography variant="body2" color="text.secondary">
            —
          </Typography>
        ),
    },
    {
      field: "scopeValue",
      headerName: "Scope",
      width: 100,
      renderCell: (params: GridRenderCellParams<ServiceRequestSummaryDto>) =>
        params.value ? (
          <Tooltip title={params.row.scopeType ?? "Scope"}>
            <Typography variant="body2">{params.value}</Typography>
          </Tooltip>
        ) : (
          <Typography variant="body2" color="text.secondary">
            —
          </Typography>
        ),
    },
    {
      field: "attemptCount",
      headerName: "Attempts",
      width: 90,
      renderCell: (params: GridRenderCellParams<ServiceRequestSummaryDto>) => (
        <Typography variant="body2">
          {params.value}/{params.row.maxAttempts}
        </Typography>
      ),
    },
    {
      field: "createdAt",
      headerName: "Age",
      width: 120,
      renderCell: (params: GridRenderCellParams) => (
        <Typography variant="body2" color="text.secondary">
          {formatDistanceToNow(new Date(params.value), { addSuffix: false })}
        </Typography>
      ),
    },
    {
      field: "lastError",
      headerName: "Error",
      flex: 1,
      minWidth: 200,
      renderCell: (params: GridRenderCellParams) =>
        params.value ? (
          <Tooltip title={params.value}>
            <Typography
              variant="body2"
              color="error.main"
              sx={{
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
              }}
            >
              {params.value}
            </Typography>
          </Tooltip>
        ) : null,
    },
  ];

  return (
    <>
      <PageHeader
        title="Fulfillment Dashboard"
        subtitle="Monitor and manage background check service requests"
        action={
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => {
              // In production: queryClient.invalidateQueries(["fulfillment"])
            }}
          >
            Refresh
          </Button>
        }
      />

      {/* Stats Row */}
      <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
        <StatCard
          label="Pending"
          value={stats.pending}
          icon={<HourglassEmptyIcon fontSize="large" />}
          color="grey.500"
        />
        <StatCard
          label="In Progress"
          value={stats.inProgress}
          icon={<PlayCircleIcon fontSize="large" />}
          color="warning.main"
        />
        <StatCard
          label="Failed"
          value={stats.failed}
          icon={<ErrorIcon fontSize="large" />}
          color="error.main"
        />
        <StatCard
          label="Total Active"
          value={stats.total}
          icon={<AssignmentIcon fontSize="large" />}
          color="primary.main"
        />
      </Stack>

      {/* Filters */}
      <Card variant="outlined" sx={{ mb: 3 }}>
        <CardContent>
          <Stack direction="row" spacing={2} alignItems="center">
            <FilterListIcon color="action" />
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Status</InputLabel>
              <Select
                value={statusFilter}
                label="Status"
                onChange={(e) =>
                  setStatusFilter(e.target.value as ServiceStatus | "all")
                }
              >
                <MenuItem value="all">All Statuses</MenuItem>
                <MenuItem value="Pending">Pending</MenuItem>
                <MenuItem value="Dispatched">Dispatched</MenuItem>
                <MenuItem value="InProgress">In Progress</MenuItem>
                <MenuItem value="Failed">Failed</MenuItem>
              </Select>
            </FormControl>
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>Category</InputLabel>
              <Select
                value={categoryFilter}
                label="Category"
                onChange={(e) =>
                  setCategoryFilter(e.target.value as ServiceCategory | "all")
                }
              >
                <MenuItem value="all">All Categories</MenuItem>
                {availableCategories.map((cat) => (
                  <MenuItem key={cat} value={cat}>
                    {cat}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Typography variant="body2" color="text.secondary" sx={{ ml: 2 }}>
              Showing {filteredQueue.length} of {fulfillmentQueue.length} items
            </Typography>
          </Stack>
        </CardContent>
      </Card>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load fulfillment queue. Please try again.
        </Alert>
      )}

      {/* Data Grid */}
      <DataGrid
        rows={filteredQueue}
        columns={columns}
        getRowId={(row) => row.id}
        loading={isLoading}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 25, 50, 100]}
        onRowClick={handleRowClick}
        slots={{
          noRowsOverlay: () => (
            <DataGridNoRowsOverlay message="No service requests in the fulfillment queue." />
          ),
        }}
        sx={{
          minHeight: 500,
          "& .MuiDataGrid-row": { cursor: "pointer" },
          "& .MuiDataGrid-row:hover": { bgcolor: "action.hover" },
        }}
        disableRowSelectionOnClick
      />
    </>
  );
};

export default FulfillmentDashboardPage;
