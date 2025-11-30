import React from "react";

import { AppBar, Box, Container, Toolbar, Typography } from "@mui/material";
import { Outlet } from "react-router-dom";

import PrimaryNav, { PrimaryNavItem } from "@/components/navigation/PrimaryNav";

const navItems: PrimaryNavItem[] = [
  { label: "Users", path: "/users", description: "Invite + manage operators" },
  {
    label: "Customers",
    path: "/customers",
    description: "CRA clients and contacts"
  },
  {
    label: "Subjects",
    path: "/subjects",
    description: "Registry, merges, and lineage"
  },
  {
    label: "Orders",
    path: "/orders",
    description: "Intake + workflow monitoring"
  }
];

const AppLayout = () => {
  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar position="static" color="primary" elevation={0}>
        <Toolbar sx={{ px: 4 }}>
          <Typography component="div" variant="h5" sx={{ fontWeight: 700 }}>
            Holmes Admin
          </Typography>
        </Toolbar>
        <PrimaryNav items={navItems} />
      </AppBar>

      <Container
        component="main"
        maxWidth="xl"
        sx={{
          py: 4,
          display: "flex",
          flexDirection: "column",
          gap: 3
        }}
      >
        <Outlet />
      </Container>
    </Box>
  );
};

export default AppLayout;
