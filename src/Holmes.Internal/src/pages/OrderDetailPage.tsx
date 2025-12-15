import React, { useState } from "react";

import AccessTimeIcon from "@mui/icons-material/AccessTime";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import BuildIcon from "@mui/icons-material/Build";
import EmailIcon from "@mui/icons-material/Email";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import HistoryIcon from "@mui/icons-material/History";
import InfoIcon from "@mui/icons-material/Info";
import NotificationsIcon from "@mui/icons-material/Notifications";
import PauseIcon from "@mui/icons-material/Pause";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import RefreshIcon from "@mui/icons-material/Refresh";
import SmsIcon from "@mui/icons-material/Sms";
import TimelineIcon from "@mui/icons-material/Timeline";
import WebhookIcon from "@mui/icons-material/Webhook";
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
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Skeleton,
  Stack,
  Tab,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { format, formatDistanceToNow } from "date-fns";
import { useNavigate, useParams } from "react-router-dom";

import type {
  NotificationSummaryDto,
  OrderAuditEventDto,
  OrderTimelineEntryDto,
  SlaClockDto,
} from "@/types/api";

import SlaBadge, {
  clockStateToSlaStatus,
} from "@/components/patterns/SlaBadge";
import { TierProgressView } from "@/components/services";
import {
  useCustomer,
  useIsAdmin,
  useOrder,
  useOrderEvents,
  useOrderNotifications,
  useOrderServices,
  useOrderSlaClocks,
  useOrderTimeline,
  usePauseSlaClock,
  useResumeSlaClock,
  useRetryNotification,
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
  // Extract a human-readable event name from the assembly-qualified type name
  // Format: "Namespace.ClassName, AssemblyName, Version=x.x.x.x, Culture=neutral, PublicKeyToken=xxx"
  // First split by ", " to isolate the type name, then split by "." to get the class name
  const typeName = event.eventName.split(", ")[0];
  const displayName = typeName.split(".").pop() ?? event.eventName;

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
// SLA Clocks Panel Component
// ============================================================================

const getClockKindLabel = (kind: SlaClockDto["kind"]) => {
  switch (kind) {
    case "Intake":
      return "Intake";
    case "Fulfillment":
      return "Fulfillment";
    case "Overall":
      return "Overall";
    case "Custom":
      return "Custom";
    default:
      return kind;
  }
};

interface SlaClockCardProps {
  clock: SlaClockDto;
  canPauseResume: boolean;
  onPause: (clockId: string) => void;
  onResume: (clockId: string) => void;
  isPausing: boolean;
  isResuming: boolean;
}

const SlaClockCard = ({
  clock,
  canPauseResume,
  onPause,
  onResume,
  isPausing,
  isResuming,
}: SlaClockCardProps) => {
  const status = clockStateToSlaStatus(clock.state);
  const isPaused = clock.state === "Paused";
  const isTerminal = clock.state === "Completed" || clock.state === "Breached";
  const canTakeAction = canPauseResume && !isTerminal;

  return (
    <Card variant="outlined" sx={{ mb: 2 }}>
      <CardContent>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="flex-start"
        >
          <Box>
            <Stack
              direction="row"
              spacing={1}
              alignItems="center"
              sx={{ mb: 1 }}
            >
              <Typography variant="subtitle1" sx={{ fontWeight: 500 }}>
                {getClockKindLabel(clock.kind)}
              </Typography>
              <SlaBadge status={status} />
            </Stack>
            <Box
              sx={{
                display: "grid",
                gridTemplateColumns: "100px 1fr",
                gap: 0.5,
                fontSize: "0.875rem",
              }}
            >
              <Typography variant="body2" color="text.secondary">
                Started
              </Typography>
              <Typography variant="body2">
                {format(new Date(clock.startedAt), "MMM d, yyyy h:mm a")}
              </Typography>

              <Typography variant="body2" color="text.secondary">
                Deadline
              </Typography>
              <Typography variant="body2">
                {format(new Date(clock.deadlineAt), "MMM d, yyyy h:mm a")}
                {" · "}
                {formatDistanceToNow(new Date(clock.deadlineAt), {
                  addSuffix: true,
                })}
              </Typography>

              <Typography variant="body2" color="text.secondary">
                Target
              </Typography>
              {(() => {
                const dayLabel =
                  clock.targetBusinessDays === 1 ? "day" : "days";
                return (
                  <Typography variant="body2">
                    {clock.targetBusinessDays} business {dayLabel}
                  </Typography>
                );
              })()}

              {(() => {
                const pauseReasonDisplay = clock.pauseReason ? (
                  <>
                    <Typography variant="body2" color="text.secondary">
                      Pause Reason
                    </Typography>
                    <Typography variant="body2">{clock.pauseReason}</Typography>
                  </>
                ) : null;
                return pauseReasonDisplay;
              })()}

              {(() => {
                const completedAtDisplay = clock.completedAt ? (
                  <>
                    <Typography variant="body2" color="text.secondary">
                      Completed
                    </Typography>
                    <Typography variant="body2">
                      {format(
                        new Date(clock.completedAt),
                        "MMM d, yyyy h:mm a",
                      )}
                    </Typography>
                  </>
                ) : null;
                return completedAtDisplay;
              })()}
            </Box>
          </Box>

          {(() => {
            const resumeIcon = isResuming ? (
              <CircularProgress size={20} />
            ) : (
              <PlayArrowIcon />
            );
            const pauseIcon = isPausing ? (
              <CircularProgress size={20} />
            ) : (
              <PauseIcon />
            );
            const actionButton = isPaused ? (
              <Tooltip title="Resume clock">
                <IconButton
                  size="small"
                  onClick={() => onResume(clock.id)}
                  disabled={isResuming}
                  color="primary"
                >
                  {resumeIcon}
                </IconButton>
              </Tooltip>
            ) : (
              <Tooltip title="Pause clock">
                <IconButton
                  size="small"
                  onClick={() => onPause(clock.id)}
                  disabled={isPausing}
                  color="warning"
                >
                  {pauseIcon}
                </IconButton>
              </Tooltip>
            );
            const actionDisplay = canTakeAction ? (
              <Box>{actionButton}</Box>
            ) : null;
            return actionDisplay;
          })()}
        </Stack>
      </CardContent>
    </Card>
  );
};

interface SlaClocksTabProps {
  orderId: string;
}

const SlaClocksTab = ({ orderId }: SlaClocksTabProps) => {
  const isAdmin = useIsAdmin();
  const { data: clocks, isLoading, error } = useOrderSlaClocks(orderId);
  const pauseMutation = usePauseSlaClock();
  const resumeMutation = useResumeSlaClock();

  const [pauseDialogOpen, setPauseDialogOpen] = useState(false);
  const [pauseClockId, setPauseClockId] = useState<string | null>(null);
  const [pauseReason, setPauseReason] = useState("");

  const handlePauseClick = (clockId: string) => {
    setPauseClockId(clockId);
    setPauseReason("");
    setPauseDialogOpen(true);
  };

  const handlePauseConfirm = () => {
    if (pauseClockId && pauseReason.trim()) {
      pauseMutation.mutate(
        { clockId: pauseClockId, payload: { reason: pauseReason.trim() } },
        {
          onSuccess: () => {
            setPauseDialogOpen(false);
            setPauseClockId(null);
            setPauseReason("");
          },
        },
      );
    }
  };

  const handleResume = (clockId: string) => {
    resumeMutation.mutate(clockId);
  };

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
        Failed to load SLA clocks. Please try again.
      </Alert>
    );
  }

  if (!clocks || clocks.length === 0) {
    return (
      <Card variant="outlined">
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>
            SLA Clocks
          </Typography>
          <Typography color="text.secondary">
            No SLA clocks have been started for this order yet.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <>
      <Card variant="outlined">
        <CardContent>
          <Stack
            direction="row"
            justifyContent="space-between"
            alignItems="center"
            sx={{ mb: 2 }}
          >
            <Typography variant="h6">SLA Clocks</Typography>
            <Chip
              label={`${clocks.length} clock${clocks.length === 1 ? "" : "s"}`}
              size="small"
              color="default"
            />
          </Stack>
          {clocks.map((clock) => (
            <SlaClockCard
              key={clock.id}
              clock={clock}
              canPauseResume={isAdmin}
              onPause={handlePauseClick}
              onResume={handleResume}
              isPausing={pauseMutation.isPending && pauseClockId === clock.id}
              isResuming={resumeMutation.isPending}
            />
          ))}
        </CardContent>
      </Card>

      {/* Pause Dialog */}
      <Dialog open={pauseDialogOpen} onClose={() => setPauseDialogOpen(false)}>
        <DialogTitle>Pause SLA Clock</DialogTitle>
        <DialogContent>
          <Typography variant="body2" sx={{ mb: 2 }}>
            Please provide a reason for pausing this clock. Time will not count
            while the clock is paused.
          </Typography>
          <TextField
            fullWidth
            label="Reason"
            value={pauseReason}
            onChange={(e) => setPauseReason(e.target.value)}
            placeholder="e.g., Waiting for candidate response"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPauseDialogOpen(false)}>Cancel</Button>
          {(() => {
            const buttonLabel = pauseMutation.isPending
              ? "Pausing…"
              : "Pause Clock";
            return (
              <Button
                onClick={handlePauseConfirm}
                variant="contained"
                disabled={!pauseReason.trim() || pauseMutation.isPending}
              >
                {buttonLabel}
              </Button>
            );
          })()}
        </DialogActions>
      </Dialog>
    </>
  );
};

