import React, { useCallback, useEffect, useMemo, useState } from "react";

import {
  Alert,
  Box,
  Button,
  Chip,
  FormControl,
  InputLabel,
  ListSubheader,
  MenuItem,
  Pagination,
  Select,
  type SelectChangeEvent,
  Skeleton,
  Stack,
  useMediaQuery,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { DataGrid, GridColDef, GridRowParams } from "@mui/x-data-grid";
import { useQueryClient } from "@tanstack/react-query";
import { useNavigate, useSearchParams } from "react-router-dom";

import type {
  OrderChangeEvent,
  OrderStatus,
  OrderSummaryDto,
  PaginatedResult,
} from "@/types/api";

import { PageHeader } from "@/components/layout";
import { NewOrderDialog, OrderCard } from "@/components/orders";
import {
  CustomerNameCell,
  DataGridNoRowsOverlay,
  MonospaceIdCell,
  RelativeTimeCell,
  SubjectNameCell,
} from "@/components/patterns";
import { createOrderChangesStream, queryKeys, useOrders } from "@/hooks/api";
import {
  closedStatuses,
  getOrderStatusColor,
  getOrderStatusLabel,
  isOrderStatus,
  openStatuses,
  orderStatuses,
} from "@/lib/status";

// ============================================================================
// Status Filter
// ============================================================================

type StatusFilter = OrderStatus | "open" | "closed" | "all";

const statusFilterMap: Record<StatusFilter, OrderStatus[] | undefined> = {
  open: openStatuses,
  closed: closedStatuses,
  all: undefined,
  ...Object.fromEntries(orderStatuses.map((s) => [s, [s]])),
} as Record<StatusFilter, OrderStatus[] | undefined>;

const isStatusFilter = (value: string | null): value is StatusFilter =>
  value === "open" ||
  value === "closed" ||
  value === "all" ||
  isOrderStatus(value);

// ============================================================================
// Status Badge Component
// ============================================================================

const OrderStatusBadge = ({ status }: { status: string }) => (
  <Chip
    label={getOrderStatusLabel(status)}
    size="small"
    color={getOrderStatusColor(status)}
    variant="outlined"
  />
);

// ============================================================================
// Orders Page
// ============================================================================

const OrdersPage = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));

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
    if (isStatusFilter(statusParam)) {
      return statusParam;
    }
    return "open";
  });

  useEffect(() => {
    if (isStatusFilter(statusParam)) {
      if (statusFilter !== statusParam) {
        setStatusFilter(statusParam);
      }
      return;
    }

    if (!statusParam && statusFilter !== "open") {
      setStatusFilter("open");
    }
  }, [statusFilter, statusParam]);

  // Build query based on filter
  const statusArray = useMemo(
    () => statusFilterMap[statusFilter],
    [statusFilter],
  );

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

    const handleOrderChange = (event: MessageEvent) => {
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

    eventSource.addEventListener("order.change", handleOrderChange);

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
    (event: SelectChangeEvent) => {
      const newFilter = event.target.value as StatusFilter;
      setStatusFilter(newFilter);
      if (newFilter === "open") {
        setSearchParams({});
      } else {
        setSearchParams({ status: newFilter });
      }
      setPaginationModel((prev) => ({ ...prev, page: 0 }));
    },
    [setSearchParams],
  );

  const handleRowClick = useCallback(
    (params: GridRowParams<OrderSummaryDto>) => {
      navigate(`/orders/${params.row.orderId}`);
    },
    [navigate],
  );

  const handleOrderClick = useCallback(
    (orderId: string) => {
      navigate(`/orders/${orderId}`);
    },
    [navigate],
  );

  const handleMobilePageChange = useCallback(
    (_event: React.ChangeEvent<unknown>, page: number) => {
      setPaginationModel((prev) => ({ ...prev, page: page - 1 }));
    },
    [],
  );

  // Columns
  const columns: GridColDef<OrderSummaryDto>[] = useMemo(
    () => [
      {
        field: "orderId",
        headerName: "Order",
        width: 140,
        renderCell: (params) => <MonospaceIdCell id={params.value} />,
      },
      {
        field: "status",
        headerName: "Status",
        width: 160,
        renderCell: (params) => <OrderStatusBadge status={params.value} />,
      },
      {
        field: "subjectId",
        headerName: "Subject",
        width: 200,
        renderCell: (params) => <SubjectNameCell subjectId={params.value} />,
      },
      {
        field: "customerId",
        headerName: "Customer",
        width: 200,
        renderCell: (params) => <CustomerNameCell customerId={params.value} />,
      },
      {
        field: "lastUpdatedAt",
        headerName: "Last Updated",
        width: 180,
        renderCell: (params) => <RelativeTimeCell timestamp={params.value} />,
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

  // Mobile card list view
  const mobileOrderList = (
    <Stack spacing={2}>
      {isLoading && (
        <>
          {[1, 2, 3].map((i) => (
            <Skeleton key={i} variant="rounded" height={120} />
          ))}
        </>
      )}
      {!isLoading && ordersData?.items.length === 0 && (
        <Box sx={{ py: 4, textAlign: "center", color: "text.secondary" }}>
          No orders found
        </Box>
      )}
      {!isLoading &&
        ordersData?.items.map((order) => (
          <OrderCard
            key={order.orderId}
            order={order}
            onClick={handleOrderClick}
          />
        ))}
      {ordersData && ordersData.totalPages > 1 && (
        <Box sx={{ display: "flex", justifyContent: "center", pt: 2 }}>
          <Pagination
            count={ordersData.totalPages}
            page={paginationModel.page + 1}
            onChange={handleMobilePageChange}
            color="primary"
          />
        </Box>
      )}
    </Stack>
  );

  // Desktop grid view
  const desktopOrderGrid = (
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
  );

  const ordersContent = isMobile ? mobileOrderList : desktopOrderGrid;

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
        <FormControl size="small" sx={{ minWidth: 200 }}>
          <InputLabel id="status-filter-label">Status</InputLabel>
          <Select
            labelId="status-filter-label"
            value={statusFilter}
            label="Status"
            onChange={handleStatusFilterChange}
          >
            <MenuItem value="open">Open Orders</MenuItem>
            <MenuItem value="closed">Closed</MenuItem>
            <MenuItem value="all">All</MenuItem>
            <ListSubheader>By Status</ListSubheader>
            {orderStatuses.map((status) => (
              <MenuItem key={status} value={status}>
                <Chip
                  label={getOrderStatusLabel(status)}
                  color={getOrderStatusColor(status)}
                  size="small"
                  variant="outlined"
                  sx={{ pointerEvents: "none" }}
                />
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {/* Orders - Grid on desktop, Cards on mobile */}
        {ordersContent}
      </Stack>

      <NewOrderDialog
        open={newOrderOpen}
        onClose={() => setNewOrderOpen(false)}
      />
    </>
  );
};

export default OrdersPage;
