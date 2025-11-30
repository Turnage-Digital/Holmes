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

  // Find the active tab by matching the pathname
  const active =
    items.find((item) => {
      // Exact match for root
      if (item.path === "/") {
        return location.pathname === "/";
      }
      // For other paths, check if the current path starts with the item path
      // This handles nested routes like /orders/123 matching /orders
      return location.pathname.startsWith(item.path);
    })?.path ?? false;

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
