import React from "react";

import { createTheme } from "@holmes/ui-core";
import { CssBaseline, ThemeProvider } from "@mui/material";
import { QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";

import AuthBoundary from "./components/AuthBoundary";
import { queryClient } from "./lib/queryClient";
import CustomerDetailPage from "./pages/CustomerDetailPage";
import CustomersPage from "./pages/CustomersPage";
import DashboardPage from "./pages/DashboardPage";
import FulfillmentDashboardPage from "./pages/FulfillmentDashboardPage";
import OrderDetailPage from "./pages/OrderDetailPage";
import OrdersPage from "./pages/OrdersPage";
import SubjectDetailPage from "./pages/SubjectDetailPage";
import SubjectsPage from "./pages/SubjectsPage";
import UsersPage from "./pages/UsersPage";

import { AppShell } from "@/components/layout";

const App = () => {
  const devtools = import.meta.env.DEV ? (
    <ReactQueryDevtools initialIsOpen={false} />
  ) : null;

  const appTheme = createTheme();

  return (
    <ThemeProvider theme={appTheme}>
      <CssBaseline />
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <Routes>
            <Route element={<AuthBoundary />}>
              <Route path="/" element={<AppShell />}>
                <Route index element={<DashboardPage />} />
                <Route path="orders" element={<OrdersPage />} />
                <Route path="orders/:orderId" element={<OrderDetailPage />} />
                <Route path="subjects" element={<SubjectsPage />} />
                <Route path="subjects/:subjectId" element={<SubjectDetailPage />} />
                <Route path="customers" element={<CustomersPage />} />
                <Route path="customers/:customerId" element={<CustomerDetailPage />} />
                <Route path="users" element={<UsersPage />} />
                <Route path="fulfillment" element={<FulfillmentDashboardPage />} />
                {/* Redirect old default to dashboard */}
                <Route path="*" element={<Navigate to="/" replace />} />
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
