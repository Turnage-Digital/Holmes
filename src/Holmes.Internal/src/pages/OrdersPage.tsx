import React, { useCallback, useEffect, useMemo, useState } from "react";

import {
  Alert,
  Box,
  Button,
  Chip,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
} from "@mui/material";
import { DataGrid, GridColDef, GridRowParams } from "@mui/x-data-grid";
import { useQueryClient } from "@tanstack/react-query";
import { formatDistanceToNow } from "date-fns";
import { useNavigate, useSearchParams } from "react-router-dom";

import type {
  OrderChangeEvent,
  OrderSummaryDto,
  PaginatedResult,
} from "@/types/api";

import { PageHeader } from "@/components/layout";
import NewOrderDialog from "@/components/orders/NewOrderDialog";
import { DataGridNoRowsOverlay } from "@/components/patterns";
import {
  createOrderChangesStream,
  queryKeys,
  useCustomer,
  useOrders,
  useSubject,
} from "@/hooks/api";

// ============================================================================
// Status Filter
// ============================================================================

type StatusFilter = "all" | "active" | "completed" | "canceled";

const statusFilterMap: Record<StatusFilter, string[] | undefined> = {
  all: undefined,
  active: ["Invited", "IntakeInProgress", "IntakeComplete"],
  completed: [
    "ReadyForRouting",
    "RoutingInProgress",
    "ReadyForReport",
    "Closed",
  ],
  canceled: ["Canceled", "Blocked"],
};

// ============================================================================
// Status Badge Component
// ============================================================================

const getStatusColor = (status: string) => {
  switch (status) {
    case "ReadyForRouting":
    case "Closed":
      return "success";
    case "IntakeComplete":
    case "ReadyForReport":
      return "info";
    case "IntakeInProgress":
    case "RoutingInProgress":
      return "warning";
    case "Invited":
    case "Created":
      return "default";
    case "Blocked":
      return "error";
    case "Canceled":
      return "default";
    default:
      return "default";
  }
};

const formatStatus = (status: string) => {
  switch (status) {
    case "ReadyForRouting":
      return "Ready for Routing";
    case "IntakeComplete":
      return "Intake Complete";
    case "IntakeInProgress":
      return "In Progress";
    case "ReadyForReport":
      return "Ready for Report";
    case "RoutingInProgress":
      return "Routing";
    default:
      return status;
  }
};

const StatusBadge = ({ status }: { status: string }) => (
  <Chip
    label={formatStatus(status)}
    size="small"
    color={getStatusColor(status)}
    variant="outlined"
  />
);

// ============================================================================
// Cell Renderers with Data Fetching
// ============================================================================

const SubjectCell = ({ subjectId }: { subjectId: string }) => {
  const { data: subject } = useSubject(subjectId);

  if (!subject) {
    return <>{subjectId.slice(0, 8)}…</>;
  }

  const name = [subject.givenName, subject.familyName]
    .filter(Boolean)
    .join(" ");
  return <>{name || subject.email || `${subjectId.slice(0, 8)}…`}</>;
};

const CustomerCell = ({ customerId }: { customerId: string }) => {
  const { data: customer } = useCustomer(customerId);
  return <span>{customer?.name ?? `${customerId.slice(0, 8)}…`}</span>;
};

// ============================================================================
// Orders Page
// ============================================================================

