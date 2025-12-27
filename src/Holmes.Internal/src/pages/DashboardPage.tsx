import React, { useCallback, useEffect, useState } from "react";

import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Divider,
  Skeleton,
  Stack,
  Typography,
} from "@mui/material";
import { formatDistanceToNow } from "date-fns";
import { useNavigate } from "react-router-dom";

import type { OrderChangeEvent, OrderStatus } from "@/types/api";

import { PageHeader } from "@/components/layout";
import NewOrderDialog from "@/components/orders/NewOrderDialog";
import {
  createOrderChangesStream,
  useCurrentUser,
  useCustomers,
  useIsAdmin,
  useOrderStats,
  useUsers,
} from "@/hooks/api";
import { getOrderStatusColor, getOrderStatusLabel } from "@/lib/status";

// ============================================================================
// Pipeline Component
// ============================================================================

interface PipelineStage {
  status: OrderStatus;
  label: string;
  count: number;
}

interface OrderPipelineProps {
  stages: PipelineStage[];
  isLoading: boolean;
  onStageClick: (status: OrderStatus) => void;
}

const OrderPipeline = ({
  stages,
  isLoading,
  onStageClick,
}: OrderPipelineProps) => {
  if (isLoading) {
    return (
      <Stack direction="row" spacing={1} alignItems="center">
        {[1, 2, 3, 4].map((i) => (
          <React.Fragment key={i}>
            <Skeleton variant="rounded" width={120} height={60} />
            {i < 4 && (
              <Typography color="text.secondary" sx={{ px: 1 }}>
                →
              </Typography>
            )}
          </React.Fragment>
        ))}
      </Stack>
    );
  }

  return (
    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
      {stages.map((stage, index) => (
        <React.Fragment key={stage.status}>
          <Card
            variant="outlined"
            sx={{
              cursor: "pointer",
              transition: "all 0.2s",
              "&:hover": {
                borderColor: "primary.main",
                bgcolor: "action.hover",
              },
            }}
            onClick={() => onStageClick(stage.status)}
          >
            <CardContent sx={{ py: 1.5, px: 2, "&:last-child": { pb: 1.5 } }}>
              <Typography
                variant="h4"
                component="div"
                sx={{ fontWeight: 600, textAlign: "center" }}
              >
                {stage.count}
              </Typography>
              <Typography
                variant="caption"
                color="text.secondary"
                sx={{ textAlign: "center", display: "block" }}
              >
                {stage.label}
              </Typography>
            </CardContent>
          </Card>
          {index < stages.length - 1 && (
            <Typography color="text.secondary" sx={{ px: 0.5 }}>
              →
            </Typography>
          )}
        </React.Fragment>
      ))}
    </Stack>
  );
};

// ============================================================================
// Recent Activity Component
// ============================================================================

interface ActivityItem {
  orderId: string;
  status: string;
  reason?: string;
  changedAt: string;
}

interface RecentActivityProps {
  items: ActivityItem[];
  isLoading: boolean;
  onViewAll: () => void;
  onOrderClick: (orderId: string) => void;
}

const RecentActivity = ({
  items,
  isLoading,
  onViewAll,
  onOrderClick,
}: RecentActivityProps) => {
  if (isLoading) {
    return (
      <Stack spacing={2}>
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} variant="rounded" height={48} />
        ))}
      </Stack>
    );
  }

  if (items.length === 0) {
    return (
      <Typography color="text.secondary" sx={{ py: 2 }}>
        No recent activity. Orders will appear here as they progress.
      </Typography>
    );
  }

  return (
    <Stack spacing={1}>
      {items.map((item) => (
        <Box
          key={`${item.orderId}-${item.changedAt}`}
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            py: 1,
            px: 1.5,
            borderRadius: 1,
            cursor: "pointer",
            "&:hover": { bgcolor: "action.hover" },
          }}
          onClick={() => onOrderClick(item.orderId)}
        >
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Chip
              label={getOrderStatusLabel(item.status)}
              size="small"
              color={getOrderStatusColor(item.status)}
              variant="outlined"
            />
            <Typography variant="body2" color="text.secondary">
              Order {item.orderId.slice(0, 8)}…
            </Typography>
          </Stack>
          <Typography variant="caption" color="text.secondary">
            {formatDistanceToNow(new Date(item.changedAt), { addSuffix: true })}
          </Typography>
        </Box>
      ))}
      <Box sx={{ pt: 1 }}>
        <Button size="small" onClick={onViewAll}>
          View all orders →
        </Button>
      </Box>
    </Stack>
  );
};

