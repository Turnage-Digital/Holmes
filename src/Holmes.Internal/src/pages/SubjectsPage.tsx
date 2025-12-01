import React, { useState } from "react";

import { Alert } from "@mui/material";
import { DataGrid, GridColDef } from "@mui/x-data-grid";

import type { SubjectListItemDto } from "@/types/api";

import { PageHeader } from "@/components/layout";
import {
  DataGridNoRowsOverlay,
  EmptyableCell,
  MonospaceIdCell,
  RelativeTimeCell,
  StatusBadge,
} from "@/components/patterns";
import { useSubjects } from "@/hooks/api";

const SubjectsPage = () => {
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 25,
  });

  const {
    data: subjectsData,
    isLoading,
    error,
  } = useSubjects(paginationModel.page + 1, paginationModel.pageSize);

  const columns: GridColDef<SubjectListItemDto>[] = [
    {
      field: "id",
      headerName: "ID",
      width: 140,
      renderCell: (params) => <MonospaceIdCell id={params.value} />,
    },
    {
      field: "status",
      headerName: "Status",
      width: 120,
      renderCell: (params) => (
        <StatusBadge type="subject" status={params.value} />
      ),
    },
    {
      field: "firstName",
      headerName: "First Name",
      width: 150,
      renderCell: (params) => <EmptyableCell value={params.value} />,
    },
    {
      field: "lastName",
      headerName: "Last Name",
      width: 150,
      renderCell: (params) => <EmptyableCell value={params.value} />,
    },
    {
      field: "email",
      headerName: "Email",
      width: 220,
      renderCell: (params) => <EmptyableCell value={params.value} />,
    },
    {
      field: "birthDate",
      headerName: "DOB",
      width: 120,
      renderCell: (params) => <EmptyableCell value={params.value} />,
    },
    {
      field: "aliases",
      headerName: "Aliases",
      width: 80,
      renderCell: (params) => params.value?.length ?? 0,
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
        title="Subjects"
        subtitle="Registry of individuals being screened"
      />

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load subjects. Please try again.
        </Alert>
      )}

      <DataGrid
        rows={subjectsData?.items ?? []}
        columns={columns}
        getRowId={(row) => row.id}
        loading={isLoading}
        paginationModel={paginationModel}
        onPaginationModelChange={setPaginationModel}
        pageSizeOptions={[10, 25, 50]}
        paginationMode="server"
        rowCount={subjectsData?.totalItems ?? 0}
        slots={{
          noRowsOverlay: () => (
            <DataGridNoRowsOverlay message="No subjects yet. Subjects are created when orders are placed." />
          ),
        }}
        sx={{ minHeight: 400 }}
        disableRowSelectionOnClick
      />
    </>
  );
};

export default SubjectsPage;
