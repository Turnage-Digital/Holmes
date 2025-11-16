import React, { ReactNode } from "react";

import InboxIcon from "@mui/icons-material/Inbox";
import { Box, Button, Stack, Typography } from "@mui/material";

interface EmptyStateProps {
  title: string;
  description?: string;
  icon?: ReactNode;
  actionLabel?: string;
  onActionClick?: () => void;
}

const EmptyState = ({
  title,
  description,
  icon = <InboxIcon fontSize="large" color="disabled" />,
  actionLabel,
  onActionClick,
}: EmptyStateProps) => (
  <Stack
    spacing={1}
    sx={{
      alignItems: "center",
      justifyContent: "center",
      textAlign: "center",
      py: 6,
      px: 2,
      color: "text.secondary",
    }}
  >
    <Box>{icon}</Box>
    <Typography variant="subtitle1" component="p" color="text.primary">
      {title}
    </Typography>
    {description && (
      <Typography variant="body2" component="p">
        {description}
      </Typography>
    )}
    {actionLabel && onActionClick && (
      <Button variant="outlined" onClick={onActionClick}>
        {actionLabel}
      </Button>
    )}
  </Stack>
);

export default EmptyState;