// ============================================================================
// Admin Cards Component
// ============================================================================

interface AdminCardsProps {
  customerCount: number;
  userCount: number;
  isLoading: boolean;
  onCustomersClick: () => void;
  onUsersClick: () => void;
}

const AdminCards = ({
  customerCount,
  userCount,
  isLoading,
  onCustomersClick,
  onUsersClick,
}: AdminCardsProps) => {
  if (isLoading) {
    return (
      <Stack direction="row" spacing={2}>
        <Skeleton variant="rounded" width={200} height={80} />
        <Skeleton variant="rounded" width={200} height={80} />
      </Stack>
    );
  }

  return (
    <Stack direction="row" spacing={2}>
      <Card
        variant="outlined"
        sx={{
          minWidth: 180,
          cursor: "pointer",
          "&:hover": { borderColor: "primary.main" },
        }}
        onClick={onCustomersClick}
      >
        <CardContent sx={{ py: 1.5, "&:last-child": { pb: 1.5 } }}>
          <Typography variant="overline" color="text.secondary">
            Customers
          </Typography>
          {customerCount === 0 && (
            <Typography variant="body2" color="primary">
              Set up your first customer →
            </Typography>
          )}
          {customerCount > 0 && (
            <Typography variant="h5">{customerCount} active</Typography>
          )}
        </CardContent>
      </Card>

      <Card
        variant="outlined"
        sx={{
          minWidth: 180,
          cursor: "pointer",
          "&:hover": { borderColor: "primary.main" },
        }}
        onClick={onUsersClick}
      >
        <CardContent sx={{ py: 1.5, "&:last-child": { pb: 1.5 } }}>
          <Typography variant="overline" color="text.secondary">
            Users
          </Typography>
          <Typography variant="h5">{userCount} active</Typography>
        </CardContent>
      </Card>
    </Stack>
  );
};

// ============================================================================
// Dashboard Page
// ============================================================================

