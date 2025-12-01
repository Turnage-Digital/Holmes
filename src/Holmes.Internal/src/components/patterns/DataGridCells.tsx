import React from "react";

import { formatDistanceToNow } from "date-fns";

import { useCustomer, useSubject } from "@/hooks/api";

/**
 * Displays a truncated ID in monospace font.
 * Shows first 12 characters with ellipsis.
 */
export const MonospaceIdCell = ({ id }: { id: string }) => (
  <span style={{ fontFamily: "monospace" }}>{id.slice(0, 12)}…</span>
);

/**
 * Displays a timestamp as relative time (e.g., "2 hours ago").
 */
export const RelativeTimeCell = ({ timestamp }: { timestamp: string }) =>
  formatDistanceToNow(new Date(timestamp), { addSuffix: true });

/**
 * Displays a relative time, or "Never" if timestamp is null/undefined.
 */
export const OptionalRelativeTimeCell = ({
  timestamp,
}: {
  timestamp: string | null | undefined;
}) =>
  timestamp
    ? formatDistanceToNow(new Date(timestamp), { addSuffix: true })
    : "Never";

/**
 * Fetches and displays a subject's name.
 * Shows truncated ID while loading, then displays the resolved name.
 */
export const SubjectNameCell = ({ subjectId }: { subjectId: string }) => {
  const { data: subject } = useSubject(subjectId);

  if (!subject) {
    return <>{subjectId.slice(0, 8)}…</>;
  }

  const name = [subject.givenName, subject.familyName]
    .filter(Boolean)
    .join(" ");
  return <>{name || subject.email || `${subjectId.slice(0, 8)}…`}</>;
};

/**
 * Fetches and displays a customer's name.
 * Shows truncated ID while loading.
 */
export const CustomerNameCell = ({ customerId }: { customerId: string }) => {
  const { data: customer } = useCustomer(customerId);
  return <span>{customer?.name ?? `${customerId.slice(0, 8)}…`}</span>;
};

/**
 * Displays a value or a dash if empty/null.
 */
export const EmptyableCell = ({
  value,
}: {
  value: string | null | undefined;
}) => <>{value || "—"}</>;
