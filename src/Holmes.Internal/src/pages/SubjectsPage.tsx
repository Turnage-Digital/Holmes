import React, { FormEvent, useState } from "react";

import { apiFetch, toQueryString } from "@holmes/ui-core";
import MergeIcon from "@mui/icons-material/Merge";
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
import { MergeSubjectsRequest, PaginatedResult, SubjectDto } from "@/types/api";
import { getErrorMessage } from "@/utils/errorMessage";

const fetchSubjectsPage = ({
  page,
  pageSize,
}: {
  page: number;
  pageSize: number;
}) =>
  apiFetch<PaginatedResult<SubjectDto>>(
    `/subjects${toQueryString({
      page,
      pageSize,
    })}`,
  );

const mergeSubjects = (payload: MergeSubjectsRequest) =>
  apiFetch<SubjectDto>("/subjects/merge", {
    method: "POST",
    body: payload,
  });

const SubjectsPage = () => {
  const [paginationModel, setPaginationModel] = useState({
    page: 0,
    pageSize: 25,
  });
  const [mergeForm, setMergeForm] = useState<MergeSubjectsRequest>({
    winnerSubjectId: "",
    mergedSubjectId: "",
    reason: "",
  });
  const [successMessage, setSuccessMessage] = useState<string>();
  const [clientError, setClientError] = useState<string>();

  const queryClient = useQueryClient();

  const subjectsQuery = useQuery({
    queryKey: ["subjects", paginationModel.page, paginationModel.pageSize],
    queryFn: () =>
      fetchSubjectsPage({
        page: paginationModel.page + 1,
        pageSize: paginationModel.pageSize,
      }),
    placeholderData: keepPreviousData,
  });

  const mergeSubjectsMutation = useMutation({
    mutationFn: mergeSubjects,
    onMutate: () => {
      setClientError(undefined);
      setSuccessMessage(undefined);
    },
    onSuccess: async () => {
      setSuccessMessage("Subjects merged successfully");
      setMergeForm({
        winnerSubjectId: "",
        mergedSubjectId: "",
        reason: "",
      });
      await queryClient.invalidateQueries({ queryKey: ["subjects"] });
    },
    onError: (err) => setClientError(getErrorMessage(err)),
  });

  const handleMerge = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    mergeSubjectsMutation.mutate(mergeForm);
  };

  const columns = React.useMemo<GridColDef<SubjectDto>[]>(
    () => [
      { field: "id", headerName: "Subject ID", width: 220 },
      {
        field: "name",
        headerName: "Name",
        flex: 1.2,
        minWidth: 200,
        renderCell: (params) => {
          const row = params.row as SubjectDto;
          return [row.firstName, row.middleName, row.lastName]
            .filter(Boolean)
            .join(" ");
        },
      },
      { field: "status", headerName: "Status", width: 140 },
      { field: "birthDate", headerName: "DOB", width: 140 },
      {
        field: "aliases",
        headerName: "Aliases",
        flex: 1,
        minWidth: 220,
        renderCell: (params) => {
          const row = params.row as SubjectDto;
          return row.aliases.length;
        },
      },
      {
        field: "mergeParentId",
        headerName: "Merged Into",
        flex: 1,
        minWidth: 220,
      },
    ],
    [],
  );

  const isRefreshing = subjectsQuery.isFetching;
  const isMerging = mergeSubjectsMutation.isPending;
  const mergeButtonLabel = isMerging ? "Merging..." : "Merge";
  const tableLoading = subjectsQuery.isFetching;
  const fetchErrorMessage = subjectsQuery.error
    ? getErrorMessage(subjectsQuery.error)
    : undefined;
  const combinedError = clientError ?? fetchErrorMessage;
  const subjectsResult = subjectsQuery.data;

  return (
    <Stack spacing={3}>
      <PageHeader
        title="Subjects"
        subtitle="Registry visibility, merges, and lineage checks."
        meta={<SlaBadge status="breached" deadlineLabel="Investigate dedupe" />}
        actions={
          <Button
            startIcon={<RefreshIcon />}
            disabled={isRefreshing}
            onClick={() => subjectsQuery.refetch()}
          >
            Refresh
          </Button>
        }
      />

      {combinedError && <Alert severity="error">{combinedError}</Alert>}
      {successMessage && <Alert severity="success">{successMessage}</Alert>}

      <Box component="form" onSubmit={handleMerge}>
        <SectionCard
          title="Merge subjects"
          subtitle="Prove dedupe + lineage flows."
        >
          <CardContent>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <TextField
                required
                label="Winner subject ID"
                value={mergeForm.winnerSubjectId}
                onChange={(event) =>
                  setMergeForm((prev) => ({
                    ...prev,
                    winnerSubjectId: event.target.value,
                  }))
                }
                fullWidth
              />
              <TextField
                required
                label="Merged subject ID"
                value={mergeForm.mergedSubjectId}
                onChange={(event) =>
                  setMergeForm((prev) => ({
                    ...prev,
                    mergedSubjectId: event.target.value,
                  }))
                }
                fullWidth
              />
              <TextField
                label="Reason"
                value={mergeForm.reason ?? ""}
                onChange={(event) =>
                  setMergeForm((prev) => ({
                    ...prev,
                    reason: event.target.value,
                  }))
                }
                fullWidth
              />
              <Button
                type="submit"
                variant="contained"
                startIcon={<MergeIcon />}
                disabled={isMerging}
                sx={{ minHeight: 56 }}
              >
                {mergeButtonLabel}
              </Button>
            </Stack>
          </CardContent>
        </SectionCard>
      </Box>

      <SectionCard title="Registry" subtitle="Subject registry projection.">
        <CardContent>
          <Box sx={{ height: 520, width: "100%" }}>
            <DataGrid
              disableColumnMenu
              rows={subjectsResult?.items ?? []}
              getRowId={(row) => row.id}
              columns={columns}
              paginationModel={paginationModel}
              onPaginationModelChange={(model) => setPaginationModel(model)}
              rowCount={subjectsResult?.totalItems ?? 0}
              paginationMode="server"
              loading={tableLoading}
              slots={{
                noRowsOverlay: DataGridNoRowsOverlay,
              }}
            />
          </Box>
        </CardContent>
      </SectionCard>
    </Stack>
  );
};

export default SubjectsPage;
