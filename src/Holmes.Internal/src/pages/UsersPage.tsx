import React, { FormEvent, useCallback, useEffect, useMemo, useState } from "react";

import { apiFetch, toQueryString } from "@holmes/ui-core";
import AddIcon from "@mui/icons-material/Add";
import RefreshIcon from "@mui/icons-material/Refresh";
import {
  Alert,
  Box,
  Button,
  CardContent,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Switch,
  TextField,
  Typography
} from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { formatDistanceToNow } from "date-fns";

import { PageHeader } from "@/components/layout";
import { AuditPanel, DataGridNoRowsOverlay, SectionCard, SlaBadge, TimelineCard } from "@/components/patterns";
import {
  GrantUserRoleRequest,
  InviteUserRequest,
  PaginatedResult,
  RoleAssignmentDto,
  Ulid,
  UserDto,
  UserRole
} from "@/types/api";
import { getErrorMessage } from "@/utils/errorMessage";

const roleOptions: UserRole[] = ["Admin", "Operations"];

const defaultInviteForm: InviteUserRequest = {
  email: "",
  displayName: "",
  sendInviteEmail: true,
  roles: [{ role: "Admin" }]
};

const initialGrantDialogState = {
  user: null as UserDto | null,
  role: "Admin" as UserRole,
  customerId: ""
};

const formatRoleLabel = (assignment: RoleAssignmentDto) =>
  assignment.customerId
    ? `${assignment.role} (${assignment.customerId})`
    : assignment.role;

const fetchUsersPage = ({
                          page,
                          pageSize
                        }: {
  page: number;
  pageSize: number;
}) =>
  apiFetch<PaginatedResult<UserDto>>(
    `/users${toQueryString({
      page,
      pageSize
    })}`
  );

const inviteUser = (payload: InviteUserRequest) =>
  apiFetch<UserDto>("/users/invitations", {
    method: "POST",
    body: payload
  });

const grantRole = ({
                     userId,
                     payload
                   }: {
  userId: Ulid;
  payload: GrantUserRoleRequest;
}) =>
  apiFetch<UserDto>(`/users/${userId}/roles`, {
    method: "POST",
    body: payload
  });

const revokeRole = ({
                      userId,
                      assignment
                    }: {
  userId: Ulid;
  assignment: RoleAssignmentDto;
}) =>
  apiFetch<UserDto>(`/users/${userId}/roles`, {
    method: "DELETE",
    body: {
      role: assignment.role,
      customerId: assignment.customerId ?? undefined
    }
  });

