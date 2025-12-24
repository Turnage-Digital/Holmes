import React from "react";

import AutorenewIcon from "@mui/icons-material/Autorenew";
import CancelIcon from "@mui/icons-material/Cancel";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import PlayCircleIcon from "@mui/icons-material/PlayCircle";
import RefreshIcon from "@mui/icons-material/Refresh";
import {
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  IconButton,
  LinearProgress,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import { formatDistanceToNow } from "date-fns";

import type {
  ServiceCategory,
  ServiceSummaryDto,
  ServiceStatus,
} from "@/types/api";

import { StatusBadge } from "@/components/patterns";

// ============================================================================
// Status Icon Component
// ============================================================================

const statusIcons: Record<ServiceStatus, React.ReactElement> = {
  Pending: <HourglassEmptyIcon fontSize="small" />,
  Dispatched: <PlayCircleIcon fontSize="small" />,
  InProgress: <AutorenewIcon fontSize="small" />,
  Completed: <CheckCircleIcon fontSize="small" />,
  Failed: <ErrorIcon fontSize="small" />,
  Canceled: <CancelIcon fontSize="small" />,
};

const statusColors: Record<ServiceStatus, string> = {
  Pending: "grey.400",
  Dispatched: "info.main",
  InProgress: "warning.main",
  Completed: "success.main",
  Failed: "error.main",
  Canceled: "grey.500",
};

// ============================================================================
// Category Colors
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
// Service Status Card Component
// ============================================================================

interface ServiceStatusCardProps {
  service: ServiceSummaryDto;
  showCategory?: boolean;
  onRetry?: (serviceId: string) => void;
  onCancel?: (serviceId: string) => void;
  isRetrying?: boolean;
  isCanceling?: boolean;
}

const ServiceStatusCard = ({
  service,
  showCategory = true,
  onRetry,
  onCancel,
  isRetrying = false,
  isCanceling = false,
}: ServiceStatusCardProps) => {
  const isInProgress =
    service.status === "InProgress" || service.status === "Dispatched";
  const hasError = service.status === "Failed";
  const hasRetries = service.attemptCount > 1;

  // Determine if actions are available
  const canRetry =
    hasError && onRetry && service.attemptCount < service.maxAttempts;
  const canCancel =
    onCancel &&
    (service.status === "Pending" ||
      service.status === "Dispatched" ||
      service.status === "InProgress");

  // Get the most relevant timestamp
  const getRelevantTimestamp = () => {
    if (service.completedAt)
      return { label: "Completed", time: service.completedAt };
    if (service.failedAt) return { label: "Failed", time: service.failedAt };
    if (service.dispatchedAt)
      return { label: "Dispatched", time: service.dispatchedAt };
    return { label: "Created", time: service.createdAt };
  };

  const timestampInfo = getRelevantTimestamp();
  const chipColor = hasError ? "error" : "default";

  const vendorDisplay = service.vendorCode ? (
    <Typography variant="caption" color="text.secondary">
      Vendor: {service.vendorCode}
    </Typography>
  ) : null;

  const scopeDisplay = service.scopeValue ? (
    <Tooltip title={service.scopeType ?? "Scope"}>
      <Typography variant="caption" color="text.secondary">
        {service.scopeValue}
      </Typography>
    </Tooltip>
  ) : null;

  const retriesDisplay = hasRetries ? (
    <Tooltip
      title={`Attempt ${service.attemptCount} of ${service.maxAttempts}`}
    >
      <Chip
        label={`${service.attemptCount}/${service.maxAttempts}`}
        size="small"
        color={chipColor}
        variant="outlined"
      />
    </Tooltip>
  ) : null;

  const errorDisplay =
    hasError && service.lastError ? (
      <Typography
        variant="caption"
        color="error.main"
        sx={{
          mt: 0.5,
          p: 1,
          bgcolor: "error.50",
          borderRadius: 1,
          border: "1px solid",
          borderColor: "error.200",
        }}
      >
        {service.lastError}
      </Typography>
    ) : null;

  return (
    <Card
      variant="outlined"
      sx={{
        borderColor: hasError ? "error.light" : undefined,
        bgcolor: hasError ? "error.50" : undefined,
      }}
    >
      <CardContent sx={{ py: 1.5, px: 2, "&:last-child": { pb: 1.5 } }}>
        <Stack spacing={1}>
          {/* Header Row */}
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "flex-start",
            }}
          >
            <Stack spacing={0.5}>
              <Typography variant="subtitle2" fontWeight={600}>
                {service.serviceTypeCode}
              </Typography>
              {showCategory && (
                <Chip
                  label={service.category}
                  size="small"
                  color={categoryColors[service.category]}
                  variant="outlined"
                  sx={{ alignSelf: "flex-start" }}
                />
              )}
            </Stack>
            <Stack direction="row" spacing={1} alignItems="center">
              <StatusBadge type="service" status={service.status} />
              <Box sx={{ color: statusColors[service.status] }}>
                {statusIcons[service.status]}
              </Box>
              {(() => {
                const retryIcon = isRetrying ? (
                  <CircularProgress size={18} />
                ) : (
                  <RefreshIcon fontSize="small" />
                );
                const retryButton = canRetry ? (
                  <Tooltip title="Retry service">
                    <IconButton
                      size="small"
                      onClick={() => onRetry(service.id)}
                      disabled={isRetrying}
                      color="primary"
                      sx={{ ml: 0.5 }}
                    >
                      {retryIcon}
                    </IconButton>
                  </Tooltip>
                ) : null;
                return retryButton;
              })()}
              {(() => {
                const cancelIcon = isCanceling ? (
                  <CircularProgress size={18} />
                ) : (
                  <CancelOutlinedIcon fontSize="small" />
                );
                const cancelButton = canCancel ? (
                  <Tooltip title="Cancel service">
                    <IconButton
                      size="small"
                      onClick={() => onCancel(service.id)}
                      disabled={isCanceling}
                      color="error"
                      sx={{ ml: 0.5 }}
                    >
                      {cancelIcon}
                    </IconButton>
                  </Tooltip>
                ) : null;
                return cancelButton;
              })()}
            </Stack>
          </Box>

          {/* Progress indicator for in-progress services */}
          {isInProgress && (
            <LinearProgress
              variant="indeterminate"
              sx={{ height: 2, borderRadius: 1 }}
            />
          )}

          {/* Details Row */}
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
            }}
          >
            <Stack direction="row" spacing={2}>
              <Typography variant="caption" color="text.secondary">
                Tier {service.tier}
              </Typography>
              {vendorDisplay}
              {scopeDisplay}
            </Stack>
            <Stack direction="row" spacing={1} alignItems="center">
              {retriesDisplay}
              <Typography variant="caption" color="text.secondary">
                {timestampInfo.label}{" "}
                {formatDistanceToNow(new Date(timestampInfo.time), {
                  addSuffix: true,
                })}
              </Typography>
            </Stack>
          </Box>

          {/* Error message */}
          {errorDisplay}
        </Stack>
      </CardContent>
    </Card>
  );
};

export default ServiceStatusCard;
