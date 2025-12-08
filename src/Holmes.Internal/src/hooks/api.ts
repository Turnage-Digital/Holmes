import { apiFetch, createEventSource, toQueryString } from "@holmes/ui-core";
import { useMutation, useQuery, useQueryClient, UseQueryOptions } from "@tanstack/react-query";

import type {
  CreateCustomerRequest,
  CreateOrderRequest,
  CurrentUserDto,
  CustomerDetailDto,
  CustomerListItemDto,
  CustomerServiceCatalogDto,
  GrantUserRoleRequest,
  InviteUserRequest,
  InviteUserResponse,
  IssueIntakeInviteRequest,
  OrderServicesDto,
  OrderStatsDto,
  OrderSummaryDto,
  OrderSummaryQuery,
  OrderTimelineEntryDto,
  PaginatedResult,
  RegisterSubjectRequest,
  ServiceTypeDto,
  SubjectDetailDto,
  SubjectListItemDto,
  SubjectSummaryDto,
  Ulid,
  UpdateCatalogServiceRequest,
  UpdateTierConfigurationRequest,
  UserDto
} from "@/types/api";

// ============================================================================
// Query Keys
// ============================================================================

export const queryKeys = {
  currentUser: ["currentUser"] as const,
  users: (page: number, pageSize: number) => ["users", page, pageSize] as const,
  customers: (page: number, pageSize: number) =>
    ["customers", page, pageSize] as const,
  customer: (id: Ulid) => ["customers", id] as const,
  customerCatalog: (id: Ulid) => ["customers", id, "catalog"] as const,
  subjects: (page: number, pageSize: number) =>
    ["subjects", page, pageSize] as const,
  subject: (id: Ulid) => ["subjects", id] as const,
  orders: (query: OrderSummaryQuery) => ["orders", query] as const,
  order: (id: Ulid) => ["orders", "detail", id] as const,
  orderTimeline: (id: Ulid) => ["orders", "timeline", id] as const,
  orderServices: (id: Ulid) => ["orders", id, "services"] as const,
  orderStats: ["orders", "stats"] as const,
  serviceTypes: ["services", "types"] as const
};

// ============================================================================
// Current User
// ============================================================================

const fetchCurrentUser = () => apiFetch<CurrentUserDto>("/users/me");

// Cache for 5 minutes
export const useCurrentUser = (
  options?: Omit<UseQueryOptions<CurrentUserDto>, "queryKey" | "queryFn">
) =>
  useQuery({
    queryKey: queryKeys.currentUser,
    queryFn: fetchCurrentUser,
    staleTime: 5 * 60 * 1000,
    ...options
  });

export const useIsAdmin = () => {
  const { data: user } = useCurrentUser();
  return user?.roles.some((r) => r.role === "Admin") ?? false;
};

// ============================================================================
// Users
// ============================================================================

const fetchUsers = ({ page, pageSize }: { page: number; pageSize: number }) =>
  apiFetch<PaginatedResult<UserDto>>(
    `/users${toQueryString({ page, pageSize })}`
  );

export const useUsers = (page: number, pageSize: number) =>
  useQuery({
    queryKey: queryKeys.users(page, pageSize),
    queryFn: () => fetchUsers({ page, pageSize })
  });

const inviteUser = (payload: InviteUserRequest) =>
  apiFetch<InviteUserResponse>("/users/invitations", {
    method: "POST",
    body: payload
  });

export const useInviteUser = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: inviteUser,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
    }
  });
};

const grantRole = ({
                     userId,
                     payload
                   }: {
  userId: Ulid;
  payload: GrantUserRoleRequest;
}) =>
  apiFetch<void>(`/users/${userId}/roles`, {
    method: "POST",
    body: payload
  });

export const useGrantRole = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: grantRole,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
    }
  });
};

const revokeRole = ({
                      userId,
                      payload
                    }: {
  userId: Ulid;
  payload: GrantUserRoleRequest;
}) =>
  apiFetch<void>(`/users/${userId}/roles`, {
    method: "DELETE",
    body: payload
  });

export const useRevokeRole = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: revokeRole,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
    }
  });
};

// ============================================================================
// Customers
// ============================================================================

const fetchCustomers = ({
                          page,
                          pageSize
                        }: {
  page: number;
  pageSize: number;
}) =>
  apiFetch<PaginatedResult<CustomerListItemDto>>(
    `/customers${toQueryString({ page, pageSize })}`
  );

export const useCustomers = (page: number, pageSize: number) =>
  useQuery({
    queryKey: queryKeys.customers(page, pageSize),
    queryFn: () => fetchCustomers({ page, pageSize })
  });

const fetchCustomer = (customerId: Ulid) =>
  apiFetch<CustomerDetailDto>(`/customers/${customerId}`);

export const useCustomer = (customerId: Ulid) =>
  useQuery({
    queryKey: queryKeys.customer(customerId),
    queryFn: () => fetchCustomer(customerId),
    enabled: !!customerId
  });

const createCustomer = (payload: CreateCustomerRequest) =>
  apiFetch<CustomerListItemDto>("/customers", {
    method: "POST",
    body: payload
  });

export const useCreateCustomer = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createCustomer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["customers"] });
    }
  });
};

// ============================================================================
// Subjects
// ============================================================================

