import React, { useMemo } from "react";

import {
  Box,
  Button,
  Card,
  CardContent,
  CardHeader,
  Typography,
} from "@mui/material";
import { useLocation } from "react-router-dom";

const DEFAULT_RETURN_URL = "/";

const AuthOptionsPage = () => {
  const location = useLocation();
  const requestedReturnUrl =
    (location.state as { returnUrl?: string } | null)?.returnUrl ??
    DEFAULT_RETURN_URL;

  const loginUrl = useMemo(
    () => `/auth/login?returnUrl=${encodeURIComponent(requestedReturnUrl)}`,
    [requestedReturnUrl],
  );

  const handleContinue = () => {
    window.location.href = loginUrl;
  };

  return (
    <Box
      sx={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        bgcolor: "background.default",
        p: 2,
      }}
    >
      <Card sx={{ maxWidth: 480, width: "100%", boxShadow: 6 }}>
        <CardHeader
          title="Sign in to Holmes"
          subheader="Choose an identity provider."
        />
        <CardContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            Continue with Google to access the Holmes admin console.
          </Typography>
          <Button
            variant="contained"
            color="primary"
            fullWidth
            onClick={handleContinue}
            size="large"
          >
            Continue with Google
          </Button>
        </CardContent>
      </Card>
    </Box>
  );
};

export default AuthOptionsPage;
