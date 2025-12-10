import React, { useState } from "react";

import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import BuildIcon from "@mui/icons-material/Build";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import HistoryIcon from "@mui/icons-material/History";
import InfoIcon from "@mui/icons-material/Info";
import TimelineIcon from "@mui/icons-material/Timeline";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  IconButton,
  Skeleton,
  Stack,
  Tab,
  Tabs,
  Tooltip,
  Typography,
} from "@mui/material";
import { format, formatDistanceToNow } from "date-fns";
import { useNavigate, useParams } from "react-router-dom";

import type { OrderAuditEventDto, OrderTimelineEntryDto } from "@/types/api";

import { TierProgressView } from "@/components/services";
import {
  useCustomer,
  useOrder,
  useOrderEvents,
  useOrderServices,
  useOrderTimeline,
  useSubject,
} from "@/hooks/api";
import { getOrderStatusColor, getOrderStatusLabel } from "@/lib/status";

// ============================================================================
// Tab Panel Component
// ============================================================================

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel = ({ children, value, index }: TabPanelProps) => (
  <div role="tabpanel" hidden={value !== index}>
    {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
  </div>
);

// ============================================================================
// Timeline Component
// ============================================================================

interface TimelineProps {
  events: OrderTimelineEntryDto[];
  isLoading: boolean;
}

const Timeline = ({ events, isLoading }: TimelineProps) => {
  if (isLoading) {
    return (
      <Stack spacing={2}>
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} variant="rounded" height={60} />
        ))}
      </Stack>
    );
  }

  if (events.length === 0) {
    return (
      <Typography color="text.secondary">
        No timeline events recorded yet.
      </Typography>
    );
  }

  return (
    <Stack spacing={0}>
      {events.map((event, index) => (
        <Box
          key={event.eventId}
          sx={{
            display: "flex",
            gap: 2,
            py: 2,
            borderBottom: index < events.length - 1 ? "1px solid" : "none",
            borderColor: "divider",
          }}
        >
          {/* Timeline dot and line */}
          <Box
            sx={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              pt: 0.5,
            }}
          >
            <Box
              sx={{
                width: 10,
                height: 10,
                borderRadius: "50%",
                bgcolor: index === 0 ? "primary.main" : "grey.400",
              }}
            />
            {index < events.length - 1 && (
              <Box
                sx={{
                  width: 2,
                  flexGrow: 1,
                  bgcolor: "grey.200",
                  mt: 0.5,
                }}
              />
            )}
          </Box>

          {/* Event content */}
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="body2" sx={{ fontWeight: 500 }}>
              {event.description || event.eventType}
            </Typography>
            {(() => {
              const sourceDisplay = event.source ? (
                <Typography variant="caption" color="text.secondary">
                  {event.source}
                </Typography>
              ) : null;
              return sourceDisplay;
            })()}
            <Typography
              variant="caption"
              color="text.secondary"
              display="block"
            >
              {format(new Date(event.occurredAt), "MMM d, yyyy 'at' h:mm a")}
              {" · "}
              {formatDistanceToNow(new Date(event.occurredAt), {
                addSuffix: true,
              })}
            </Typography>
          </Box>
        </Box>
      ))}
    </Stack>
  );
};

// ============================================================================
// Details Tab Content
// ============================================================================

interface DetailsTabProps {
  order: NonNullable<ReturnType<typeof useOrder>["data"]>;
}

const DetailsTab = ({ order }: DetailsTabProps) => (
  <Card variant="outlined">
    <CardContent>
      <Typography variant="h6" sx={{ mb: 2 }}>
        Order Details
      </Typography>
      <Stack spacing={2}>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: "140px 1fr",
            gap: 1,
          }}
        >
          <Typography variant="body2" color="text.secondary">
            Order ID
          </Typography>
          <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
            {order.orderId}
          </Typography>

          <Typography variant="body2" color="text.secondary">
            Subject ID
          </Typography>
          <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
            {order.subjectId}
          </Typography>

          <Typography variant="body2" color="text.secondary">
            Customer ID
          </Typography>
          <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
            {order.customerId}
          </Typography>

          <Typography variant="body2" color="text.secondary">
            Policy
          </Typography>
          <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
            {order.policySnapshotId}
          </Typography>

          {(() => {
            const packageCodeDisplay = order.packageCode ? (
              <>
                <Typography variant="body2" color="text.secondary">
                  Package
                </Typography>
                <Typography variant="body2">{order.packageCode}</Typography>
              </>
            ) : null;
            return packageCodeDisplay;
          })()}

          <Typography variant="body2" color="text.secondary">
            Last Updated
          </Typography>
          <Typography variant="body2">
            {format(new Date(order.lastUpdatedAt), "MMM d, yyyy 'at' h:mm a")}
          </Typography>

          {(() => {
            const readyForFulfillmentDisplay = order.readyForFulfillmentAt ? (
              <>
                <Typography variant="body2" color="text.secondary">
                  Ready for Fulfillment
                </Typography>
                <Typography variant="body2">
                  {format(
                    new Date(order.readyForFulfillmentAt),
                    "MMM d, yyyy 'at' h:mm a",
                  )}
                </Typography>
              </>
            ) : null;
            return readyForFulfillmentDisplay;
          })()}

          {(() => {
            const canceledAtDisplay = order.canceledAt ? (
              <>
                <Typography variant="body2" color="text.secondary">
                  Canceled
                </Typography>
                <Typography variant="body2">
                  {format(
                    new Date(order.canceledAt),
                    "MMM d, yyyy 'at' h:mm a",
                  )}
                </Typography>
              </>
            ) : null;
            return canceledAtDisplay;
          })()}

          {(() => {
            const closedAtDisplay = order.closedAt ? (
              <>
                <Typography variant="body2" color="text.secondary">
                  Closed
                </Typography>
                <Typography variant="body2">
                  {format(new Date(order.closedAt), "MMM d, yyyy 'at' h:mm a")}
                </Typography>
              </>
            ) : null;
            return closedAtDisplay;
          })()}
        </Box>
      </Stack>
    </CardContent>
  </Card>
);

