import type { OrderStatus } from "@/types/api";
import type { ChipProps } from "@mui/material";

type StatusColor = ChipProps["color"];

interface StatusMeta {
  label: string;
  chipColor: StatusColor;
}

// ============================================================================
// Order Status
// ============================================================================

export const orderStatuses: OrderStatus[] = [
  "Created",
  "Invited",
  "IntakeInProgress",
  "IntakeComplete",
  "ReadyForRouting",
  "RoutingInProgress",
  "ReadyForReport",
  "Closed",
  "Blocked",
  "Canceled",
];

export const openStatuses: OrderStatus[] = [
  "Created",
  "Invited",
  "IntakeInProgress",
  "IntakeComplete",
  "ReadyForRouting",
  "RoutingInProgress",
  "ReadyForReport",
  "Blocked",
];

export const closedStatuses: OrderStatus[] = ["Closed", "Canceled"];

const orderStatusMeta: Record<OrderStatus, StatusMeta> = {
  Created: { label: "Created", chipColor: "default" },
  Invited: { label: "Invited", chipColor: "default" },
  IntakeInProgress: { label: "Intake In Progress", chipColor: "warning" },
  IntakeComplete: { label: "Intake Complete", chipColor: "info" },
  ReadyForRouting: { label: "Ready for Routing", chipColor: "success" },
  RoutingInProgress: { label: "Routing In Progress", chipColor: "warning" },
  ReadyForReport: { label: "Ready for Report", chipColor: "info" },
  Closed: { label: "Closed", chipColor: "success" },
  Blocked: { label: "Blocked", chipColor: "error" },
  Canceled: { label: "Canceled", chipColor: "default" },
};

export const isOrderStatus = (value: string | null): value is OrderStatus =>
  !!value && orderStatuses.includes(value as OrderStatus);

export const getOrderStatusLabel = (status: string): string => {
  if (isOrderStatus(status)) {
    return orderStatusMeta[status].label;
  }
  return status;
};

export const getOrderStatusColor = (status: string): StatusColor => {
  if (isOrderStatus(status)) {
    return orderStatusMeta[status].chipColor;
  }
  return "default";
};

// ============================================================================
// User Status
// ============================================================================

export type UserStatus = "Active" | "Invited" | "Suspended";

const userStatuses: UserStatus[] = ["Active", "Invited", "Suspended"];

const userStatusMeta: Record<UserStatus, StatusMeta> = {
  Active: { label: "Active", chipColor: "success" },
  Invited: { label: "Invited", chipColor: "warning" },
  Suspended: { label: "Suspended", chipColor: "error" },
};

const isUserStatus = (value: string): value is UserStatus =>
  userStatuses.includes(value as UserStatus);

export const getUserStatusLabel = (status: string): string => {
  if (isUserStatus(status)) {
    return userStatusMeta[status].label;
  }
  return status;
};

export const getUserStatusColor = (status: string): StatusColor => {
  if (isUserStatus(status)) {
    return userStatusMeta[status].chipColor;
  }
  return "default";
};

// ============================================================================
// Subject Status
// ============================================================================

export type SubjectStatus = "Active" | "Merged" | "Archived";

const subjectStatuses: SubjectStatus[] = ["Active", "Merged", "Archived"];

const subjectStatusMeta: Record<SubjectStatus, StatusMeta> = {
  Active: { label: "Active", chipColor: "success" },
  Merged: { label: "Merged", chipColor: "warning" },
  Archived: { label: "Archived", chipColor: "default" },
};

const isSubjectStatus = (value: string): value is SubjectStatus =>
  subjectStatuses.includes(value as SubjectStatus);

export const getSubjectStatusLabel = (status: string): string => {
  if (isSubjectStatus(status)) {
    return subjectStatusMeta[status].label;
  }
  return status;
};

export const getSubjectStatusColor = (status: string): StatusColor => {
  if (isSubjectStatus(status)) {
    return subjectStatusMeta[status].chipColor;
  }
  return "default";
};

// ============================================================================
// Customer Status
// ============================================================================

export type CustomerStatus = "Active" | "Suspended";

const customerStatuses: CustomerStatus[] = ["Active", "Suspended"];

const customerStatusMeta: Record<CustomerStatus, StatusMeta> = {
  Active: { label: "Active", chipColor: "success" },
  Suspended: { label: "Suspended", chipColor: "error" },
};

const isCustomerStatus = (value: string): value is CustomerStatus =>
  customerStatuses.includes(value as CustomerStatus);

export const getCustomerStatusLabel = (status: string): string => {
  if (isCustomerStatus(status)) {
    return customerStatusMeta[status].label;
  }
  return status;
};

export const getCustomerStatusColor = (status: string): StatusColor => {
  if (isCustomerStatus(status)) {
    return customerStatusMeta[status].chipColor;
  }
  return "default";
};

// ============================================================================
// Unified Status API
// ============================================================================

export type EntityType = "order" | "user" | "subject" | "customer";

export const getStatusLabel = (type: EntityType, status: string): string => {
  switch (type) {
    case "order":
      return getOrderStatusLabel(status);
    case "user":
      return getUserStatusLabel(status);
    case "subject":
      return getSubjectStatusLabel(status);
    case "customer":
      return getCustomerStatusLabel(status);
    default:
      return status;
  }
};

export const getStatusColor = (
  type: EntityType,
  status: string,
): StatusColor => {
  switch (type) {
    case "order":
      return getOrderStatusColor(status);
    case "user":
      return getUserStatusColor(status);
    case "subject":
      return getSubjectStatusColor(status);
    case "customer":
      return getCustomerStatusColor(status);
    default:
      return "default";
  }
};
