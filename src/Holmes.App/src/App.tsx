import React from "react";

import {CssBaseline, ThemeProvider} from "@mui/material";
import {QueryClientProvider} from "@tanstack/react-query";
import {ReactQueryDevtools} from "@tanstack/react-query-devtools";
import {BrowserRouter, Navigate, Route, Routes} from "react-router-dom";

import AuthBoundary from "./components/AuthBoundary";
import {queryClient} from "./lib/queryClient";
import CustomersPage from "./pages/CustomersPage";
import OrdersPage from "./pages/OrdersPage";
import SubjectsPage from "./pages/SubjectsPage";
import UsersPage from "./pages/UsersPage";
import {appTheme} from "./theme";

import {AppLayout} from "@/components/layout";

const App = () => {
    const devtools = import.meta.env.DEV ? (
        <ReactQueryDevtools initialIsOpen={false}/>
    ) : null;

    return (
        <ThemeProvider theme={appTheme}>
            <CssBaseline/>
            <QueryClientProvider client={queryClient}>
                <BrowserRouter>
                    <Routes>
                        <Route element={<AuthBoundary/>}>
                            <Route path="/" element={<AppLayout/>}>
                                <Route index element={<Navigate to="/users" replace/>}/>
                                <Route path="users" element={<UsersPage/>}/>
                                <Route path="customers" element={<CustomersPage/>}/>
                                <Route path="subjects" element={<SubjectsPage/>}/>
                                <Route path="orders" element={<OrdersPage/>}/>
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
