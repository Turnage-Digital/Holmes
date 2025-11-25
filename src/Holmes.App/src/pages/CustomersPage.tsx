import React, { FormEvent, useState } from "react";

import AddBusinessIcon from "@mui/icons-material/AddBusiness";
import RefreshIcon from "@mui/icons-material/Refresh";
import {
  Alert,
  Box,
  Button,
  CardContent,
  Stack,
  TextField,
} from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";

import { PageHeader } from "@/components/layout";
import {
  DataGridNoRowsOverlay,
  SectionCard,
  SlaBadge,
} from "@/components/patterns";
import { apiFetch, toQueryString } from "@/lib/api";
import {
  CreateCustomerRequest,
  CustomerDto,
  PaginatedResult,
} from "@/types/api";
import { getErrorMessage } from "@/utils/errorMessage";

const fetchCustomersPage = ({
  page,
  pageSize,
}: {
  page: number;
  pageSize: number;
}) =>
  apiFetch<PaginatedResult<CustomerDto>>(
    `/customers${toQueryString({
      page,
      pageSize,
    })}`,
  );

const createCustomer = (payload: CreateCustomerRequest) =>
  apiFetch<CustomerDto>("/customers", {
    method: "POST",
    body: payload,
  });

const CustomersPage = () => {
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 25,
  });
  const [formState, setFormState] = useState({
    name: "",
    policySnapshotId: "",
    billingEmail: "",
    contactName: "",
    contactEmail: "",
  });
  const [successMessage, setSuccessMessage] = useState<string>();
  const [clientError, setClientError] = useState<string>();

  const queryClient = useQueryClient();

  const customersQuery = useQuery({
    queryKey: ["customers", paginationModel.page, paginationModel.pageSize],
    queryFn: () =>
      fetchCustomersPage({
        page: paginationModel.page + 1,
        pageSize: paginationModel.pageSize,
      }),
    placeholderData: keepPreviousData,
  });

  const createCustomerMutation = useMutation({
    mutationFn: createCustomer,
    onMutate: () => {
      setClientError(undefined);
      setSuccessMessage(undefined);
    },
    onSuccess: async (_, variables) => {
      setSuccessMessage(`Customer ${variables.name} created`);
      setFormState({
        name: "",
        policySnapshotId: "",
        billingEmail: "",
        contactName: "",
        contactEmail: "",
      });
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    },
    onError: (err) => setClientError(getErrorMessage(err)),
  });

  const handleCreateCustomer = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const payload: CreateCustomerRequest = {
      name: formState.name,
      policySnapshotId: formState.policySnapshotId,
      billingEmail: formState.billingEmail || undefined,
    };

    if (formState.contactName && formState.contactEmail) {
      payload.contacts = [
        {
          name: formState.contactName,
          email: formState.contactEmail,
        },
      ];
    }

    createCustomerMutation.mutate(payload);
  };

  const columns = React.useMemo<GridColDef<CustomerDto>[]>(
    () => [
      { field: "name", headerName: "Name", flex: 1, minWidth: 200 },
      { field: "tenantId", headerName: "Tenant", width: 200 },
      { field: "status", headerName: "Status", width: 150 },
      {
        field: "policySnapshotId",
        headerName: "Policy snapshot",
        flex: 1,
        minWidth: 200,
      },
      {
        field: "contacts",
        headerName: "Contacts",
        flex: 1.2,
        minWidth: 220,
        sortable: false,
        renderCell: (params) => {
          const row = params.row as CustomerDto;
          return row.contacts.map((contact) => contact.email).join(", ");
        },
      },
    ],
    [],
  );

  const isRefreshing = customersQuery.isFetching;
  const isCreatingCustomer = createCustomerMutation.isPending;
  const createButtonLabel = isCreatingCustomer ? "Creating..." : "Create";
  const tableLoading = customersQuery.isFetching;
  const fetchErrorMessage = customersQuery.error
    ? getErrorMessage(customersQuery.error)
    : undefined;
  const combinedError = clientError ?? fetchErrorMessage;
  const customersResult = customersQuery.data;

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Customers"
        subtitle="CRA clients, policy snapshots, and contact rosters."
        meta={<SlaBadge status="at_risk" deadlineLabel="Billing lock in 6h" />}
        actions={
          <Button
            startIcon={<RefreshIcon />}
            disabled={isRefreshing}
            onClick={() => customersQuery.refetch()}
          >
            Refresh
          </Button>
        }
      />

      {combinedError && <Alert severity="error">{combinedError}</Alert>}
      {successMessage && <Alert severity="success">{successMessage}</Alert>}

      <Box component="form" onSubmit={handleCreateCustomer}>
        <SectionCard
          title="Create customer"
          subtitle="Bind policy snapshots and optional billing contacts."
        >
          <CardContent>
            <Stack spacing={2}>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                <TextField
                  required
                  label="Customer name"
                  value={formState.name}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      name: event.target.value,
                    }))
                  }
                  fullWidth
                />
                <TextField
                  required
                  label="Policy snapshot ID"
                  value={formState.policySnapshotId}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      policySnapshotId: event.target.value,
                    }))
                  }
                  fullWidth
                />
                <TextField
                  label="Billing email"
                  type="email"
                  value={formState.billingEmail}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      billingEmail: event.target.value,
                    }))
                  }
                  fullWidth
                />
              </Stack>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                <TextField
                  label="Primary contact name"
                  value={formState.contactName}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      contactName: event.target.value,
                    }))
                  }
                  fullWidth
                />
                <TextField
                  label="Primary contact email"
                  type="email"
                  value={formState.contactEmail}
                  onChange={(event) =>
                    setFormState((prev) => ({
                      ...prev,
                      contactEmail: event.target.value,
                    }))
                  }
                  fullWidth
                />
                <Button
                  type="submit"
                  variant="contained"
                  startIcon={<AddBusinessIcon />}
                  sx={{
                    alignSelf: { xs: "stretch", md: "flex-end" },
                    minHeight: 56,
                  }}
                  disabled={isCreatingCustomer}
                >
                  {createButtonLabel}
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </SectionCard>
      </Box>

      <SectionCard
        title="Registry"
        subtitle="Customers tied to the current tenant."
      >
        <CardContent>
          <Box sx={{ height: 520, width: "100%" }}>
            <DataGrid
              disableColumnMenu
              rows={customersResult?.items ?? []}
              getRowId={(row) => row.id}
              columns={columns}
              paginationModel={paginationModel}
              onPaginationModelChange={(model) => setPaginationModel(model)}
              rowCount={customersResult?.totalItems ?? 0}
              paginationMode="server"
              loading={tableLoading}
              slots={{
                noRowsOverlay: () => (
                  <DataGridNoRowsOverlay message="No customers found yet." />
                ),
              }}
            />
          </Box>
        </CardContent>
      </SectionCard>
    </Stack>
  );
};

export default CustomersPage;
