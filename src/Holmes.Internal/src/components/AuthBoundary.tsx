import React from "react";

import { Outlet } from "react-router-dom";

import AuthProvider from "@/context/AuthProvider";

const AuthBoundary = () => (
  <AuthProvider>
    <Outlet />
  </AuthProvider>
);

export default AuthBoundary;
