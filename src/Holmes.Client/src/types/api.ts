export type Ulid = string;

export type UserStatus = "PendingApproval" | "Active" | "Suspended";

export type UserRole =
    | "Admin"
    | "CustomerAdmin"
    | "Compliance"
    | "Operations"
    | "Auditor";

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

export interface CurrentUserDto {
    id: Ulid;
    email: string;
    displayName?: string;
    roles: UserRole[];
}

export interface InviteUserRole {
    role: UserRole;
    customerId?: Ulid | null;
}

export interface InviteUserRequest {
    email: string;
    displayName?: string;
    sendInviteEmail?: boolean;
    roles: InviteUserRole[];
}

export interface GrantUserRoleRequest {
    role: UserRole;
    customerId?: Ulid | null;
}

export type CustomerStatus = "Active" | "Suspended";

export interface CustomerContactDto {
    id: Ulid;
    name: string;
    email: string;
    phone?: string;
    role?: string;
}

export interface CustomerDto {
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

export interface CreateCustomerContact {
    name: string;
    email: string;
    phone?: string;
    role?: string;
}

export interface CreateCustomerRequest {
    name: string;
    policySnapshotId: string;
    billingEmail?: string;
    contacts?: CreateCustomerContact[];
}

export type SubjectStatus = "Active" | "Merged" | "Archived";

export interface SubjectAliasDto {
    id: Ulid;
    firstName: string;
    lastName: string;
    middleName?: string;
    createdAt: string;
}

export interface SubjectDto {
    id: Ulid;
    firstName: string;
    lastName: string;
    middleName?: string;
    birthDate?: string;
    ssnLast4?: string;
    status: SubjectStatus;
    mergeParentId?: Ulid | null;
    aliases: SubjectAliasDto[];
    createdAt: string;
    updatedAt: string;
}

export interface MergeSubjectsRequest {
    winnerSubjectId: Ulid;
    mergedSubjectId: Ulid;
    reason?: string;
}

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
