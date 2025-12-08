import React from "react";

import AutorenewIcon from "@mui/icons-material/Autorenew";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import PlayCircleIcon from "@mui/icons-material/PlayCircle";
import CancelIcon from "@mui/icons-material/Cancel";
import {
  Box,
  Card,
  CardContent,
  Chip,
  LinearProgress,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import { formatDistanceToNow } from "date-fns";

import type { ServiceCategory, ServiceRequestSummaryDto, ServiceStatus } from "@/types/api";

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

const categoryColors: Record<ServiceCategory, "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"> = {
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
  service: ServiceRequestSummaryDto;
  showCategory?: boolean;
}

const ServiceStatusCard = ({
  service,
  showCategory = true,
}: ServiceStatusCardProps) => {
  const isInProgress = service.status === "InProgress" || service.status === "Dispatched";
  const hasError = service.status === "Failed";
  const hasRetries = service.attemptCount > 1;

  // Get the most relevant timestamp
  const getRelevantTimestamp = () => {
    if (service.completedAt) return { label: "Completed", time: service.completedAt };
    if (service.failedAt) return { label: "Failed", time: service.failedAt };
    if (service.dispatchedAt) return { label: "Dispatched", time: service.dispatchedAt };
    return { label: "Created", time: service.createdAt };
  };

  const timestampInfo = getRelevantTimestamp();

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
              {service.vendorCode && (
                <Typography variant="caption" color="text.secondary">
                  Vendor: {service.vendorCode}
                </Typography>
              )}
              {service.scopeValue && (
                <Tooltip title={service.scopeType ?? "Scope"}>
                  <Typography variant="caption" color="text.secondary">
                    {service.scopeValue}
                  </Typography>
                </Tooltip>
              )}
            </Stack>
            <Stack direction="row" spacing={1} alignItems="center">
              {hasRetries && (
                <Tooltip title={`Attempt ${service.attemptCount} of ${service.maxAttempts}`}>
                  <Chip
                    label={`${service.attemptCount}/${service.maxAttempts}`}
                    size="small"
                    color={hasError ? "error" : "default"}
                    variant="outlined"
                  />
                </Tooltip>
              )}
              <Typography variant="caption" color="text.secondary">
                {timestampInfo.label}{" "}
                {formatDistanceToNow(new Date(timestampInfo.time), {
                  addSuffix: true,
                })}
              </Typography>
            </Stack>
          </Box>

          {/* Error message */}
          {hasError && service.lastError && (
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
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};

export default ServiceStatusCard;
