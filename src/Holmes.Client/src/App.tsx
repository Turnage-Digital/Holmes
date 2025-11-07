import React, { useMemo } from "react";

import {
  AppBar,
  Box,
  Container,
  CssBaseline,
  Tab,
  Tabs,
  Toolbar,
  Typography,
  createTheme,
  ThemeProvider,
} from "@mui/material";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import {
  BrowserRouter,
  Link,
  Navigate,
  Outlet,
  Route,
  Routes,
  useLocation,
} from "react-router-dom";

import AuthBoundary from "./components/AuthBoundary";
import AuthOptionsPage from "./pages/AuthOptionsPage";
import CustomersPage from "./pages/CustomersPage";
import SubjectsPage from "./pages/SubjectsPage";
import UsersPage from "./pages/UsersPage";

const queryClient = new QueryClient();

const AppShell = () => {
  const location = useLocation();

  const tabs = useMemo(
    () => [
      { label: "Users", path: "/users" },
      { label: "Customers", path: "/customers" },
      { label: "Subjects", path: "/subjects" },
    ],
    [],
  );

  const activeTab =
    tabs.find((tab) => location.pathname.startsWith(tab.path))?.path ??
    "/users";

  return (
    <Box sx={{ display: "flex", flexDirection: "column", minHeight: "100vh" }}>
      <AppBar position="static" color="primary">
        <Toolbar>
          <Box sx={{ flexGrow: 1 }}>
            <Typography variant="h6" component="div">
              Holmes Admin
            </Typography>
          </Box>
        </Toolbar>
        <Tabs
          value={activeTab}
          textColor="inherit"
          indicatorColor="secondary"
          variant="scrollable"
          scrollButtons="auto"
        >
          {tabs.map((tab) => (
            <Tab
              key={tab.path}
              value={tab.path}
              label={tab.label}
              component={Link}
              to={tab.path}
              sx={{ minHeight: 48 }}
            />
          ))}
        </Tabs>
      </AppBar>

      <Container maxWidth="xl" sx={{ flexGrow: 1, py: 3 }}>
        <Outlet />
      </Container>
    </Box>
  );
};

const theme = createTheme({
  palette: {
    primary: {
      main: "#1b2e5f",
    },
    secondary: {
      main: "#ff8a4c",
    },
  },
});

const App = () => {
  const devtools = import.meta.env.DEV ? (
    <ReactQueryDevtools initialIsOpen={false} />
  ) : null;

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Routes>
            <Route path="/auth/options" element={<AuthOptionsPage />} />
            <Route element={<AuthBoundary />}>
              <Route path="/" element={<AppShell />}>
                <Route index element={<Navigate to="/users" replace />} />
                <Route path="users" element={<UsersPage />} />
                <Route path="customers" element={<CustomersPage />} />
                <Route path="subjects" element={<SubjectsPage />} />
              </Route>
            </Route>
          </Routes>
        </BrowserRouter>
        {devtools}
      </QueryClientProvider>
    </ThemeProvider>
  );
};

export default App;
