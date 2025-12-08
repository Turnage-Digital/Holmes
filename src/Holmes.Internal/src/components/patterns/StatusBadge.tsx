import React from "react";

import { Chip } from "@mui/material";

import { type EntityType, getStatusColor, getStatusLabel } from "@/lib/status";

interface StatusBadgeProps {
  /** The type of entity this status belongs to */
  type: EntityType;
  /** The status value to display */
  status: string;
  /** Visual variant of the chip */
  variant?: "filled" | "outlined";
  /** Size of the badge */
  size?: "small" | "medium";
}

/**
 * Displays a status badge with appropriate color and label based on entity type.
 * Consolidates status rendering across orders, users, and subjects.
 */
const StatusBadge = ({
                       type,
                       status,
                       variant = "outlined",
                       size = "small"
                     }: StatusBadgeProps) => (
  <Chip
    label={getStatusLabel(type, status)}
    size={size}
    color={getStatusColor(type, status)}
    variant={variant}
  />
);

export default StatusBadge;
