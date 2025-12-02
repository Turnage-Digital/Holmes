import React, { useState } from "react";

import AssignmentIcon from "@mui/icons-material/Assignment";
import BusinessIcon from "@mui/icons-material/Business";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import DashboardIcon from "@mui/icons-material/Dashboard";
import GroupIcon from "@mui/icons-material/Group";
import LogoutIcon from "@mui/icons-material/Logout";
import MenuIcon from "@mui/icons-material/Menu";
import PeopleIcon from "@mui/icons-material/People";
import {
  Avatar,
  Box,
  CircularProgress,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Tooltip,
  Typography,
  useMediaQuery,
} from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { Link, Outlet, useLocation } from "react-router-dom";

import { useCurrentUser, useIsAdmin } from "@/hooks/api";

const DRAWER_WIDTH = 240;
const DRAWER_COLLAPSED_WIDTH = 72;

interface NavItem {
  label: string;
  path: string;
  icon: React.ReactNode;
}

interface NavSection {
  title: string;
  items: NavItem[];
  adminOnly?: boolean;
}

const navSections: NavSection[] = [
  {
    title: "Work",
    items: [
      { label: "Dashboard", path: "/", icon: <DashboardIcon /> },
      { label: "Orders", path: "/orders", icon: <AssignmentIcon /> },
    ],
  },
  {
    title: "Reference",
    items: [{ label: "Subjects", path: "/subjects", icon: <PeopleIcon /> }],
  },
  {
    title: "Admin",
    adminOnly: true,
    items: [
      { label: "Customers", path: "/customers", icon: <BusinessIcon /> },
      { label: "Users", path: "/users", icon: <GroupIcon /> },
    ],
  },
];

