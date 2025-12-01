import React from "react";

import { Card, CardActionArea, Chip, Stack, Typography } from "@mui/material";

import type { OrderSummaryDto } from "@/types/api";

import {
  CustomerNameCell,
  RelativeTimeCell,
  SubjectNameCell,
} from "@/components/patterns";
import { getOrderStatusColor, getOrderStatusLabel } from "@/lib/status";

interface OrderCardProps {
  order: OrderSummaryDto;
  onClick: (orderId: string) => void;
}

const OrderCard = ({ order, onClick }: OrderCardProps) => {
  const statusColor = getOrderStatusColor(order.status);
  const statusLabel = getOrderStatusLabel(order.status);

  return (
    <Card variant="outlined">
      <CardActionArea onClick={() => onClick(order.orderId)} sx={{ p: 2 }}>
        <Stack spacing={1}>
          {/* Top row: Status + Time */}
          <Stack
            direction="row"
            justifyContent="space-between"
            alignItems="center"
          >
            <Chip
              label={statusLabel}
              color={statusColor}
              size="small"
              variant="outlined"
            />
            <Typography variant="caption" color="text.secondary">
              <RelativeTimeCell timestamp={order.lastUpdatedAt} />
            </Typography>
          </Stack>

          {/* Subject name */}
          <Typography variant="body1" fontWeight={500}>
            <SubjectNameCell subjectId={order.subjectId} />
          </Typography>

          {/* Customer */}
          <Typography variant="body2" color="text.secondary">
            <CustomerNameCell customerId={order.customerId} />
          </Typography>

          {/* Order ID */}
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ fontFamily: "monospace" }}
          >
            {order.orderId.slice(0, 12)}…
          </Typography>

          {/* Reason if present */}
          {order.lastStatusReason && (
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{
                overflow: "hidden",
                textOverflow: "ellipsis",
                whiteSpace: "nowrap",
              }}
            >
              {order.lastStatusReason}
            </Typography>
          )}
        </Stack>
      </CardActionArea>
    </Card>
  );
};

export default OrderCard;
