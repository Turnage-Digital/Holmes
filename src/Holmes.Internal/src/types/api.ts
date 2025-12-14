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

// Subject Detail Types (returned by GET /subjects/{id})
export type AddressType = "Residential" | "Mailing" | "Business" | "Unknown";
export type PhoneType = "Mobile" | "Home" | "Work" | "Unknown";
export type ReferenceType = "Personal" | "Professional" | "Unknown";

export interface SubjectAddressDto {
  id: Ulid;
  street1: string;
  street2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  countyFips?: string;
  fromDate: string;
  toDate?: string;
  isCurrent: boolean;
  type: AddressType;
  createdAt: string;
}

export interface SubjectEmploymentDto {
  id: Ulid;
  employerName: string;
  employerPhone?: string;
  employerAddress?: string;
  jobTitle?: string;
  supervisorName?: string;
  supervisorPhone?: string;
  startDate: string;
  endDate?: string;
  isCurrent: boolean;
  reasonForLeaving?: string;
  canContact: boolean;
  createdAt: string;
}

export interface SubjectEducationDto {
  id: Ulid;
  institutionName: string;
  institutionAddress?: string;
  degree?: string;
  major?: string;
  attendedFrom?: string;
  attendedTo?: string;
  graduationDate?: string;
  graduated: boolean;
  createdAt: string;
}

export interface SubjectReferenceDto {
  id: Ulid;
  name: string;
  phone?: string;
  email?: string;
  relationship?: string;
  yearsKnown?: number;
  type: ReferenceType;
  createdAt: string;
}

export interface SubjectPhoneDto {
  id: Ulid;
  phoneNumber: string;
  type: PhoneType;
  isPrimary: boolean;
  createdAt: string;
}

export interface SubjectDetailDto {
  id: Ulid;
  firstName: string;
  middleName?: string;
  lastName: string;
  birthDate?: string;
  email?: string;
  ssnLast4?: string;
  status: SubjectStatus;
  mergeParentId?: Ulid | null;
  aliases: SubjectAliasDto[];
  addresses: SubjectAddressDto[];
  employments: SubjectEmploymentDto[];
  educations: SubjectEducationDto[];
  references: SubjectReferenceDto[];
  phones: SubjectPhoneDto[];
  createdAt: string;
  updatedAt: string;
}

// ============================================================================
// Order Types
// ============================================================================

export type OrderStatus =
  | "Created"
  | "Invited"
  | "IntakeInProgress"
  | "IntakeComplete"
  | "ReadyForFulfillment"
  | "FulfillmentInProgress"
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
  readyForFulfillmentAt?: string | null;
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

export interface OrderAuditEventDto {
  position: number;
  version: number;
  eventId: string;
  eventName: string;
  payload: Record<string, unknown>;
  createdAt: string;
  correlationId?: string | null;
  actorId?: string | null;
}

