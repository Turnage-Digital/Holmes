import React, {useEffect, useMemo} from "react";

import {Alert, Box, Button, Stack} from "@mui/material";
import {DataGrid, GridColDef} from "@mui/x-data-grid";
import {useQuery, useQueryClient} from "@tanstack/react-query";
import {formatDistanceToNow} from "date-fns";

import {PageHeader} from "@/components/layout";
import {DataGridNoRowsOverlay, SectionCard} from "@/components/patterns";
import {apiFetch, createEventSource} from "@/lib/api";
import {OrderSummary} from "@/types/api";

const fetchOrderSummary = () => apiFetch<OrderSummary[]>("/orders/summary");

interface OrderChangePayload {
    orderId: string;
    status: string;
    reason?: string;
    changedAt: string;
}

const OrdersPage = () => {
    const queryClient = useQueryClient();
    const ordersQuery = useQuery({
        queryKey: ["orders", "summary"],
        queryFn: fetchOrderSummary,
    });

    useEffect(() => {
        const source = createEventSource("/orders/changes");
        source.onmessage = (event) => {
            const payload: OrderChangePayload = JSON.parse(event.data);
            queryClient.setQueryData<OrderSummary[]>(
                ["orders", "summary"],
                (current) => {
                    if (!current) {
                        return current;
                    }

                    const existing = current.find(
                        (order) => order.orderId === payload.orderId,
                    );

                    if (!existing) {
                        return current;
                    }

                    return current.map((order) =>
                        order.orderId === payload.orderId
                            ? {
                                ...order,
                                status: payload.status as OrderSummary["status"],
                                lastStatusReason: payload.reason,
                                lastUpdatedAt: payload.changedAt,
                            }
                            : order,
                    );
                },
            );
        };

        return () => {
            source.close();
        };
    }, [queryClient]);

    const columns = useMemo<GridColDef<OrderSummary>[]>(
        () => [
            {field: "orderId", headerName: "Order", flex: 1, minWidth: 160},
            {field: "customerId", headerName: "Customer", flex: 1, minWidth: 160},
            {field: "subjectId", headerName: "Subject", flex: 1, minWidth: 160},
            {field: "policySnapshotId", headerName: "Policy", minWidth: 140},
            {
                field: "status",
                headerName: "Status",
                minWidth: 160,
                valueGetter: ({row}) => row.status,
            },
            {
                field: "lastStatusReason",
                headerName: "Reason",
                flex: 1,
                minWidth: 200,
            },
            {
                field: "lastUpdatedAt",
                headerName: "Updated",
                minWidth: 160,
                valueGetter: ({row}) =>
                    formatDistanceToNow(new Date(row.lastUpdatedAt), {
                        addSuffix: true,
                    }),
            },
        ],
        [],
    );

    return (
        <Stack spacing={3}>
            <PageHeader
                title="Orders"
                description="Track order workflow states in real time."
                actions={
                    <Button
                        variant="outlined"
                        onClick={() => ordersQuery.refetch()}
                        disabled={ordersQuery.isFetching}
                    >
                        Refresh
                    </Button>
                }
            />
            {ordersQuery.error ? (
                <Alert severity="error">
                    {(ordersQuery.error as Error).message ??
                        "Unable to load orders."}
                </Alert>
            ) : null}
            <SectionCard title="Order Summary">
                <Box sx={{height: 520, width: "100%"}}>
                    <DataGrid
                        loading={ordersQuery.isLoading}
                        rows={ordersQuery.data ?? []}
                        getRowId={(row) => row.orderId}
                        columns={columns}
                        disableRowSelectionOnClick
                        slots={{
                            noRowsOverlay: DataGridNoRowsOverlay,
                        }}
                    />
                </Box>
            </SectionCard>
        </Stack>
    );
};

export default OrdersPage;
