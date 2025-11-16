import React from "react";

import { CardContent, Stack, Typography } from "@mui/material";

import SectionCard from "./SectionCard";

export interface AuditMetric {
  label: string;
  value: string;
  helperText?: string;
}

interface AuditPanelProps {
  title?: string;
  metrics: AuditMetric[];
}

const AuditPanel = ({ title = "Snapshot", metrics }: AuditPanelProps) => (
  <SectionCard title={title}>
    <CardContent sx={{ pt: 1 }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={3}
        flexWrap="wrap"
        useFlexGap
      >
        {metrics.map((metric) => (
          <Stack key={metric.label} spacing={0.5} minWidth={160}>
            <Typography variant="overline" color="text.secondary">
              {metric.label}
            </Typography>
            <Typography variant="h5" component="div">
              {metric.value}
            </Typography>
            {metric.helperText && (
              <Typography variant="caption" color="text.secondary">
                {metric.helperText}
              </Typography>
            )}
          </Stack>
        ))}
      </Stack>
    </CardContent>
  </SectionCard>
);

export default AuditPanel;