// ============================================================================
// Notifications Tab Component
// ============================================================================

const getChannelIcon = (channel: NotificationSummaryDto["channel"]) => {
  switch (channel) {
    case "Email":
      return <EmailIcon fontSize="small" />;
    case "Sms":
      return <SmsIcon fontSize="small" />;
    case "Webhook":
      return <WebhookIcon fontSize="small" />;
    default:
      return <NotificationsIcon fontSize="small" />;
  }
};

const getDeliveryStatusColor = (
  status: NotificationSummaryDto["status"],
): "success" | "warning" | "error" | "default" | "info" => {
  switch (status) {
    case "Delivered":
      return "success";
    case "Pending":
    case "Queued":
    case "Sending":
      return "info";
    case "Failed":
    case "Bounced":
      return "error";
    case "Cancelled":
      return "default";
    default:
      return "default";
  }
};

const getTriggerTypeLabel = (
  triggerType: NotificationSummaryDto["triggerType"],
) => {
  switch (triggerType) {
    case "IntakeSessionInvited":
      return "Intake Invited";
    case "IntakeSubmissionReceived":
      return "Intake Submitted";
    case "ConsentCaptured":
      return "Consent Captured";
    case "OrderStateChanged":
      return "Order Status Changed";
    case "SlaClockAtRisk":
      return "SLA At Risk";
    case "SlaClockBreached":
      return "SLA Breached";
    case "NotificationFailed":
      return "Notification Failed";
    default:
      return triggerType;
  }
};

