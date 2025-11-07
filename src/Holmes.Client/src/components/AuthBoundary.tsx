import React, {useEffect} from "react";

import {Box, CircularProgress} from "@mui/material";
import {useQuery} from "@tanstack/react-query";
import {Outlet, useLocation, useNavigate} from "react-router-dom";

import {AuthProvider} from "@/context/AuthContext";
import {ApiError, apiFetch} from "@/lib/api";
import {CurrentUserDto} from "@/types/api";

const FullScreenLoader = () => (
    <Box
        sx={{
            minHeight: "100vh",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
        }}
    >
        <CircularProgress/>
    </Box>
);

const AuthBoundary = () => {
    const location = useLocation();
    const navigate = useNavigate();
    const currentUserQuery = useQuery<CurrentUserDto, ApiError>({
        queryKey: ["currentUser"],
        queryFn: () => apiFetch<CurrentUserDto>("/users/me"),
        retry: false,
    });

    useEffect(() => {
        if (currentUserQuery.error instanceof ApiError) {
            const status = currentUserQuery.error.status;
            const shouldRedirect = status === 401 || status === 403 || status === 404;
            if (shouldRedirect) {
                navigate("/auth/options", {
                    replace: true,
                    state: {
                        returnUrl: location.pathname + location.search,
                    },
                });
            }
        }
    }, [currentUserQuery.error, location.pathname, location.search, navigate]);

    if (currentUserQuery.isPending) {
        return <FullScreenLoader/>;
    }

    if (currentUserQuery.error) {
        return null;
    }

    return (
        <AuthProvider value={currentUserQuery.data}>
            <Outlet/>
        </AuthProvider>
    );
};

export default AuthBoundary;
