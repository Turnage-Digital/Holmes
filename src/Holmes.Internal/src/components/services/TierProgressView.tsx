import React, { useMemo } from "react";

import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Chip,
  LinearProgress,
  Stack,
  Typography
} from "@mui/material";

import ServiceStatusCard from "./ServiceStatusCard";

import type { ServiceRequestSummaryDto, ServiceStatus } from "@/types/api";

// ============================================================================
// Tier Summary Component
// ============================================================================

interface TierSummaryProps {
  tier: number;
  services: ServiceRequestSummaryDto[];
  defaultExpanded?: boolean;
}

const getTierStatus = (
  services: ServiceRequestSummaryDto[]
): {
  label: string;
  color: "success" | "warning" | "error" | "default";
  icon: React.ReactElement;
} => {
  if (services.length === 0) {
    return {
      label: "Empty",
      color: "default",
      icon: <HourglassEmptyIcon />
    };
  }

  const hasFailures = services.some((s) => s.status === "Failed");
  const allComplete = services.every(
    (s) => s.status === "Completed" || s.status === "Canceled"
  );
  const allPending = services.every((s) => s.status === "Pending");

  if (hasFailures) {
    return {
      label: "Has Failures",
      color: "error",
      icon: <ErrorOutlineIcon />
    };
  }

  if (allComplete) {
    return {
      label: "Complete",
      color: "success",
      icon: <CheckCircleOutlineIcon />
    };
  }

  if (allPending) {
    return {
      label: "Pending",
      color: "default",
      icon: <HourglassEmptyIcon />
    };
  }

  return {
    label: "In Progress",
    color: "warning",
    icon: <HourglassEmptyIcon />
  };
};

const TierAccordion = ({
                         tier,
                         services,
                         defaultExpanded = false
                       }: TierSummaryProps) => {
  const status = getTierStatus(services);

  // Calculate progress
  const completedCount = services.filter(
    (s) => s.status === "Completed"
  ).length;
  const progressPercent =
    services.length > 0 ? (completedCount / services.length) * 100 : 0;

  // Count by status
  const statusCounts = services.reduce(
    (acc, s) => {
      acc[s.status] = (acc[s.status] || 0) + 1;
      return acc;
    },
    {} as Record<ServiceStatus, number>
  );

  return (
    <Accordion defaultExpanded={defaultExpanded} variant="outlined">
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            width: "100%",
            gap: 2
          }}
        >
          <Chip
            label={`Tier ${tier}`}
            color="primary"
            size="small"
            sx={{ fontWeight: 600, minWidth: 70 }}
          />

          <Box sx={{ flexGrow: 1, mx: 2 }}>
            <LinearProgress
              variant="determinate"
              value={progressPercent}
              sx={{ height: 6, borderRadius: 3 }}
              color={status.color === "error" ? "error" : "primary"}
            />
          </Box>

          <Stack direction="row" spacing={1} alignItems="center">
            {statusCounts.Completed && (
              <Chip
                label={`${statusCounts.Completed} done`}
                size="small"
                color="success"
                variant="outlined"
              />
            )}
            {statusCounts.InProgress && (
              <Chip
                label={`${statusCounts.InProgress} running`}
                size="small"
                color="warning"
                variant="outlined"
              />
            )}
            {statusCounts.Failed && (
              <Chip
                label={`${statusCounts.Failed} failed`}
                size="small"
                color="error"
                variant="outlined"
              />
            )}
            {statusCounts.Pending && (
              <Chip
                label={`${statusCounts.Pending} pending`}
                size="small"
                variant="outlined"
              />
            )}
          </Stack>

          <Typography variant="body2" color="text.secondary" sx={{ ml: 1 }}>
            {completedCount}/{services.length}
          </Typography>
        </Box>
      </AccordionSummary>
      <AccordionDetails>
        <Stack spacing={1}>
          {services.map((service) => (
            <ServiceStatusCard
              key={service.id}
              service={service}
              showCategory
            />
          ))}
        </Stack>
      </AccordionDetails>
    </Accordion>
  );
};

// ============================================================================
// Tier Progress View Component
// ============================================================================

interface TierProgressViewProps {
  services: ServiceRequestSummaryDto[];
}

const TierProgressView = ({ services }: TierProgressViewProps) => {
  // Group services by tier
  const tierGroups = useMemo(() => {
    const groups = new Map<number, ServiceRequestSummaryDto[]>();

    services.forEach((service) => {
      const existing = groups.get(service.tier) ?? [];
      groups.set(service.tier, [...existing, service]);
    });

    // Sort by tier number
    return Array.from(groups.entries()).sort(([a], [b]) => a - b);
  }, [services]);

  // Calculate overall stats
  const totalServices = services.length;
  const completedServices = services.filter(
    (s) => s.status === "Completed"
  ).length;
  const failedServices = services.filter((s) => s.status === "Failed").length;
  const inProgressServices = services.filter(
    (s) => s.status === "InProgress" || s.status === "Dispatched"
  ).length;

  if (services.length === 0) {
    return (
      <Typography color="text.secondary" sx={{ py: 2, textAlign: "center" }}>
        No services have been created for this order yet.
      </Typography>
    );
  }

  return (
    <Stack spacing={3}>
      {/* Overall Progress Summary */}
      <Box
        sx={{
          display: "flex",
          gap: 3,
          p: 2,
          bgcolor: "grey.50",
          borderRadius: 1
        }}
      >
        <Box>
          <Typography variant="h4" fontWeight={600}>
            {completedServices}/{totalServices}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            Services Complete
          </Typography>
        </Box>
        <Box sx={{ flexGrow: 1, display: "flex", alignItems: "center" }}>
          <LinearProgress
            variant="determinate"
            value={(completedServices / totalServices) * 100}
            sx={{ height: 12, borderRadius: 6, width: "100%" }}
          />
        </Box>
        <Stack direction="row" spacing={2}>
          {inProgressServices > 0 && (
            <Box>
              <Typography variant="h5" color="warning.main" fontWeight={600}>
                {inProgressServices}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Running
              </Typography>
            </Box>
          )}
          {failedServices > 0 && (
            <Box>
              <Typography variant="h5" color="error.main" fontWeight={600}>
                {failedServices}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Failed
              </Typography>
            </Box>
          )}
        </Stack>
      </Box>

      {/* Tier Accordions */}
      {tierGroups.map(([tier, tierServices], index) => (
        <TierAccordion
          key={tier}
          tier={tier}
          services={tierServices}
          defaultExpanded={index === 0}
        />
      ))}
    </Stack>
  );
};

export default TierProgressView;