const UsersPage = () => {
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 25
  });
  const [inviteForm, setInviteForm] =
    useState<InviteUserRequest>(defaultInviteForm);
  const [successMessage, setSuccessMessage] = useState<string>();
  const [clientError, setClientError] = useState<string>();
  const [grantDialog, setGrantDialog] = useState(initialGrantDialogState);

  const queryClient = useQueryClient();

  const usersQuery = useQuery({
    queryKey: ["users", paginationModel.page, paginationModel.pageSize],
    queryFn: () =>
      fetchUsersPage({
        page: paginationModel.page + 1,
        pageSize: paginationModel.pageSize
      }),
    placeholderData: keepPreviousData
  });

  const inviteUserMutation = useMutation({
    mutationFn: inviteUser,
    onMutate: () => {
      setClientError(undefined);
      setSuccessMessage(undefined);
    },
    onSuccess: async (_, variables) => {
      setInviteForm(defaultInviteForm);
      setSuccessMessage(`Invitation sent to ${variables.email}`);
      await queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (err) => setClientError(getErrorMessage(err))
  });

  const revokeRoleMutation = useMutation({
    mutationFn: revokeRole,
    onMutate: () => setClientError(undefined),
    onSuccess: async (_, { assignment }) => {
      setSuccessMessage(`Revoked ${assignment.role} from user`);
      await queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (err) => setClientError(getErrorMessage(err))
  });

  const grantRoleMutation = useMutation({
    mutationFn: grantRole,
    onMutate: () => setClientError(undefined),
    onSuccess: async (_, variables) => {
      if (grantDialog.user) {
        setSuccessMessage(
          `Granted ${variables.payload.role} to ${grantDialog.user.email}`
        );
      }
      setGrantDialog(initialGrantDialogState);
      await queryClient.invalidateQueries({ queryKey: ["users"] });
    },
    onError: (err) => setClientError(getErrorMessage(err))
  });

  const handleInviteSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    inviteUserMutation.mutate(inviteForm);
  };

  const handleRevokeRole = useCallback(
    (userId: Ulid, assignment: RoleAssignmentDto) => {
      revokeRoleMutation.mutate({ userId, assignment });
    },
    [revokeRoleMutation]
  );

  const handleGrantDialogClose = () => {
    grantRoleMutation.reset();
    setGrantDialog(initialGrantDialogState);
  };

  const columns = useMemo<GridColDef<UserDto>[]>(
    () => [
      { field: "email", headerName: "Email", flex: 1, minWidth: 200 },
      { field: "displayName", headerName: "Name", flex: 1, minWidth: 160 },
      { field: "status", headerName: "Status", width: 150 },
      {
        field: "roles",
        headerName: "Roles",
        flex: 1.5,
        minWidth: 280,
        sortable: false,
        renderCell: (params) => (
          <Stack direction="row" spacing={1} flexWrap="wrap">
            {params.row.roleAssignments.map((assignment) => (
              <Chip
                key={assignment.id}
                label={formatRoleLabel(assignment)}
                size="small"
                onDelete={() => handleRevokeRole(params.row.id, assignment)}
              />
            ))}
          </Stack>
        )
      },
      {
        field: "actions",
        headerName: "Actions",
        width: 140,
        sortable: false,
        renderCell: (params) => (
          <Button
            size="small"
            variant="outlined"
            onClick={() => {
              grantRoleMutation.reset();
              setGrantDialog({
                user: params.row,
                role: "Admin",
                customerId: ""
              });
            }}
          >
            Grant role
          </Button>
        )
      }
    ],
    [grantRoleMutation, handleRevokeRole]
  );

  const inviteButtonLabel = inviteUserMutation.isPending
    ? "Inviting..."
    : "Invite";
  const sendEmailEnabled = inviteForm.sendInviteEmail ?? true;
  const tableLoading = usersQuery.isFetching;
  const grantDialogError = grantRoleMutation.isError
    ? getErrorMessage(grantRoleMutation.error)
    : undefined;
  const grantDialogSubmitting = grantRoleMutation.isPending;
  const fetchErrorMessage = usersQuery.error
    ? getErrorMessage(usersQuery.error)
    : undefined;
  const combinedError = clientError ?? fetchErrorMessage;
  const usersResult = usersQuery.data;
  const auditMetrics = useMemo(() => {
    const items = usersResult?.items ?? [];
    const total = usersResult?.totalItems ?? items.length;
    const invited = items.filter((user) => user.status === "Invited").length;
    const active = items.filter((user) => user.status === "Active").length;
    const suspended = items.filter(
      (user) => user.status === "Suspended"
    ).length;
    return [
      {
        label: "Total users",
        value: total.toString(),
        helperText: `${active} active`
      },
      {
        label: "Invited",
        value: invited.toString(),
        helperText: "Awaiting first login"
      },
      {
        label: "Suspended",
        value: suspended.toString(),
        helperText: "Temporarily blocked"
      }
    ];
  }, [usersResult]);

  const timelineItems = useMemo(() => {
    const items = usersResult?.items ?? [];
    return items
      .slice()
      .sort(
        (a, b) =>
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      )
      .slice(0, 5)
      .map((user) => ({
        id: user.id,
        title: user.displayName ?? user.email,
        description:
          user.roleAssignments.length > 0
            ? user.roleAssignments
              .map((assignment) =>
                assignment.customerId
                  ? `${assignment.role} (${assignment.customerId})`
                  : assignment.role
              )
              .join(", ")
            : "No roles granted yet",
        timestamp: formatDistanceToNow(new Date(user.createdAt), {
          addSuffix: true
        }),
        meta: user.status
      }));
  }, [usersResult]);

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Users"
        subtitle="Invite operators, manage access, and prove role audits."
        meta={
          <SlaBadge status="on_track" deadlineLabel="Policy sync due in 2h" />
        }
        actions={
          <Button
            startIcon={<RefreshIcon />}
            onClick={() => usersQuery.refetch()}
            disabled={usersQuery.isFetching}
          >
            Refresh
          </Button>
        }
      />

      {combinedError && <Alert severity="error">{combinedError}</Alert>}
      {successMessage && <Alert severity="success">{successMessage}</Alert>}

      {auditMetrics.length > 0 && (
        <Stack direction={{ xs: "column", lg: "row" }} spacing={3}>
          <Box sx={{ flex: 1, width: "100%" }}>
            <AuditPanel metrics={auditMetrics} />
          </Box>
          <Box sx={{ flex: 1, width: "100%" }}>
            <TimelineCard
              title="Recent changes"
              subtitle="Latest invites and updates"
              items={timelineItems}
            />
          </Box>
        </Stack>
      )}

      <Box component="form" onSubmit={handleInviteSubmit}>
        <SectionCard
          title="Invite user"
          subtitle="Send an invite email and assign at least one role."
        >
          <CardContent>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <TextField
                required
                label="Email"
                type="email"
                value={inviteForm.email}
                onChange={(event) =>
                  setInviteForm((prev) => ({
                    ...prev,
                    email: event.target.value
                  }))
                }
                fullWidth
              />
              <TextField
                label="Display name"
                value={inviteForm.displayName ?? ""}
                onChange={(event) =>
                  setInviteForm((prev) => ({
                    ...prev,
                    displayName: event.target.value
                  }))
                }
                fullWidth
              />
              <FormControl sx={{ minWidth: 200 }}>
                <InputLabel id="invite-role-label">Role</InputLabel>
                <Select
                  labelId="invite-role-label"
                  label="Role"
                  value={inviteForm.roles[0]?.role ?? "Admin"}
                  onChange={(event) =>
                    setInviteForm((prev) => ({
                      ...prev,
                      roles: [{ role: event.target.value as UserRole }]
                    }))
                  }
                >
                  {roleOptions.map((role) => (
                    <MenuItem key={role} value={role}>
                      {role}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Stack direction="row" alignItems="center" spacing={1}>
                <Switch
                  checked={sendEmailEnabled}
                  onChange={(event) =>
                    setInviteForm((prev) => ({
                      ...prev,
                      sendInviteEmail: event.target.checked
                    }))
                  }
                />
                <Typography variant="body2">Send email</Typography>
              </Stack>
              <Button
                type="submit"
                variant="contained"
                startIcon={<AddIcon />}
                disabled={inviteUserMutation.isPending}
              >
                {inviteButtonLabel}
              </Button>
            </Stack>
          </CardContent>
        </SectionCard>
      </Box>

      <SectionCard
        title="Directory"
        subtitle="Live projection of the user_directory read model."
      >
        <CardContent>
          <Box sx={{ height: 520, width: "100%" }}>
            <DataGrid
              disableColumnMenu
              rows={usersResult?.items ?? []}
              getRowId={(row) => row.id}
              columns={columns}
              paginationMode="server"
              rowCount={usersResult?.totalItems ?? 0}
              paginationModel={paginationModel}
              onPaginationModelChange={(model) => setPaginationModel(model)}
              loading={tableLoading}
              density="comfortable"
              slots={{
                noRowsOverlay: DataGridNoRowsOverlay
              }}
            />
          </Box>
        </CardContent>
      </SectionCard>

      <GrantRoleDialog
        dialogState={grantDialog}
        onClose={handleGrantDialogClose}
        isSubmitting={grantDialogSubmitting}
        errorMessage={grantDialogError}
        onSubmit={(role, customerId) => {
          if (!grantDialog.user) {
            return;
          }
          grantRoleMutation.mutate({
            userId: grantDialog.user.id,
            payload: {
              role,
              customerId: customerId || undefined
            }
          });
        }}
      />
    </Stack>
  );
};

interface GrantRoleDialogProps {
  dialogState: {
    user: UserDto | null;
    role: UserRole;
    customerId: string;
  };
  isSubmitting: boolean;
  errorMessage?: string;
  onClose: () => void;
  onSubmit: (role: UserRole, customerId: string) => void;
}

const GrantRoleDialog = ({
                           dialogState,
                           onClose,
                           onSubmit,
                           isSubmitting,
                           errorMessage
                         }: GrantRoleDialogProps) => {
  const [role, setRole] = useState<UserRole>(dialogState.role);
  const [customerId, setCustomerId] = useState(dialogState.customerId);

  useEffect(() => {
    setRole(dialogState.role);
    setCustomerId(dialogState.customerId);
  }, [dialogState.role, dialogState.customerId]);

  const handleSubmit = () => {
    onSubmit(role, customerId);
  };

  const targetEmail = dialogState.user?.email ?? "";
  const grantButtonLabel = isSubmitting ? "Saving..." : "Grant";

  const errorAlert = errorMessage ? (
    <Alert severity="error" sx={{ mb: 2 }}>
      {errorMessage}
    </Alert>
  ) : null;

  return (
    <Dialog
      open={Boolean(dialogState.user)}
      onClose={onClose}
      maxWidth="sm"
      fullWidth
    >
      <DialogTitle>Grant role</DialogTitle>
      <DialogContent>
        {errorAlert}
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Typography variant="body2" color="text.secondary">
            {targetEmail}
          </Typography>
          <FormControl>
            <InputLabel id="grant-role-select">Role</InputLabel>
            <Select
              labelId="grant-role-select"
              label="Role"
              value={role}
              onChange={(event) => setRole(event.target.value as UserRole)}
            >
              {roleOptions.map((roleValue) => (
                <MenuItem key={roleValue} value={roleValue}>
                  {roleValue}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            label="Customer ID (optional)"
            value={customerId}
            onChange={(event) => setCustomerId(event.target.value)}
            helperText="Provide when granting CustomerAdmin roles scoped to a tenant."
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          onClick={handleSubmit}
          variant="contained"
          disabled={isSubmitting}
        >
          {grantButtonLabel}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default UsersPage;
