import React, { ReactElement } from "react";

import AccessTimeIcon from "@mui/icons-material/AccessTime";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { Chip, ChipProps } from "@mui/material";

export type SlaStatus = "on_track" | "at_risk" | "breached";

const statusConfig: Record<
  SlaStatus,
  { label: string; color: ChipProps["color"]; icon?: ReactElement }
> = {
  on_track: {
    label: "On Track",
    color: "success",
    icon: <AccessTimeIcon fontSize="small" />,
  },
  at_risk: {
    label: "At Risk",
    color: "warning",
    icon: <WarningAmberIcon fontSize="small" />,
  },
  breached: {
    label: "Breached",
    color: "error",
    icon: <WarningAmberIcon fontSize="small" />,
  },
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