export interface OrderStatsDto {
  invited: number;
  intakeInProgress: number;
  intakeComplete: number;
  readyForFulfillment: number;
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

// ============================================================================
// Service Types
// ============================================================================

export type ServiceCategory =
  | "Criminal"
  | "Identity"
  | "Employment"
  | "Education"
  | "Driving"
  | "Credit"
  | "Drug"
  | "Civil"
  | "Reference"
  | "Healthcare"
  | "Custom";

export type ServiceStatus =
  | "Pending"
  | "Dispatched"
  | "InProgress"
  | "Completed"
  | "Failed"
  | "Canceled";

export interface ServiceTypeDto {
  code: string;
  displayName: string;
  category: ServiceCategory;
  defaultTier: number;
}

export interface ServiceRequestSummaryDto {
  id: Ulid;
  orderId: Ulid;
  customerId: Ulid;
  serviceTypeCode: string;
  category: ServiceCategory;
  tier: number;
  status: ServiceStatus;
  vendorCode?: string;
  vendorReferenceId?: string;
  attemptCount: number;
  maxAttempts: number;
  lastError?: string;
  scopeType?: string;
  scopeValue?: string;
  createdAt: string;
  dispatchedAt?: string;
  completedAt?: string;
  failedAt?: string;
  canceledAt?: string;
}

export interface OrderServicesDto {
  orderId: Ulid;
  services: ServiceRequestSummaryDto[];
  totalServices: number;
  completedServices: number;
  pendingServices: number;
  failedServices: number;
}

// ============================================================================
// Customer Service Catalog Types
// ============================================================================

export interface CatalogServiceItemDto {
  serviceTypeCode: string;
  displayName: string;
  category: ServiceCategory;
  isEnabled: boolean;
  tier: number;
  vendorCode?: string;
}

export interface TierConfigurationDto {
  tier: number;
  name: string;
  description?: string;
  requiredServices: string[];
  optionalServices: string[];
  autoDispatch: boolean;
  waitForPreviousTier: boolean;
}

export interface CustomerServiceCatalogDto {
  customerId: Ulid;
  services: CatalogServiceItemDto[];
  tiers: TierConfigurationDto[];
  updatedAt: string;
}

export interface ServiceCatalogServiceInput {
  serviceTypeCode: string;
  isEnabled: boolean;
  tier: number;
  vendorCode?: string;
}

export interface ServiceCatalogTierInput {
  tier: number;
  name: string;
  description?: string;
  requiredServices?: string[];
  optionalServices?: string[];
  autoDispatch: boolean;
  waitForPreviousTier: boolean;
}

export interface UpdateServiceCatalogRequest {
  services?: ServiceCatalogServiceInput[];
  tiers?: ServiceCatalogTierInput[];
}

// ============================================================================
// Fulfillment Queue Types
// ============================================================================

export interface FulfillmentQueueQuery {
  page?: number;
  pageSize?: number;
  customerId?: Ulid;
  status?: ServiceStatus[];
  category?: ServiceCategory[];

  [key: string]:
    | string
    | number
    | ServiceStatus[]
    | ServiceCategory[]
    | Ulid
    | undefined;
}

// ============================================================================
// SLA Clock Types
// ============================================================================

export type ClockKind = "Intake" | "Fulfillment" | "Overall" | "Custom";

export type ClockState =
  | "Running"
  | "AtRisk"
  | "Breached"
  | "Paused"
  | "Completed";

export interface SlaClockDto {
  id: Ulid;
  orderId: Ulid;
  customerId: Ulid;
  kind: ClockKind;
  state: ClockState;
  startedAt: string;
  deadlineAt: string;
  atRiskThresholdAt: string;
  atRiskAt?: string | null;
  breachedAt?: string | null;
  pausedAt?: string | null;
  completedAt?: string | null;
  pauseReason?: string | null;
  accumulatedPauseTime: string;
  targetBusinessDays: number;
  atRiskThresholdPercent: number;
}

export interface PauseClockRequest {
  reason: string;
}

// ============================================================================
// Notification Types
// ============================================================================

export type NotificationTriggerType =
  | "IntakeSessionInvited"
  | "IntakeSubmissionReceived"
  | "ConsentCaptured"
  | "OrderStateChanged"
  | "SlaClockAtRisk"
  | "SlaClockBreached"
  | "NotificationFailed";

export type NotificationChannel = "Email" | "Sms" | "Webhook";

export type DeliveryStatus =
  | "Pending"
  | "Queued"
  | "Sending"
  | "Delivered"
  | "Failed"
  | "Bounced"
  | "Cancelled";

export interface NotificationSummaryDto {
  id: Ulid;
  customerId: Ulid;
  orderId?: Ulid | null;
  triggerType: NotificationTriggerType;
  channel: NotificationChannel;
  recipientAddress: string;
  status: DeliveryStatus;
  isAdverseAction: boolean;
  createdAt: string;
  deliveredAt?: string | null;
  deliveryAttemptCount: number;
}