interface NotificationCardProps {
  notification: NotificationSummaryDto;
  canRetry: boolean;
  onRetry: (notificationId: string) => void;
  isRetrying: boolean;
}

const NotificationCard = ({
  notification,
  canRetry,
  onRetry,
  isRetrying,
}: NotificationCardProps) => {
  const canRetryThis =
    canRetry &&
    (notification.status === "Failed" || notification.status === "Bounced");

  return (
    <Box
      sx={{
        display: "flex",
        gap: 2,
        py: 2,
        borderBottom: "1px solid",
        borderColor: "divider",
        "&:last-child": { borderBottom: "none" },
      }}
    >
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          width: 40,
          height: 40,
          borderRadius: 1,
          bgcolor: "grey.100",
          color: "grey.600",
        }}
      >
        {getChannelIcon(notification.channel)}
      </Box>
      <Box sx={{ flexGrow: 1 }}>
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
          <Typography variant="body2" sx={{ fontWeight: 500 }}>
            {getTriggerTypeLabel(notification.triggerType)}
          </Typography>
          <Chip
            label={notification.status}
            size="small"
            color={getDeliveryStatusColor(notification.status)}
            variant="outlined"
          />
          {notification.isAdverseAction && (
            <Chip label="Adverse Action" size="small" color="warning" />
          )}
        </Stack>
        <Typography variant="body2" color="text.secondary">
          {notification.channel} to {notification.recipientAddress}
        </Typography>
        <Stack direction="row" spacing={2} sx={{ mt: 0.5 }}>
          <Typography variant="caption" color="text.secondary">
            Created{" "}
            {format(new Date(notification.createdAt), "MMM d, yyyy h:mm a")}
          </Typography>
          {notification.deliveredAt && (
            <Typography variant="caption" color="text.secondary">
              Delivered{" "}
              {format(new Date(notification.deliveredAt), "MMM d, yyyy h:mm a")}
            </Typography>
          )}
          {notification.deliveryAttemptCount > 1 && (
            <Typography variant="caption" color="text.secondary">
              {notification.deliveryAttemptCount} attempts
            </Typography>
          )}
        </Stack>
      </Box>
      {(() => {
        const retryIcon = isRetrying ? (
          <CircularProgress size={20} />
        ) : (
          <RefreshIcon />
        );
        const retryButton = canRetryThis ? (
          <Box sx={{ display: "flex", alignItems: "center" }}>
            <Tooltip title="Retry notification">
              <IconButton
                size="small"
                onClick={() => onRetry(notification.id)}
                disabled={isRetrying}
                color="primary"
              >
                {retryIcon}
              </IconButton>
            </Tooltip>
          </Box>
        ) : null;
        return retryButton;
      })()}
    </Box>
  );
};

