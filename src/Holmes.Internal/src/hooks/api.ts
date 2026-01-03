import { apiFetch, createEventSource, toQueryString } from "@holmes/ui-core";
import { useMutation, useQuery, useQueryClient, UseQueryOptions } from "@tanstack/react-query";

import type {
  CancelService,
  CreateCustomerRequest,
  CreateOrderRequest,
  CreateOrderWithIntakeResponse,
  CurrentUserDto,
  CustomerDetailDto,
  CustomerListItemDto,
  CustomerServiceCatalogDto,
  FulfillmentQueueQuery,
  GrantUserRoleRequest,
  InviteUserRequest,
  InviteUserResponse,
  IssueIntakeInviteRequest,
  NotificationSummaryDto,
  OrderAuditEventDto,
  OrderServicesDto,
  OrderStatsDto,
  OrderSummaryDto,
  OrderSummaryQuery,
  OrderTimelineEntryDto,
  PaginatedResult,
  PauseClockRequest,
  RegisterSubjectRequest,
  ServiceSummaryDto,
  ServiceTypeDto,
  SlaClockDto,
  SubjectDetailDto,
  SubjectListItemDto,
  SubjectSummaryDto,
  Ulid,
  UpdateServiceCatalogRequest,
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
  orderEvents: (id: Ulid) => ["orders", id, "events"] as const,
  orderServices: (id: Ulid) => ["orders", id, "services"] as const,
  orderSlaClocks: (id: Ulid) => ["orders", id, "slaClocks"] as const,
  orderNotifications: (id: Ulid) => ["orders", id, "notifications"] as const,
  orderStats: ["orders", "stats"] as const,
  serviceTypes: ["services", "types"] as const,
  fulfillmentQueue: (query: FulfillmentQueueQuery) =>
    ["services", "queue", query] as const
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
  const hasAdminRole = user?.roles.some((r) => r.role === "Admin");
  return hasAdminRole ?? false;
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

const fetchOrderEvents = (orderId: Ulid) =>
  apiFetch<OrderAuditEventDto[]>(`/orders/${orderId}/events`);

export const useOrderEvents = (orderId: Ulid) =>
  useQuery({
    queryKey: queryKeys.orderEvents(orderId),
    queryFn: () => fetchOrderEvents(orderId),
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

const createOrderWithIntake = (
  payload: CreateOrderRequest
): Promise<CreateOrderWithIntakeResponse> =>
  apiFetch<CreateOrderWithIntakeResponse>("/orders", {
    method: "POST",
    body: payload
  });

export const useCreateOrderWithIntake = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createOrderWithIntake,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["subjects"] });
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

// Cache for 10 minutes
export const useServiceTypes = () =>
  useQuery({
    queryKey: queryKeys.serviceTypes,
    queryFn: fetchServiceTypes,
    staleTime: 10 * 60 * 1000
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
// Fulfillment Queue
// ============================================================================

const fetchFulfillmentQueue = (query: FulfillmentQueueQuery) =>
  apiFetch<PaginatedResult<ServiceSummaryDto>>(
    `/services/queue${toQueryString(query)}`
  );

export const useFulfillmentQueue = (query: FulfillmentQueueQuery) =>
  useQuery({
    queryKey: queryKeys.fulfillmentQueue(query),
    queryFn: () => fetchFulfillmentQueue(query)
  });

const retryService = (serviceId: Ulid) =>
  apiFetch<void>(`/services/${serviceId}/retry`, {
    method: "POST"
  });

export const useRetryService = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: retryService,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["services"] });
    }
  });
};

const cancelService = ({
                         serviceId,
                         payload
                       }: {
  serviceId: Ulid;
  payload: CancelService;
}) =>
  apiFetch<void>(`/services/${serviceId}/cancel`, {
    method: "POST",
    body: payload
  });

export const useCancelService = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cancelService,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
      queryClient.invalidateQueries({ queryKey: ["services"] });
    }
  });
};

// ============================================================================
// Customer Service Catalog
// ============================================================================

const fetchCustomerCatalog = (customerId: Ulid) =>
  apiFetch<CustomerServiceCatalogDto>(
    `/customers/${customerId}/service-catalog`
  );

export const useCustomerCatalog = (customerId: Ulid) =>
  useQuery({
    queryKey: queryKeys.customerCatalog(customerId),
    queryFn: () => fetchCustomerCatalog(customerId),
    enabled: !!customerId
  });

const updateServiceCatalog = ({
                                customerId,
                                payload
                              }: {
  customerId: Ulid;
  payload: UpdateServiceCatalogRequest;
}) =>
  apiFetch<void>(`/customers/${customerId}/service-catalog`, {
    method: "PUT",
    body: payload
  });

export const useUpdateServiceCatalog = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: updateServiceCatalog,
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.customerCatalog(variables.customerId)
      });
    }
  });
};

// ============================================================================
// SLA Clocks
// ============================================================================

const fetchOrderSlaClocks = (orderId: Ulid) =>
  apiFetch<SlaClockDto[]>(`/clocks/sla?orderId=${orderId}`);

export const useOrderSlaClocks = (orderId: Ulid) =>
  useQuery({
    queryKey: queryKeys.orderSlaClocks(orderId),
    queryFn: () => fetchOrderSlaClocks(orderId),
    enabled: !!orderId
  });

const pauseSlaClock = ({
                         clockId,
                         payload
                       }: {
  clockId: Ulid;
  payload: PauseClockRequest;
}) =>
  apiFetch<void>(`/clocks/sla/${clockId}/pause`, {
    method: "POST",
    body: payload
  });

export const usePauseSlaClock = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: pauseSlaClock,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
    }
  });
};

const resumeSlaClock = (clockId: Ulid) =>
  apiFetch<void>(`/clocks/sla/${clockId}/resume`, {
    method: "POST"
  });

export const useResumeSlaClock = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: resumeSlaClock,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
    }
  });
};

// ============================================================================
// Notifications
// ============================================================================

const fetchOrderNotifications = (orderId: Ulid) =>
  apiFetch<NotificationSummaryDto[]>(`/notifications?orderId=${orderId}`);

export const useOrderNotifications = (orderId: Ulid) =>
  useQuery({
    queryKey: queryKeys.orderNotifications(orderId),
    queryFn: () => fetchOrderNotifications(orderId),
    enabled: !!orderId
  });

const retryNotification = (notificationId: Ulid) =>
  apiFetch<void>(`/notifications/${notificationId}/retry`, {
    method: "POST"
  });

export const useRetryNotification = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: retryNotification,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["orders"] });
    }
  });
};

// ============================================================================
// SSE Helpers
// ============================================================================

export const createOrderChangesStream = () =>
  createEventSource("/orders/changes");

export const createServiceChangesStream = (orderId?: Ulid) =>
  createEventSource(
    orderId ? `/services/changes?orderId=${orderId}` : "/services/changes"
  );

export const createSlaClockChangesStream = (orderId?: Ulid) =>
  createEventSource(
    orderId ? `/clocks/sla/changes?orderId=${orderId}` : "/clocks/sla/changes"
  );