const AppShell = () => {
  const theme = useTheme();
  const location = useLocation();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"));

  const { data: currentUser, isLoading } = useCurrentUser();
  const isAdmin = useIsAdmin();

  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  const drawerWidth = collapsed ? DRAWER_COLLAPSED_WIDTH : DRAWER_WIDTH;

  const isActivePath = (path: string) => {
    if (path === "/") {
      return location.pathname === "/";
    }
    return location.pathname.startsWith(path);
  };

  const handleDrawerToggle = () => {
    if (isMobile) {
      setMobileOpen(!mobileOpen);
    } else {
      setCollapsed(!collapsed);
    }
  };

  const handleNavClick = () => {
    if (isMobile) {
      setMobileOpen(false);
    }
  };

  // Filter sections based on admin status
  const visibleSections = navSections.filter(
    (section) => !section.adminOnly || isAdmin,
  );

  const getUserInitials = () => {
    if (currentUser?.displayName) {
      return currentUser.displayName
        .split(" ")
        .map((n) => n[0])
        .join("")
        .toUpperCase()
        .slice(0, 2);
    }
    if (currentUser?.email) {
      return currentUser.email.slice(0, 2).toUpperCase();
    }
    return "?";
  };
  const userInitials = getUserInitials();

  // Computed values for drawer
  const drawerVariant = isMobile ? "temporary" : "permanent";
  const drawerOpen = isMobile ? mobileOpen : true;
  const currentDrawerWidth = isMobile ? DRAWER_WIDTH : drawerWidth;

  const drawerContent = (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        bgcolor: "primary.main",
        color: "primary.contrastText",
      }}
    >
      {/* Logo/Brand */}
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          justifyContent: collapsed ? "center" : "space-between",
          px: collapsed ? 1 : 2,
          py: 2,
          minHeight: 64,
        }}
      >
        {!collapsed && (
          <Typography variant="h6" sx={{ fontWeight: 700 }}>
            Holmes
          </Typography>
        )}
        {!isMobile && (
          <IconButton
            onClick={handleDrawerToggle}
            sx={{ color: "primary.contrastText" }}
            size="small"
          >
            {collapsed && <ChevronRightIcon />}
            {!collapsed && <ChevronLeftIcon />}
          </IconButton>
        )}
      </Box>

      <Divider sx={{ borderColor: "rgba(255,255,255,0.12)" }} />

      {/* Navigation Sections */}
      <Box sx={{ flexGrow: 1, overflowY: "auto", overflowX: "hidden", py: 1 }}>
        {visibleSections.map((section, sectionIndex) => (
          <Box key={section.title}>
            {sectionIndex > 0 && (
              <Divider sx={{ my: 1, borderColor: "rgba(255,255,255,0.12)" }} />
            )}
            {!collapsed && (
              <Typography
                variant="overline"
                sx={{
                  px: 2,
                  py: 1,
                  display: "block",
                  color: "rgba(255,255,255,0.6)",
                  fontSize: "0.65rem",
                  letterSpacing: "0.1em",
                }}
              >
                {section.title}
              </Typography>
            )}
            <List disablePadding>
              {section.items.map((item) => {
                const isActive = isActivePath(item.path);
                const button = (
                  <ListItem key={item.path} disablePadding sx={{ px: 1 }}>
                    <ListItemButton
                      component={Link}
                      to={item.path}
                      onClick={handleNavClick}
                      selected={isActive}
                      sx={{
                        borderRadius: 1,
                        minHeight: 44,
                        justifyContent: collapsed ? "center" : "flex-start",
                        px: collapsed ? 1.5 : 2,
                        "&.Mui-selected": {
                          bgcolor: "rgba(255,255,255,0.16)",
                          "&:hover": {
                            bgcolor: "rgba(255,255,255,0.2)",
                          },
                        },
                        "&:hover": {
                          bgcolor: "rgba(255,255,255,0.08)",
                        },
                      }}
                    >
                      <ListItemIcon
                        sx={{
                          color: "primary.contrastText",
                          minWidth: collapsed ? 0 : 40,
                          opacity: isActive ? 1 : 0.7,
                        }}
                      >
                        {item.icon}
                      </ListItemIcon>
                      {!collapsed && (
                        <ListItemText
                          primary={item.label}
                          primaryTypographyProps={{
                            fontSize: "0.875rem",
                            fontWeight: isActive ? 600 : 400,
                          }}
                        />
                      )}
                    </ListItemButton>
                  </ListItem>
                );

                if (collapsed) {
                  return (
                    <Tooltip
                      key={item.path}
                      title={item.label}
                      placement="right"
                    >
                      {button}
                    </Tooltip>
                  );
                }
                return button;
              })}
            </List>
          </Box>
        ))}
      </Box>

      <Divider sx={{ borderColor: "rgba(255,255,255,0.12)" }} />

      {/* User Profile */}
      <Box sx={{ p: collapsed ? 1 : 2 }}>
        {collapsed && (
          <Tooltip title={currentUser?.email ?? "User"} placement="right">
            <Avatar
              sx={{
                width: 40,
                height: 40,
                bgcolor: "primary.light",
                mx: "auto",
                fontSize: "0.875rem",
              }}
            >
              {userInitials}
            </Avatar>
          </Tooltip>
        )}
        {!collapsed && (
          <Box sx={{ display: "flex", alignItems: "center", gap: 1.5 }}>
            <Avatar
              sx={{
                width: 36,
                height: 36,
                bgcolor: "primary.light",
                fontSize: "0.75rem",
              }}
            >
              {userInitials}
            </Avatar>
            <Box sx={{ flexGrow: 1, minWidth: 0 }}>
              <Typography
                variant="body2"
                sx={{
                  fontWeight: 500,
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                }}
              >
                {currentUser?.displayName ?? "User"}
              </Typography>
              <Typography
                variant="caption"
                sx={{
                  color: "rgba(255,255,255,0.6)",
                  overflow: "hidden",
                  textOverflow: "ellipsis",
                  whiteSpace: "nowrap",
                  display: "block",
                }}
              >
                {currentUser?.email}
              </Typography>
            </Box>
            <Tooltip title="Sign out">
              <IconButton
                component="a"
                href="/bff/logout"
                size="small"
                sx={{ color: "rgba(255,255,255,0.6)" }}
              >
                <LogoutIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Box>
        )}
      </Box>
    </Box>
  );

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
    <Box sx={{ display: "flex", minHeight: "100vh" }}>
      {/* Mobile App Bar */}
      {isMobile && (
        <Box
          sx={{
            position: "fixed",
            top: 0,
            left: 0,
            right: 0,
            height: 56,
            bgcolor: "primary.main",
            display: "flex",
            alignItems: "center",
            px: 2,
            zIndex: theme.zIndex.appBar,
          }}
        >
          <IconButton
            onClick={handleDrawerToggle}
            sx={{ color: "primary.contrastText", mr: 2 }}
          >
            <MenuIcon />
          </IconButton>
          <Typography
            variant="h6"
            sx={{ color: "primary.contrastText", fontWeight: 700 }}
          >
            Holmes
          </Typography>
        </Box>
      )}

      {/* Sidebar Drawer */}
      <Drawer
        variant={drawerVariant}
        open={drawerOpen}
        onClose={handleDrawerToggle}
        sx={{
          width: currentDrawerWidth,
          flexShrink: 0,
          "& .MuiDrawer-paper": {
            width: currentDrawerWidth,
            boxSizing: "border-box",
            borderRight: "none",
            transition: theme.transitions.create("width", {
              easing: theme.transitions.easing.sharp,
              duration: theme.transitions.duration.enteringScreen,
            }),
          },
        }}
      >
        {drawerContent}
      </Drawer>

      {/* Main Content */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          bgcolor: "background.default",
          minHeight: "100vh",
          pt: isMobile ? "56px" : 0,
        }}
      >
        <Box sx={{ p: { xs: 2, sm: 3, md: 4 }, maxWidth: 1600, mx: "auto" }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  );
};

export default AppShell;
