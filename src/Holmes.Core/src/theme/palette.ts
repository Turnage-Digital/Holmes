import { PaletteOptions } from "@mui/material";

export const palette: PaletteOptions = {
  mode: "light",
  primary: {
    main: "#1E3A5F",
    light: "#2D5A8A",
    dark: "#0F2440",
    contrastText: "#FFFFFF",
  },
  secondary: {
    main: "#5C7A94",
    light: "#7A9AB5",
    dark: "#3D5A70",
    contrastText: "#FFFFFF",
  },
  error: {
    main: "#D32F2F",
    light: "#EF5350",
    dark: "#C62828",
  },
  warning: {
    main: "#ED6C02",
    light: "#FF9800",
    dark: "#E65100",
  },
  info: {
    main: "#0288D1",
    light: "#03A9F4",
    dark: "#01579B",
  },
  success: {
    main: "#2E7D32",
    light: "#4CAF50",
    dark: "#1B5E20",
  },
  background: {
    default: "#F8F9FA",
    paper: "#FFFFFF",
  },
  text: {
    primary: "#1A1A1A",
    secondary: "#5F6368",
    disabled: "#9AA0A6",
  },
  divider: "#DDE1E5",
  grey: {
    50: "#F8F9FA",
    100: "#F1F3F5",
    200: "#E8EAED",
    300: "#DDE1E5",
    400: "#BDC1C6",
    500: "#9AA0A6",
    600: "#80868B",
    700: "#5F6368",
    800: "#3C4043",
    900: "#202124",
  },
};

// Status-specific colors for order pipeline and badges
export const statusColors = {
  created: "#9AA0A6",      // Gray - initial
  invited: "#64748B",      // Slate - waiting
  intakeInProgress: "#F59E0B", // Amber - active
  intakeComplete: "#10B981",   // Emerald - done
  readyForRouting: "#2E7D32",  // Green - success
  routingInProgress: "#0288D1", // Blue - processing
  readyForReport: "#7C3AED",   // Purple - final stage
  closed: "#6B7280",       // Gray - finished
  blocked: "#DC2626",      // Red - problem
  canceled: "#6B7280",     // Gray - inactive
} as const;

export type StatusColorKey = keyof typeof statusColors;
