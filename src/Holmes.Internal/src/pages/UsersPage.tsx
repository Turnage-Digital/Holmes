import React, { FormEvent, useState } from "react";

import AddIcon from "@mui/icons-material/Add";
import {
  Alert,
  Button,
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
  TextField,
} from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";

import type {
  InviteUserRequest,
  RoleAssignmentDto,
  UserDto,
  UserRole,
} from "@/types/api";

import { PageHeader } from "@/components/layout";
import {
  DataGridNoRowsOverlay,
  OptionalRelativeTimeCell,
  RelativeTimeCell,
  StatusBadge,
} from "@/components/patterns";
import { useInviteUser, useUsers } from "@/hooks/api";
import { getErrorMessage } from "@/utils/errorMessage";

const roleOptions: UserRole[] = ["Admin", "Operations"];

const initialFormState: InviteUserRequest = {
  email: "",
  displayName: "",
  sendInviteEmail: true,
  roles: [{ role: "Operations" }],
};

const formatRoleLabel = (assignment: RoleAssignmentDto) =>
  assignment.customerId
    ? `${assignment.role} (${assignment.customerId.slice(0, 8)}…)`
    : assignment.role;

const UsersPage = () => {
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 25,
  });
  const [dialogOpen, setDialogOpen] = useState(false);
  const [formState, setFormState] =
    useState<InviteUserRequest>(initialFormState);
  const [successMessage, setSuccessMessage] = useState<string>();
  const [clientError, setClientError] = useState<string>();

  const {
    data: usersData,
    isLoading,
    error,
  } = useUsers(paginationModel.page + 1, paginationModel.pageSize);

  const inviteUserMutation = useInviteUser();

  const handleOpenDialog = () => {
    setDialogOpen(true);
    setClientError(undefined);
    setSuccessMessage(undefined);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setFormState(initialFormState);
    setClientError(undefined);
  };

  const handleInviteUser = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setClientError(undefined);
    setSuccessMessage(undefined);

    if (!formState.email.trim()) {
      setClientError("Email is required.");
      return;
    }

    if (!formState.email.includes("@")) {
      setClientError("Please enter a valid email address.");
      return;
    }

    try {
      const response = await inviteUserMutation.mutateAsync(formState);
      const message = import.meta.env.DEV && response.confirmationLink
        ? `Invitation sent to ${formState.email}. Confirmation link: ${response.confirmationLink}`
        : `Invitation sent to ${formState.email}.`;
      setSuccessMessage(message);
      handleCloseDialog();
    } catch (err) {
      setClientError(getErrorMessage(err));
    }
  };

  const columns: GridColDef<UserDto>[] = [
    {
      field: "email",
      headerName: "Email",
      width: 250,
    },
    {
      field: "status",
      headerName: "Status",
      width: 120,
      renderCell: (params) => <StatusBadge type="user" status={params.value} />,
    },
    {
      field: "displayName",
      headerName: "Name",
      width: 180,
    },
    {
      field: "roleAssignments",
      headerName: "Roles",
      width: 250,
      renderCell: (params) => {
        const roles = params.value ?? [];
        if (roles.length === 0) return "—";
        return (
          <Stack
            direction="row"
            spacing={0.5}
            alignItems="center"
            sx={{ height: "100%" }}
          >
            {roles.slice(0, 3).map((role: RoleAssignmentDto) => (
              <Chip
                key={role.id}
                label={formatRoleLabel(role)}
                size="small"
                variant="outlined"
              />
            ))}
            {roles.length > 3 && (
              <Chip label={`+${roles.length - 3}`} size="small" />
            )}
          </Stack>
        );
      },
    },
    {
      field: "lastSeenAt",
      headerName: "Last Seen",
      width: 160,
      renderCell: (params) => (
        <OptionalRelativeTimeCell timestamp={params.value} />
      ),
    },
    {
      field: "createdAt",
      headerName: "Created",
      width: 160,
      renderCell: (params) => <RelativeTimeCell timestamp={params.value} />,
    },
  ];

  return (
    <>
      <PageHeader
        title="Users"
        subtitle="Manage operators and administrators"
        action={
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleOpenDialog}
          >
            Invite User
          </Button>
        }
      />

      {successMessage && (
        <Alert
          severity="success"
          onClose={() => setSuccessMessage(undefined)}
          sx={{ mb: 2 }}
        >
          {successMessage}
        </Alert>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load users. Please try again.
        </Alert>
      )}

      <DataGrid
        rows={usersData?.items ?? []}
        columns={columns}
        getRowId={(row) => row.id}
        loading={isLoading}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 25, 50]}
        paginationMode="server"
        rowCount={usersData?.totalItems ?? 0}
        slots={{
          noRowsOverlay: () => (
            <DataGridNoRowsOverlay message="No users yet." />
          ),
        }}
        sx={{ minHeight: 400 }}
        disableRowSelectionOnClick
      />

      {/* Invite User Dialog */}
      <Dialog
        open={dialogOpen}
        onClose={handleCloseDialog}
        maxWidth="sm"
        fullWidth
      >
        <form onSubmit={handleInviteUser}>
          <DialogTitle>Invite User</DialogTitle>
          <DialogContent>
            <Stack spacing={3} sx={{ mt: 1 }}>
              {clientError && <Alert severity="error">{clientError}</Alert>}

              <TextField
                label="Email"
                type="email"
                value={formState.email}
                onChange={(e) =>
                  setFormState((prev) => ({ ...prev, email: e.target.value }))
                }
                required
                fullWidth
              />

              <TextField
                label="Display Name"
                value={formState.displayName}
                onChange={(e) =>
                  setFormState((prev) => ({
                    ...prev,
                    displayName: e.target.value,
                  }))
                }
                fullWidth
                helperText="Optional. The user can set this themselves later."
              />

              <FormControl fullWidth>
                <InputLabel id="role-select-label">Role</InputLabel>
                <Select
                  labelId="role-select-label"
                  value={formState.roles[0]?.role ?? "Operations"}
                  label="Role"
                  onChange={(e) =>
                    setFormState((prev) => ({
                      ...prev,
                      roles: [{ role: e.target.value as UserRole }],
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
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={handleCloseDialog}>Cancel</Button>
            <Button
              type="submit"
              variant="contained"
              disabled={inviteUserMutation.isPending}
            >
              {inviteUserMutation.isPending && <>Sending…</>}
              {!inviteUserMutation.isPending && <>Send Invitation</>}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </>
  );
};

export default UsersPage;