const OrdersPage = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();

  const [newOrderOpen, setNewOrderOpen] = useState(false);
  const [sseError, setSseError] = useState<string | null>(null);

  // Pagination state
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 25,
  });

  // Status filter from URL or default
  const statusParam = searchParams.get("status");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>(() => {
    if (statusParam) {
      // Check if it's a specific status
      const validFilters: StatusFilter[] = [
        "all",
        "active",
        "completed",
        "canceled",
      ];
      if (validFilters.includes(statusParam as StatusFilter)) {
        return statusParam as StatusFilter;
      }
    }
    return "active";
  });

  // Build query based on filter
  const statusArray = useMemo(() => {
    // If URL has a specific status, use that
    if (
      statusParam &&
      !["all", "active", "completed", "canceled"].includes(statusParam)
    ) {
      return [statusParam];
    }
    return statusFilterMap[statusFilter];
  }, [statusFilter, statusParam]);

  const {
    data: ordersData,
    isLoading,
    error,
  } = useOrders({
    page: paginationModel.page + 1,
    pageSize: paginationModel.pageSize,
    status: statusArray,
  });

  // SSE for real-time updates
  useEffect(() => {
    let hasConnected = false;
    const eventSource = createOrderChangesStream();

    eventSource.onopen = () => {
      hasConnected = true;
      setSseError(null);
    };

    eventSource.onmessage = (event) => {
      try {
        const payload: OrderChangeEvent = JSON.parse(event.data);

        // Update the order in cache
        queryClient.setQueryData<PaginatedResult<OrderSummaryDto>>(
          queryKeys.orders({
            page: paginationModel.page + 1,
            pageSize: paginationModel.pageSize,
            status: statusArray,
          }),
          (current) => {
            if (!current) return current;

            const existingIndex = current.items.findIndex(
              (o) => o.orderId === payload.orderId,
            );

            if (existingIndex === -1) {
              // New order or not in current view - refetch
              queryClient.invalidateQueries({ queryKey: ["orders"] });
              return current;
            }

            // Update existing order
            const updated = [...current.items];
            updated[existingIndex] = {
              ...updated[existingIndex],
              status: payload.status,
              lastStatusReason: payload.reason ?? null,
              lastUpdatedAt: payload.changedAt,
            };

            return { ...current, items: updated };
          },
        );
      } catch {
        // Ignore parse errors
      }
    };

    eventSource.onerror = () => {
      // Only show error if we were previously connected
      if (hasConnected) {
        setSseError("Live updates disconnected.");
      }
    };

    return () => {
      eventSource.close();
    };
  }, [queryClient, paginationModel, statusArray]);

  // Handlers
  const handleStatusFilterChange = useCallback(
    (_: React.MouseEvent<HTMLElement>, newFilter: StatusFilter | null) => {
      if (newFilter) {
        setStatusFilter(newFilter);
        setSearchParams(newFilter === "active" ? {} : { status: newFilter });
        setPaginationModel((prev) => ({ ...prev, page: 0 }));
      }
    },
    [setSearchParams],
  );

  const handleRowClick = useCallback(
    (params: GridRowParams<OrderSummaryDto>) => {
      navigate(`/orders/${params.row.orderId}`);
    },
    [navigate],
  );

  // Columns
  const columns: GridColDef<OrderSummaryDto>[] = useMemo(
    () => [
      {
        field: "orderId",
        headerName: "Order",
        width: 140,
        renderCell: (params) => (
          <span style={{ fontFamily: "monospace" }}>
            {params.value?.slice(0, 12)}…
          </span>
        ),
      },
      {
        field: "status",
        headerName: "Status",
        width: 160,
        renderCell: (params) => <StatusBadge status={params.value} />,
      },
      {
        field: "subjectId",
        headerName: "Subject",
        width: 200,
        renderCell: (params) => <SubjectCell subjectId={params.value} />,
      },
      {
        field: "customerId",
        headerName: "Customer",
        width: 200,
        renderCell: (params) => <CustomerCell customerId={params.value} />,
      },
      {
        field: "lastUpdatedAt",
        headerName: "Last Updated",
        width: 180,
        renderCell: (params) =>
          formatDistanceToNow(new Date(params.value), { addSuffix: true }),
      },
      {
        field: "lastStatusReason",
        headerName: "Reason",
        flex: 1,
        minWidth: 200,
      },
    ],
    [],
  );

  return (
    <>
      <PageHeader
        title="Orders"
        subtitle="Monitor and manage intake and workflow progress"
        action={
          <Button variant="contained" onClick={() => setNewOrderOpen(true)}>
            New Order
          </Button>
        }
      />

      {sseError && (
        <Alert
          severity="warning"
          onClose={() => setSseError(null)}
          sx={{ mb: 2 }}
        >
          {sseError}
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load orders. Please try again.
        </Alert>
      )}

      <Stack spacing={2}>
        {/* Status Filter */}
        <Box>
          <ToggleButtonGroup
            value={statusFilter}
            exclusive
            onChange={handleStatusFilterChange}
            size="small"
          >
            <ToggleButton value="active">Active</ToggleButton>
            <ToggleButton value="completed">Completed</ToggleButton>
            <ToggleButton value="canceled">Canceled</ToggleButton>
            <ToggleButton value="all">All</ToggleButton>
          </ToggleButtonGroup>
        </Box>

        {/* Orders Grid */}
        <DataGrid
          rows={ordersData?.items ?? []}
          columns={columns}
          getRowId={(row) => row.orderId}
          loading={isLoading}
          paginationModel={paginationModel}
          onPaginationModelChange={setPaginationModel}
          pageSizeOptions={[10, 25, 50]}
          paginationMode="server"
          rowCount={ordersData?.totalItems ?? 0}
          onRowClick={handleRowClick}
          slots={{
            noRowsOverlay: () => (
              <DataGridNoRowsOverlay message="No orders found" />
            ),
          }}
          sx={{
            minHeight: 400,
            "& .MuiDataGrid-row": {
              cursor: "pointer",
            },
          }}
          disableRowSelectionOnClick
        />
      </Stack>

      <NewOrderDialog
        open={newOrderOpen}
        onClose={() => setNewOrderOpen(false)}
      />
    </>
  );
};

export default OrdersPage;
