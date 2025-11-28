import React from "react";

import {CssBaseline, GlobalStyles, ThemeProvider} from "@mui/material";
import {QueryClient, QueryClientProvider} from "@tanstack/react-query";

import IntakeFlow from "@/flows/intake/IntakeFlow";
import theme from "@/theme";

const queryClient = new QueryClient();

const App = () => (
    <ThemeProvider theme={theme}>
        <CssBaseline/>
        <GlobalStyles
            styles={{
                body: {
                    minHeight: "100vh",
                    backgroundColor: theme.palette.background.default,
                },
            }}
        />
        <QueryClientProvider client={queryClient}>
            <IntakeFlow/>
        </QueryClientProvider>
    </ThemeProvider>
);

export default App;
