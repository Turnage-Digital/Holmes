export type Ulid = string;

// ============================================================================
// User Types
// ============================================================================

export type UserStatus = "Invited" | "Active" | "Suspended";

export type UserRole = "Admin" | "Operations";

export interface RoleAssignmentDto {
  id: Ulid;
  role: UserRole;
  customerId?: Ulid | null;
  grantedBy?: Ulid | null;
  grantedAt: string;
}

export interface ExternalIdentityDto {
  issuer: string;
  subject: string;
  authenticationMethod?: string;
  linkedAt: string;
  lastSeenAt?: string;
}

export interface UserDto {
  id: Ulid;
  email: string;
  displayName?: string;
  status: UserStatus;
  roleAssignments: RoleAssignmentDto[];
  externalIdentity?: ExternalIdentityDto;
  lastSeenAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface UserRoleDto {
  role: UserRole;
  customerId?: Ulid | null;
}

export interface CurrentUserDto {
  userId: Ulid;
  email: string;
  displayName?: string;
  issuer?: string;
  subject?: string;
  status: UserStatus;
  lastAuthenticatedAt: string;
  roles: UserRoleDto[];
}

export interface InviteUserRoleRequest {
  role: UserRole;
  customerId?: Ulid | null;
}

export interface InviteUserRequest {
  email: string;
  displayName?: string;
  sendInviteEmail?: boolean;
  roles: InviteUserRoleRequest[];
}

export interface InviteUserResponse {
  user: UserDto;
  confirmationLink?: string;
}

export interface GrantUserRoleRequest {
  role: UserRole;
  customerId?: Ulid | null;
}

// ============================================================================
// Customer Types
// ============================================================================

export type CustomerStatus = "Active" | "Suspended";

export interface CustomerContactDto {
  id: Ulid;
  name: string;
  email: string;
  phone?: string;
  role?: string;
}

export interface CustomerAdminDto {
  userId: Ulid;
  assignedBy?: Ulid;
  assignedAt: string;
}

export interface CustomerListItemDto {
  id: Ulid;
  tenantId: Ulid;
  name: string;
  status: CustomerStatus;
  policySnapshotId: string;
  billingEmail?: string;
  contacts: CustomerContactDto[];
  createdAt: string;
  updatedAt: string;
}

export interface CustomerDetailDto {
  id: Ulid;
  tenantId: Ulid;
  name: string;
  status: CustomerStatus;
  policySnapshotId: string;
  billingEmail?: string;
  contacts: CustomerContactDto[];
  admins: CustomerAdminDto[];
  createdAt: string;
  updatedAt: string;
}

export interface CreateCustomerContactRequest {
  name: string;
  email: string;
  phone?: string;
  role?: string;
}

export interface CreateCustomerRequest {
  name: string;
  policySnapshotId: string;
  billingEmail?: string;
  contacts?: CreateCustomerContactRequest[];
}

// ============================================================================
// Subject Types
// ============================================================================

export type SubjectStatus = "Active" | "Merged" | "Archived";

export interface SubjectAliasDto {
  id: Ulid;
  firstName: string;
  lastName: string;
  birthDate?: string;
  createdAt: string;
}

export interface SubjectListItemDto {
  id: Ulid;
  firstName: string;
  middleName?: string;
  lastName: string;
  birthDate?: string;
  email?: string;
  status: SubjectStatus;
  mergeParentId?: Ulid | null;
  aliases: SubjectAliasDto[];
  createdAt: string;
  updatedAt: string;
}

export interface SubjectSummaryDto {
  subjectId: Ulid;
  givenName: string;
  familyName: string;
  dateOfBirth?: string;
  email?: string;
  isMerged: boolean;
  aliasCount: number;
  createdAt: string;
}

export interface RegisterSubjectRequest {
  givenName: string;
  familyName: string;
  dateOfBirth?: string;
  email?: string;
}

export interface MergeSubjectsRequest {
  winnerSubjectId: Ulid;
  mergedSubjectId: Ulid;
  reason?: string;
}

// ============================================================================
// Order Types
// ============================================================================

export type OrderStatus =
  | "Created"
  | "Invited"
  | "IntakeInProgress"
  | "IntakeComplete"
  | "ReadyForRouting"
  | "RoutingInProgress"
  | "ReadyForReport"
  | "Closed"
  | "Blocked"
  | "Canceled";

export interface OrderSummaryDto {
  orderId: Ulid;
  subjectId: Ulid;
  customerId: Ulid;
  policySnapshotId: string;
  packageCode?: string | null;
  status: string;
  lastStatusReason?: string | null;
  lastUpdatedAt: string;
  readyForRoutingAt?: string | null;
  closedAt?: string | null;
  canceledAt?: string | null;
}

export interface OrderTimelineEntryDto {
  eventId: Ulid;
  orderId: Ulid;
  eventType: string;
  description: string;
  source?: string;
  occurredAt: string;
  recordedAt: string;
  metadata?: Record<string, unknown> | null;
}

export interface OrderStatsDto {
  invited: number;
  intakeInProgress: number;
  intakeComplete: number;
  readyForRouting: number;
  blocked: number;
  canceled: number;
}

export interface CreateOrderRequest {
  customerId: Ulid;
  subjectId: Ulid;
  policySnapshotId: string;
  packageCode?: string;
}

export interface OrderSummaryQuery {
  page?: number;
  pageSize?: number;
  customerId?: Ulid;
  subjectId?: Ulid;
  orderId?: Ulid;
  status?: string[];
  [key: string]: string | number | string[] | Ulid | undefined;
}

// ============================================================================
// Intake Types
// ============================================================================

export interface IssueIntakeInviteRequest {
  orderId: Ulid;
  subjectId: Ulid;
  customerId: Ulid;
  policySnapshotId: string;
  policySnapshotSchemaVersion?: string;
  policyMetadata?: Record<string, string>;
  timeToLiveHours?: number;
  policyCapturedAt?: string;
  resumeToken?: string;
}

// ============================================================================
// Pagination Types
// ============================================================================

export interface PaginationQuery {
  page?: number;
  pageSize?: number;
}

export interface PaginatedResult<TItem> {
  items: TItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

// ============================================================================
// SSE Event Types
// ============================================================================

export interface OrderChangeEvent {
  orderId: Ulid;
  status: string;
  reason?: string;
  changedAt: string;
}
