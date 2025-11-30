import React from "react";

import {
  AppBar,
  Box,
  CircularProgress,
  Container,
  Toolbar,
  Typography,
} from "@mui/material";
import { Outlet } from "react-router-dom";

import PrimaryNav, { PrimaryNavItem } from "@/components/navigation/PrimaryNav";
import { useCurrentUser, useIsAdmin } from "@/hooks/api";

const baseNavItems: PrimaryNavItem[] = [
  { label: "Dashboard", path: "/", description: "Overview and quick actions" },
  {
    label: "Orders",
    path: "/orders",
    description: "Intake + workflow monitoring",
  },
  {
    label: "Subjects",
    path: "/subjects",
    description: "Registry and lineage",
  },
];

const adminNavItems: PrimaryNavItem[] = [
  {
    label: "Customers",
    path: "/customers",
    description: "CRA clients and contacts",
  },
  { label: "Users", path: "/users", description: "Invite + manage operators" },
];

const AppLayout = () => {
  const { data: currentUser, isLoading } = useCurrentUser();
  const isAdmin = useIsAdmin();

  const navItems = isAdmin ? [...baseNavItems, ...adminNavItems] : baseNavItems;

  if (isLoading) {
    return (
      <Box
        sx={{
          minHeight: "100vh",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          bgcolor: "background.default",
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar position="static" color="primary" elevation={0}>
        <Toolbar sx={{ px: 4 }}>
          <Typography
            component="div"
            variant="h5"
            sx={{ fontWeight: 700, flexGrow: 1 }}
          >
            Holmes
          </Typography>
          {currentUser && (
            <Typography variant="body2" sx={{ opacity: 0.8 }}>
              {currentUser.displayName ?? currentUser.email}
            </Typography>
          )}
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
          gap: 3,
        }}
      >
        <Outlet />
      </Container>
    </Box>
  );
};

export default AppLayout;