interface NotificationsTabProps {
  orderId: string;
}

const NotificationsTab = ({ orderId }: NotificationsTabProps) => {
  const isAdmin = useIsAdmin();
  const {
    data: notifications,
    isLoading,
    error,
  } = useOrderNotifications(orderId);
  const retryMutation = useRetryNotification();
  const [retryingId, setRetryingId] = useState<string | null>(null);

  const handleRetry = (notificationId: string) => {
    setRetryingId(notificationId);
    retryMutation.mutate(notificationId, {
      onSettled: () => setRetryingId(null),
    });
  };

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
        Failed to load notifications. Please try again.
      </Alert>
    );
  }

  if (!notifications || notifications.length === 0) {
    return (
      <Card variant="outlined">
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Notifications
          </Typography>
          <Typography color="text.secondary">
            No notifications have been sent for this order yet.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  const deliveredCount = notifications.filter(
    (n) => n.status === "Delivered",
  ).length;
  const failedCount = notifications.filter(
    (n) => n.status === "Failed" || n.status === "Bounced",
  ).length;

  return (
    <Card variant="outlined">
      <CardContent>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          sx={{ mb: 2 }}
        >
          <Typography variant="h6">Notifications</Typography>
          <Stack direction="row" spacing={1}>
            <Chip
              label={`${notifications.length} total`}
              size="small"
              color="default"
            />
            {deliveredCount > 0 && (
              <Chip
                label={`${deliveredCount} delivered`}
                size="small"
                color="success"
                variant="outlined"
              />
            )}
            {failedCount > 0 && (
              <Chip
                label={`${failedCount} failed`}
                size="small"
                color="error"
                variant="outlined"
              />
            )}
          </Stack>
        </Stack>
        <Box>
          {notifications.map((notification) => (
            <NotificationCard
              key={notification.id}
              notification={notification}
              canRetry={isAdmin}
              onRetry={handleRetry}
              isRetrying={
                retryMutation.isPending && retryingId === notification.id
              }
            />
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
          <Tab
            icon={<AccessTimeIcon />}
            iconPosition="start"
            label="SLA Clocks"
          />
          <Tab
            icon={<NotificationsIcon />}
            iconPosition="start"
            label="Notifications"
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
        <SlaClocksTab orderId={orderId!} />
      </TabPanel>

      <TabPanel value={activeTab} index={3}>
        <NotificationsTab orderId={orderId!} />
      </TabPanel>

      <TabPanel value={activeTab} index={4}>
        <TimelineTab orderId={orderId!} />
      </TabPanel>

      <TabPanel value={activeTab} index={5}>
        <AuditLogTab orderId={orderId!} />
      </TabPanel>
    </Box>
  );
};

export default OrderDetailPage;