const fetchSubjects = ({
                         page,
                         pageSize
                       }: {
  page: number;
  pageSize: number;
}) =>
  apiFetch<PaginatedResult<SubjectListItemDto>>(
    `/subjects${toQueryString({ page, pageSize })}`
  );

export const useSubjects = (page: number, pageSize: number) =>
  useQuery({
    queryKey: queryKeys.subjects(page, pageSize),
    queryFn: () => fetchSubjects({ page, pageSize })
  });

const fetchSubject = (subjectId: Ulid) =>
  apiFetch<SubjectDetailDto>(`/subjects/${subjectId}`);

export const useSubject = (subjectId: Ulid) =>
  useQuery({
    queryKey: queryKeys.subject(subjectId),
    queryFn: () => fetchSubject(subjectId),
    enabled: !!subjectId
  });

const registerSubject = (payload: RegisterSubjectRequest) =>
  apiFetch<SubjectSummaryDto>("/subjects", {
    method: "POST",
    body: payload
  });

export const useRegisterSubject = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: registerSubject,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subjects"] });
    }
  });
};

// ============================================================================
// Orders
// ============================================================================

const fetchOrders = (query: OrderSummaryQuery) =>
  apiFetch<PaginatedResult<OrderSummaryDto>>(
    `/orders/summary${toQueryString(query)}`
  );

export const useOrders = (query: OrderSummaryQuery) =>
  useQuery({
    queryKey: queryKeys.orders(query),
    queryFn: () => fetchOrders(query)
  });

const fetchOrder = (orderId: Ulid) =>
  apiFetch<PaginatedResult<OrderSummaryDto>>(
    `/orders/summary${toQueryString({ orderId })}`
  ).then((result) => result.items[0] ?? null);

export const useOrder = (orderId: Ulid) =>
  useQuery({
    queryKey: queryKeys.order(orderId),
    queryFn: () => fetchOrder(orderId),
    enabled: !!orderId
  });

const fetchOrderTimeline = (orderId: Ulid) =>
  apiFetch<OrderTimelineEntryDto[]>(`/orders/${orderId}/timeline`);

export const useOrderTimeline = (orderId: Ulid) =>
  useQuery({
    queryKey: queryKeys.orderTimeline(orderId),
    queryFn: () => fetchOrderTimeline(orderId),
    enabled: !!orderId
  });

const fetchOrderStats = () => apiFetch<OrderStatsDto>("/orders/stats");

export const useOrderStats = () =>
  useQuery({
    queryKey: queryKeys.orderStats,
    queryFn: fetchOrderStats
  });

const createOrder = (payload: CreateOrderRequest) =>
  apiFetch<OrderSummaryDto>("/orders", {
    method: "POST",
    body: payload
  });

export const useCreateOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
    }
  });
};

// ============================================================================
// Intake
// ============================================================================

const issueIntakeInvite = (payload: IssueIntakeInviteRequest) =>
  apiFetch<void>("/intake/sessions", {
    method: "POST",
    body: payload
  });

export const useIssueIntakeInvite = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: issueIntakeInvite,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
    }
  });
};

// ============================================================================
// Services
// ============================================================================

const fetchServiceTypes = () => apiFetch<ServiceTypeDto[]>("/services/types");

export const useServiceTypes = () =>
  useQuery({
    queryKey: queryKeys.serviceTypes,
    queryFn: fetchServiceTypes,
    staleTime: 10 * 60 * 1000 // 10 minutes
  });

const fetchOrderServices = (orderId: Ulid) =>
  apiFetch<OrderServicesDto>(`/orders/${orderId}/services`);

export const useOrderServices = (orderId: Ulid) =>
  useQuery({
    queryKey: queryKeys.orderServices(orderId),
    queryFn: () => fetchOrderServices(orderId),
    enabled: !!orderId
  });

// ============================================================================
// Customer Service Catalog
// ============================================================================

const fetchCustomerCatalog = (customerId: Ulid) =>
  apiFetch<CustomerServiceCatalogDto>(`/customers/${customerId}/catalog`);

export const useCustomerCatalog = (customerId: Ulid) =>
  useQuery({
    queryKey: queryKeys.customerCatalog(customerId),
    queryFn: () => fetchCustomerCatalog(customerId),
    enabled: !!customerId
  });

const updateCatalogService = ({
                                customerId,
                                payload
                              }: {
  customerId: Ulid;
  payload: UpdateCatalogServiceRequest;
}) =>
  apiFetch<void>(`/customers/${customerId}/catalog/services`, {
    method: "PUT",
    body: payload
  });

export const useUpdateCatalogService = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: updateCatalogService,
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.customerCatalog(variables.customerId)
      });
    }
  });
};

const updateTierConfiguration = ({
                                   customerId,
                                   payload
                                 }: {
  customerId: Ulid;
  payload: UpdateTierConfigurationRequest;
}) =>
  apiFetch<void>(`/customers/${customerId}/catalog/tiers`, {
    method: "PUT",
    body: payload
  });

export const useUpdateTierConfiguration = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: updateTierConfiguration,
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.customerCatalog(variables.customerId)
      });
    }
  });
};

// ============================================================================
// SSE Helpers
// ============================================================================

export const createOrderChangesStream = () =>
  createEventSource("/orders/changes");
