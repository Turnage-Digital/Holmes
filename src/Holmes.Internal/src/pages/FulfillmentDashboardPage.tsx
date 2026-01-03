import React, { useMemo, useState } from "react";

import AssignmentIcon from "@mui/icons-material/Assignment";
import ErrorIcon from "@mui/icons-material/Error";
import FilterListIcon from "@mui/icons-material/FilterList";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import PlayCircleIcon from "@mui/icons-material/PlayCircle";
import RefreshIcon from "@mui/icons-material/Refresh";
import {
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
import { useQueryClient } from "@tanstack/react-query";
import { formatDistanceToNow } from "date-fns";
import { useNavigate } from "react-router-dom";

import type {
  ServiceCategory,
  ServiceStatus,
  ServiceSummaryDto,
} from "@/types/api";

import { PageHeader } from "@/components/layout";
import {
  DataGridNoRowsOverlay,
  MonospaceIdCell,
  StatusBadge,
} from "@/components/patterns";
import { queryKeys, useFulfillmentQueue } from "@/hooks/api";

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
  const queryClient = useQueryClient();
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

  // Build query based on filters - API uses 1-based pages
  const query = useMemo(
    () => ({
      page: paginationModel.page + 1,
      pageSize: paginationModel.pageSize,
      status:
        statusFilter === "all"
          ? undefined
          : ([statusFilter] as ServiceStatus[]),
      category:
        categoryFilter === "all"
          ? undefined
          : ([categoryFilter] as ServiceCategory[]),
    }),
    [paginationModel, statusFilter, categoryFilter],
  );

  // Fetch data from real API
  const { data, isLoading } = useFulfillmentQueue(query);
  const totalCount = data?.totalItems ?? 0;

  // Memoize the fulfillment queue to prevent unnecessary re-renders
  const fulfillmentQueue = useMemo(() => data?.items ?? [], [data?.items]);

  // Calculate stats from current page data
  const stats = useMemo(() => {
    return {
      pending: fulfillmentQueue.filter((s) => s.status === "Pending").length,
      inProgress: fulfillmentQueue.filter(
        (s) => s.status === "InProgress" || s.status === "Dispatched",
      ).length,
      failed: fulfillmentQueue.filter((s) => s.status === "Failed").length,
      total: totalCount,
    };
  }, [fulfillmentQueue, totalCount]);

  // Get unique categories for filter from current page
  const availableCategories = useMemo(() => {
    const categories = new Set(fulfillmentQueue.map((s) => s.category));
    return Array.from(categories).sort();
  }, [fulfillmentQueue]);

  // Row count for server-side pagination - always use totalCount since filtering is server-side
  const rowCount = totalCount;

  const handleRowClick = (params: GridRowParams<ServiceSummaryDto>) => {
    navigate(`/orders/${params.row.orderId}`);
  };

  const columns: GridColDef<ServiceSummaryDto>[] = [
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
      renderCell: (params: GridRenderCellParams<ServiceSummaryDto>) => (
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
      renderCell: (params: GridRenderCellParams) => {
        if (params.value) {
          return <Typography variant="body2">{params.value}</Typography>;
        }
        return (
          <Typography variant="body2" color="text.secondary">
            —
          </Typography>
        );
      },
    },
    {
      field: "scopeValue",
      headerName: "Scope",
      width: 100,
      renderCell: (params: GridRenderCellParams<ServiceSummaryDto>) => {
        if (params.value) {
          return (
            <Tooltip title={params.row.scopeType ?? "Scope"}>
              <Typography variant="body2">{params.value}</Typography>
            </Tooltip>
          );
        }
        return (
          <Typography variant="body2" color="text.secondary">
            —
          </Typography>
        );
      },
    },
    {
      field: "attemptCount",
      headerName: "Attempts",
      width: 90,
      renderCell: (params: GridRenderCellParams<ServiceSummaryDto>) => (
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
      renderCell: (params: GridRenderCellParams) => {
        if (!params.value) {
          return null;
        }
        return (
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
        );
      },
    },
  ];

  return (
    <>
      <PageHeader
        title="Fulfillment Dashboard"
        subtitle="Monitor and manage background check services"
        action={
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            disabled={isLoading}
            onClick={() => {
              queryClient.invalidateQueries({
                queryKey: queryKeys.fulfillmentQueue(query),
              });
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
              Showing {fulfillmentQueue.length} of {totalCount} items
            </Typography>
          </Stack>
        </CardContent>
      </Card>

      {/* Data Grid */}
      <DataGrid
        rows={fulfillmentQueue}
        columns={columns}
        getRowId={(row) => row.id}
        loading={isLoading}
        paginationMode="server"
        rowCount={rowCount}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 25, 50, 100]}
        onRowClick={handleRowClick}
        slots={{
          noRowsOverlay: () => (
            <DataGridNoRowsOverlay message="No services in the fulfillment queue." />
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
