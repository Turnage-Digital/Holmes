import React from "react";

import { Tab, Tabs } from "@mui/material";
import { Link, useLocation } from "react-router-dom";

export interface PrimaryNavItem {
  label: string;
  path: string;
  description?: string;
}

interface PrimaryNavProps {
  items: PrimaryNavItem[];
}

const PrimaryNav = ({ items }: PrimaryNavProps) => {
  const location = useLocation();
  const active =
    items.find((item) =>
      location.pathname === "/"
        ? item.path === "/"
        : location.pathname.startsWith(item.path),
    )?.path ?? items[0]?.path;

  return (
    <Tabs
      value={active}
      textColor="inherit"
      indicatorColor="secondary"
      variant="scrollable"
      scrollButtons="auto"
    >
      {items.map((item) => (
        <Tab
          key={item.path}
          value={item.path}
          label={item.label}
          component={Link}
          to={item.path}
          sx={{ minHeight: 48 }}
        />
      ))}
    </Tabs>
  );
};

export default PrimaryNav;