const DashboardPage = () => {
  const navigate = useNavigate();
  const isAdmin = useIsAdmin();
  const { data: currentUser } = useCurrentUser();

  const [newOrderOpen, setNewOrderOpen] = useState(false);
  const [recentActivity, setRecentActivity] = useState<ActivityItem[]>([]);
  const [sseError, setSseError] = useState<string | null>(null);

  // Data fetching
  const {
    data: orderStats,
    isLoading: statsLoading,
    error: statsError,
  } = useOrderStats();

  const { data: customersData, isLoading: customersLoading } = useCustomers(
    1,
    1,
  );
  const { data: usersData, isLoading: usersLoading } = useUsers(1, 1);

  // SSE for real-time activity
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
        setRecentActivity((prev) => {
          const updated = [
            {
              orderId: payload.orderId,
              status: payload.status,
              reason: payload.reason,
              changedAt: payload.changedAt,
            },
            ...prev,
          ].slice(0, 10);
          return updated;
        });
      } catch {
        // Ignore parse errors
      }
    };

    eventSource.addEventListener("order.change", handleOrderChange);

    eventSource.onerror = () => {
      // Only show error if we were previously connected
      // This prevents showing errors on initial connection failures
      if (hasConnected) {
        setSseError("Live updates disconnected. Refresh to reconnect.");
      }
    };

    return () => {
      eventSource.close();
    };
  }, []);

  // Handlers
  const handleStageClick = useCallback(
    (status: OrderStatus) => {
      navigate(`/orders?status=${status}`);
    },
    [navigate],
  );

  const handleViewAllOrders = useCallback(() => {
    navigate("/orders");
  }, [navigate]);

  const handleOrderClick = useCallback(
    (orderId: string) => {
      navigate(`/orders/${orderId}`);
    },
    [navigate],
  );

  const handleCustomersClick = useCallback(() => {
    navigate("/customers");
  }, [navigate]);

  const handleUsersClick = useCallback(() => {
    navigate("/users");
  }, [navigate]);

  // Pipeline stages
  const pipelineStages: PipelineStage[] = [
    {
      status: "Invited",
      label: getOrderStatusLabel("Invited"),
      count: orderStats?.invited ?? 0,
    },
    {
      status: "IntakeInProgress",
      label: getOrderStatusLabel("IntakeInProgress"),
      count: orderStats?.intakeInProgress ?? 0,
    },
    {
      status: "IntakeComplete",
      label: getOrderStatusLabel("IntakeComplete"),
      count: orderStats?.intakeComplete ?? 0,
    },
    {
      status: "ReadyForFulfillment",
      label: getOrderStatusLabel("ReadyForFulfillment"),
      count: orderStats?.readyForFulfillment ?? 0,
    },
  ];

  // Check if system needs setup (no customers)
  const needsSetup = isAdmin && customersData?.totalItems === 0;

  const welcomeSubtitle = currentUser
    ? `Welcome back, ${currentUser.displayName ?? currentUser.email}`
    : undefined;

  return (
    <>
      <PageHeader
        title="Dashboard"
        subtitle={welcomeSubtitle}
        action={
          <Button
            variant="contained"
            size="large"
            onClick={() => setNewOrderOpen(true)}
            disabled={needsSetup}
          >
            New Order
          </Button>
        }
      />

      {sseError && (
        <Alert severity="warning" onClose={() => setSseError(null)}>
          {sseError}
        </Alert>
      )}

      {statsError && (
        <Alert severity="error">
          Failed to load order statistics. Please refresh the page.
        </Alert>
      )}

      {needsSetup && (
        <Card variant="outlined">
          <CardContent sx={{ textAlign: "center", py: 6 }}>
            <Typography variant="h5" gutterBottom>
              Welcome to Holmes
            </Typography>
            <Typography color="text.secondary" sx={{ mb: 3 }}>
              Get started by setting up your first customer.
            </Typography>
            <Button
              variant="contained"
              size="large"
              onClick={handleCustomersClick}
            >
              Create Customer →
            </Button>
          </CardContent>
        </Card>
      )}
      {!needsSetup && (
        <Stack spacing={3}>
          {/* Order Pipeline */}
          <Card variant="outlined">
            <CardContent>
              <Box
                sx={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  mb: 2,
                }}
              >
                <Typography variant="h6">Order Pipeline</Typography>
              </Box>
              <OrderPipeline
                stages={pipelineStages}
                isLoading={statsLoading}
                onStageClick={handleStageClick}
              />
            </CardContent>
          </Card>

          {/* Recent Activity */}
          <Card variant="outlined">
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2 }}>
                Recent Activity
              </Typography>
              <RecentActivity
                items={recentActivity}
                isLoading={false}
                onViewAll={handleViewAllOrders}
                onOrderClick={handleOrderClick}
              />
            </CardContent>
          </Card>

          {/* Admin Cards */}
          {isAdmin && (
            <>
              <Divider />
              <Box>
                <Typography
                  variant="overline"
                  color="text.secondary"
                  sx={{ mb: 1, display: "block" }}
                >
                  Administration
                </Typography>
                <AdminCards
                  customerCount={customersData?.totalItems ?? 0}
                  userCount={usersData?.totalItems ?? 0}
                  isLoading={customersLoading || usersLoading}
                  onCustomersClick={handleCustomersClick}
                  onUsersClick={handleUsersClick}
                />
              </Box>
            </>
          )}
        </Stack>
      )}

      <NewOrderDialog
        open={newOrderOpen}
        onClose={() => setNewOrderOpen(false)}
      />
    </>
  );
};

export default DashboardPage;
