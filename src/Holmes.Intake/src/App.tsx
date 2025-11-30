import React from "react";

import { CssBaseline, GlobalStyles, ThemeProvider } from "@mui/material";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

import IntakeFlow from "@/flows/intake/IntakeFlow";
import { intakeTheme } from "@/theme";

const queryClient = new QueryClient();

const App = () => (
  <ThemeProvider theme={intakeTheme}>
    <CssBaseline />
    <GlobalStyles
      styles={{
        body: {
          minHeight: "100vh",
          backgroundColor: intakeTheme.palette.background.default
        }
      }}
    />
    <QueryClientProvider client={queryClient}>
      <IntakeFlow />
    </QueryClientProvider>
  </ThemeProvider>
);

export default App;
