import React, { ReactElement } from "react";

import AccessTimeIcon from "@mui/icons-material/AccessTime";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import PauseCircleIcon from "@mui/icons-material/PauseCircle";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { Chip, ChipProps } from "@mui/material";

import type { ClockState } from "@/types/api";

export type SlaStatus =
  | "on_track"
  | "at_risk"
  | "breached"
  | "paused"
  | "completed";

const statusConfig: Record<
  SlaStatus,
  { label: string; color: ChipProps["color"]; icon?: ReactElement }
> = {
  on_track: {
    label: "On Track",
    color: "success",
    icon: <AccessTimeIcon fontSize="small" />
  },
  at_risk: {
    label: "At Risk",
    color: "warning",
    icon: <WarningAmberIcon fontSize="small" />
  },
  breached: {
    label: "Breached",
    color: "error",
    icon: <WarningAmberIcon fontSize="small" />
  },
  paused: {
    label: "Paused",
    color: "default",
    icon: <PauseCircleIcon fontSize="small" />
  },
  completed: {
    label: "Completed",
    color: "success",
    icon: <CheckCircleIcon fontSize="small" />
  }
};

/**
 * Maps backend ClockState to frontend SlaStatus for display.
 */
export const clockStateToSlaStatus = (state: ClockState): SlaStatus => {
  switch (state) {
    case "Running":
      return "on_track";
    case "AtRisk":
      return "at_risk";
    case "Breached":
      return "breached";
    case "Paused":
      return "paused";
    case "Completed":
      return "completed";
    default:
      return "on_track";
  }
};

interface SlaBadgeProps {
  status: SlaStatus;
  deadlineLabel?: string;
}

const SlaBadge = ({ status, deadlineLabel }: SlaBadgeProps) => {
  const config = statusConfig[status];
  const label = deadlineLabel
    ? `${config.label} · ${deadlineLabel}`
    : config.label;

  return (
    <Chip size="small" color={config.color} icon={config.icon} label={label} />
  );
};

export default SlaBadge;
