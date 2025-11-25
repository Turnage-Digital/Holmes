import React, { ReactNode } from "react";

import { Stack, Typography } from "@mui/material";

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  meta?: ReactNode;
}

const PageHeader = ({ title, subtitle, actions, meta }: PageHeaderProps) => (
  <Stack
    direction={{ xs: "column", md: "row" }}
    spacing={2}
    alignItems={{ xs: "flex-start", md: "center" }}
    justifyContent="space-between"
  >
    <Stack spacing={0.5}>
      <Typography variant="h4" component="h1">
        {title}
      </Typography>
      {subtitle && (
        <Typography variant="subtitle1" component="p">
          {subtitle}
        </Typography>
      )}
      {meta}
    </Stack>

    {actions}
  </Stack>
);

export default PageHeader;