// ============================================================================
// Services Tab Content
// ============================================================================

interface ServicesTabProps {
  orderId: string;
}

const ServicesTab = ({ orderId }: ServicesTabProps) => {
  const { data: orderServices, isLoading, error } = useOrderServices(orderId);

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error">Failed to load services. Please try again.</Alert>
    );
  }

  if (!orderServices) {
    return (
      <Alert severity="info">No service data available for this order.</Alert>
    );
  }

  return <TierProgressView services={orderServices.services} />;
};

// ============================================================================
// Timeline Tab Content
// ============================================================================

interface TimelineTabProps {
  orderId: string;
}

const TimelineTab = ({ orderId }: TimelineTabProps) => {
  const { data: timeline, isLoading } = useOrderTimeline(orderId);

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Activity Timeline
        </Typography>
        <Timeline events={timeline ?? []} isLoading={isLoading} />
      </CardContent>
    </Card>
  );
};

// ============================================================================
// Audit Log Tab Content
// ============================================================================

interface AuditLogTabProps {
  orderId: string;
}

const AuditEventCard = ({ event }: { event: OrderAuditEventDto }) => {
  // Extract a human-readable event name from the full type name
  const displayName = event.eventName.split(".").pop() ?? event.eventName;

  return (
    <Accordion
      disableGutters
      sx={{
        "&:before": { display: "none" },
        boxShadow: "none",
        borderBottom: "1px solid",
        borderColor: "divider",
      }}
    >
      <AccordionSummary expandIcon={<ExpandMoreIcon />} sx={{ px: 0 }}>
        <Box
          sx={{ display: "flex", alignItems: "center", gap: 2, width: "100%" }}
        >
          <Tooltip title={`Global position: ${event.position}`}>
            <Typography
              variant="caption"
              sx={{
                fontFamily: "monospace",
                bgcolor: "grey.100",
                px: 1,
                py: 0.5,
                borderRadius: 1,
                minWidth: 40,
                textAlign: "center",
              }}
            >
              v{event.version}
            </Typography>
          </Tooltip>
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="body2" sx={{ fontWeight: 500 }}>
              {displayName}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {format(new Date(event.createdAt), "MMM d, yyyy 'at' h:mm:ss a")}
              {" · "}
              {formatDistanceToNow(new Date(event.createdAt), {
                addSuffix: true,
              })}
            </Typography>
          </Box>
          {(() => {
            const actorIdDisplay = event.actorId ? (
              <Tooltip title="Actor ID">
                <Chip
                  label={`${event.actorId.slice(0, 12)}…`}
                  size="small"
                  variant="outlined"
                  sx={{ fontFamily: "monospace", fontSize: "0.7rem" }}
                />
              </Tooltip>
            ) : null;
            return actorIdDisplay;
          })()}
        </Box>
      </AccordionSummary>
      <AccordionDetails sx={{ px: 0, pt: 0 }}>
        <Box
          sx={{
            bgcolor: "grey.50",
            p: 2,
            borderRadius: 1,
            overflow: "auto",
          }}
        >
          <Typography
            component="pre"
            sx={{
              fontFamily: "monospace",
              fontSize: "0.75rem",
              m: 0,
              whiteSpace: "pre-wrap",
              wordBreak: "break-word",
            }}
          >
            {JSON.stringify(event.payload, null, 2)}
          </Typography>
        </Box>
        {(() => {
          const correlationIdDisplay = event.correlationId ? (
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ mt: 1, display: "block" }}
            >
              Correlation ID: {event.correlationId}
            </Typography>
          ) : null;
          return correlationIdDisplay;
        })()}
      </AccordionDetails>
    </Accordion>
  );
};

