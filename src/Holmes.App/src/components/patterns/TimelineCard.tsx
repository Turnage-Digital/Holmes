import React from "react";

import { Box, CardContent, Stack, Typography } from "@mui/material";

import SectionCard from "./SectionCard";

export interface TimelineItem {
  id: string;
  timestamp: string;
  title: string;
  description?: string;
  meta?: string;
}

interface TimelineCardProps {
  title: string;
  subtitle?: string;
  items: TimelineItem[];
}

const TimelineCard = ({ title, subtitle, items }: TimelineCardProps) => {
  const content =
    items.length === 0 ? (
      <Typography variant="body2" color="text.secondary">
        No activity recorded yet.
      </Typography>
    ) : (
      items.map((item, index) => {
        const meta = item.meta ? ` · ${item.meta}` : "";
        return (
          <Stack
            key={item.id}
            direction="row"
            spacing={2}
            alignItems="flex-start"
          >
            <TimelineDot isLast={index === items.length - 1} />
            <Stack spacing={0.5} flex={1}>
              <Typography variant="subtitle2">{item.title}</Typography>
              {item.description && (
                <Typography variant="body2" color="text.secondary">
                  {item.description}
                </Typography>
              )}
              <Typography variant="caption" color="text.secondary">
                {item.timestamp}
                {meta}
              </Typography>
            </Stack>
          </Stack>
        );
      })
    );

  return (
    <SectionCard title={title} subtitle={subtitle}>
      <CardContent>
        <Stack spacing={3}>{content}</Stack>
      </CardContent>
    </SectionCard>
  );
};

const TimelineDot = ({ isLast }: { isLast: boolean }) => (
  <Box
    sx={{
      position: "relative",
      width: 12,
      height: 12,
      borderRadius: "50%",
      bgcolor: "primary.main",
      mt: 0.5,
      "&::after": {
        content: '""',
        position: "absolute",
        left: "50%",
        top: "100%",
        width: 2,
        height: isLast ? 0 : 40,
        bgcolor: "divider",
        transform: "translateX(-50%)",
      },
    }}
  />
);

export default TimelineCard;
