import React, { FormEvent, useState } from "react";

import AddBusinessIcon from "@mui/icons-material/AddBusiness";
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";

import type { CreateCustomerRequest, CustomerListItemDto } from "@/types/api";

import { PageHeader } from "@/components/layout";
import {
  DataGridNoRowsOverlay,
  MonospaceIdCell,
  RelativeTimeCell,
  StatusBadge,
} from "@/components/patterns";
import { useCreateCustomer, useCustomers } from "@/hooks/api";
import { getErrorMessage } from "@/utils/errorMessage";

const initialFormState: CreateCustomerRequest = {
  name: "",
  policySnapshotId: "",
  billingEmail: "",
  contacts: [],
};

const CustomersPage = () => {
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 25,
  });
  const [dialogOpen, setDialogOpen] = useState(false);
  const [formState, setFormState] =
    useState<CreateCustomerRequest>(initialFormState);
  const [successMessage, setSuccessMessage] = useState<string>();
  const [clientError, setClientError] = useState<string>();

  const {
    data: customersData,
    isLoading,
    error,
  } = useCustomers(paginationModel.page + 1, paginationModel.pageSize);

  const createCustomerMutation = useCreateCustomer();

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

  const handleCreateCustomer = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setClientError(undefined);
    setSuccessMessage(undefined);

    if (!formState.name.trim()) {
      setClientError("Customer name is required.");
      return;
    }

    if (!formState.policySnapshotId.trim()) {
      setClientError("Policy snapshot ID is required.");
      return;
    }

    try {
      await createCustomerMutation.mutateAsync(formState);
      setSuccessMessage(`Customer "${formState.name}" created successfully.`);
      handleCloseDialog();
    } catch (err) {
      setClientError(getErrorMessage(err));
    }
  };

  const columns: GridColDef<CustomerListItemDto>[] = [
    {
      field: "name",
      headerName: "Name",
      width: 250,
    },
    {
      field: "status",
      headerName: "Status",
      width: 120,
      renderCell: (params) => (
        <StatusBadge type="customer" status={params.value} />
      ),
    },
    {
      field: "policySnapshotId",
      headerName: "Policy",
      width: 200,
      renderCell: (params) => <MonospaceIdCell id={params.value} />,
    },
    {
      field: "billingEmail",
      headerName: "Billing Email",
      width: 220,
    },
    {
      field: "contacts",
      headerName: "Contacts",
      width: 100,
      renderCell: (params) => params.value?.length ?? 0,
    },
    {
      field: "createdAt",
      headerName: "Created",
      width: 180,
      renderCell: (params) => <RelativeTimeCell timestamp={params.value} />,
    },
  ];

  return (
    <>
      <PageHeader
        title="Customers"
        subtitle="Manage CRA clients and their configurations"
        action={
          <Button
            variant="contained"
            startIcon={<AddBusinessIcon />}
            onClick={handleOpenDialog}
          >
            Add Customer
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
          Failed to load customers. Please try again.
        </Alert>
      )}

      <DataGrid
        rows={customersData?.items ?? []}
        columns={columns}
        getRowId={(row) => row.id}
        loading={isLoading}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 25, 50]}
        paginationMode="server"
        rowCount={customersData?.totalItems ?? 0}
        slots={{
          noRowsOverlay: () => (
            <DataGridNoRowsOverlay message="No customers yet. Add your first customer to get started." />
          ),
        }}
        sx={{ minHeight: 400 }}
        disableRowSelectionOnClick
      />

      {/* Create Customer Dialog */}
      <Dialog
        open={dialogOpen}
        onClose={handleCloseDialog}
        maxWidth="sm"
        fullWidth
      >
        <form onSubmit={handleCreateCustomer}>
          <DialogTitle>Add Customer</DialogTitle>
          <DialogContent>
            <Stack spacing={3} sx={{ mt: 1 }}>
              {clientError && <Alert severity="error">{clientError}</Alert>}

              <TextField
                label="Customer Name"
                value={formState.name}
                onChange={(e) =>
                  setFormState((prev) => ({ ...prev, name: e.target.value }))
                }
                required
                fullWidth
              />

              <TextField
                label="Policy Snapshot ID"
                value={formState.policySnapshotId}
                onChange={(e) =>
                  setFormState((prev) => ({
                    ...prev,
                    policySnapshotId: e.target.value,
                  }))
                }
                required
                fullWidth
                helperText="The policy configuration that applies to this customer's orders."
              />

              <TextField
                label="Billing Email"
                type="email"
                value={formState.billingEmail}
                onChange={(e) =>
                  setFormState((prev) => ({
                    ...prev,
                    billingEmail: e.target.value,
                  }))
                }
                fullWidth
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={handleCloseDialog}>Cancel</Button>
            <Button
              type="submit"
              variant="contained"
              disabled={createCustomerMutation.isPending}
            >
              {createCustomerMutation.isPending && <>Creating…</>}
              {!createCustomerMutation.isPending && <>Create Customer</>}
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </>
  );
};

export default CustomersPage;