const AuditLogTab = ({ orderId }: AuditLogTabProps) => {
  const { data: events, isLoading, error } = useOrderEvents(orderId);

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="error">
        Failed to load audit events. Please try again.
      </Alert>
    );
  }

  if (!events || events.length === 0) {
    return (
      <Card variant="outlined">
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Audit Log
          </Typography>
          <Typography color="text.secondary">
            No events recorded for this order yet.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          sx={{ mb: 2 }}
        >
          <Typography variant="h6">Audit Log</Typography>
          <Chip
            label={`${events.length} event${events.length === 1 ? "" : "s"}`}
            size="small"
            color="default"
          />
        </Stack>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Raw domain events from the event store. Click to expand and view full
          payload.
        </Typography>
        <Box>
          {events.map((event) => (
            <AuditEventCard key={event.eventId} event={event} />
          ))}
        </Box>
      </CardContent>
    </Card>
  );
};

// ============================================================================
// Order Detail Page
// ============================================================================

const OrderDetailPage = () => {
  const navigate = useNavigate();
  const { orderId } = useParams<{ orderId: string }>();
  const [activeTab, setActiveTab] = useState(0);

  // Fetch order data
  const {
    data: order,
    isLoading: orderLoading,
    error: orderError,
  } = useOrder(orderId!);

  // Fetch related entities
  const { data: subject } = useSubject(order?.subjectId ?? "");
  const { data: customer } = useCustomer(order?.customerId ?? "");

  // Fetch services for badge count
  const { data: orderServices } = useOrderServices(orderId!);

  // Loading state
  if (orderLoading) {
    return (
      <Box
        sx={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          minHeight: 400,
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  // Error state
  if (orderError || !order) {
    const errorMessage = orderError
      ? "Failed to load order. Please try again."
      : "Order not found.";
    return (
      <Box>
        <Button
          startIcon={<ArrowBackIcon />}
          onClick={() => navigate("/orders")}
          sx={{ mb: 2 }}
        >
          Back to Orders
        </Button>
        <Alert severity="error">{errorMessage}</Alert>
      </Box>
    );
  }

  // Build subject display name
  const subjectName = subject
    ? [subject.firstName, subject.lastName].filter(Boolean).join(" ") ||
      subject.email
    : null;

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  // Service count badge
  const serviceCount = orderServices?.totalServices ?? 0;
  const completedCount = orderServices?.completedServices ?? 0;
  const servicesLabel =
    serviceCount > 0
      ? `Services (${completedCount}/${serviceCount})`
      : "Services";

  return (
    <Box>
      {/* Back button */}
      <IconButton
        onClick={() => navigate("/orders")}
        sx={{ mb: 2 }}
        size="small"
      >
        <ArrowBackIcon />
      </IconButton>

      {/* Header */}
      <Box sx={{ mb: 3 }}>
        <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 1 }}>
          <Typography variant="h4" component="h1">
            Order
          </Typography>
          <Typography
            variant="h4"
            component="span"
            sx={{ fontFamily: "monospace", opacity: 0.7 }}
          >
            {order.orderId.slice(0, 12)}…
          </Typography>
          <Chip
            label={getOrderStatusLabel(order.status)}
            color={getOrderStatusColor(order.status)}
            size="small"
            variant="outlined"
          />
        </Stack>

        <Stack direction="row" spacing={1} alignItems="center">
          {(() => {
            const subjectNameDisplay = subjectName ? (
              <Typography variant="body1">{subjectName}</Typography>
            ) : (
              <Typography variant="body1" color="text.secondary">
                Subject pending
              </Typography>
            );
            return subjectNameDisplay;
          })()}
          <Typography color="text.secondary">•</Typography>
          <Typography variant="body1" color="text.secondary">
            {customer?.name ?? "Loading customer…"}
          </Typography>
        </Stack>

        {(() => {
          const statusReasonDisplay = order.lastStatusReason ? (
            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
              {order.lastStatusReason}
            </Typography>
          ) : null;
          return statusReasonDisplay;
        })()}
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
        <Tabs value={activeTab} onChange={handleTabChange}>
          <Tab icon={<InfoIcon />} iconPosition="start" label="Details" />
          <Tab
            icon={<BuildIcon />}
            iconPosition="start"
            label={servicesLabel}
          />
          <Tab icon={<TimelineIcon />} iconPosition="start" label="Timeline" />
          <Tab icon={<HistoryIcon />} iconPosition="start" label="Audit Log" />
        </Tabs>
      </Box>

      {/* Tab Content */}
      <TabPanel value={activeTab} index={0}>
        <DetailsTab order={order} />
      </TabPanel>

      <TabPanel value={activeTab} index={1}>
        <ServicesTab orderId={orderId!} />
      </TabPanel>

      <TabPanel value={activeTab} index={2}>
        <TimelineTab orderId={orderId!} />
      </TabPanel>

      <TabPanel value={activeTab} index={3}>
        <AuditLogTab orderId={orderId!} />
      </TabPanel>
    </Box>
  );
};

export default OrderDetailPage;
