import React, { ReactNode } from "react";

import { Card, CardHeader, CardProps, Divider } from "@mui/material";

interface SectionCardProps extends CardProps {
  title: string;
  subtitle?: string;
  action?: ReactNode;
  children: ReactNode;
}

const SectionCard = ({
  title,
  subtitle,
  action,
  children,
  ...cardProps
}: SectionCardProps) => (
  <Card {...cardProps}>
    <CardHeader title={title} subheader={subtitle} action={action} />
    <Divider />
    {children}
  </Card>
);

export default SectionCard;
