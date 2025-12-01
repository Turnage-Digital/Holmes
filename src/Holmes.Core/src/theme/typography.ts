import { TypographyVariantsOptions } from "@mui/material/styles";

const fontFamily = [
  "-apple-system",
  "BlinkMacSystemFont",
  '"Segoe UI"',
  "Roboto",
  '"Helvetica Neue"',
  "Arial",
  "sans-serif",
  '"Apple Color Emoji"',
  '"Segoe UI Emoji"',
  '"Segoe UI Symbol"',
].join(",");

const monospaceFontFamily = [
  '"SF Mono"',
  '"Fira Code"',
  '"Fira Mono"',
  "Consolas",
  '"Liberation Mono"',
  "Menlo",
  "monospace",
].join(",");

export const typography: TypographyVariantsOptions = {
  fontFamily,
  fontWeightLight: 300,
  fontWeightRegular: 400,
  fontWeightMedium: 500,
  fontWeightBold: 700,
  h1: {
    fontSize: "2rem",
    fontWeight: 600,
    lineHeight: 1.25,
    letterSpacing: "-0.01em",
  },
  h2: {
    fontSize: "1.75rem",
    fontWeight: 600,
    lineHeight: 1.3,
    letterSpacing: "-0.005em",
  },
  h3: {
    fontSize: "1.5rem",
    fontWeight: 600,
    lineHeight: 1.35,
  },
  h4: {
    fontSize: "1.25rem",
    fontWeight: 600,
    lineHeight: 1.4,
  },
  h5: {
    fontSize: "1.125rem",
    fontWeight: 600,
    lineHeight: 1.45,
  },
  h6: {
    fontSize: "1rem",
    fontWeight: 600,
    lineHeight: 1.5,
  },
  subtitle1: {
    fontSize: "1rem",
    fontWeight: 500,
    lineHeight: 1.5,
  },
  subtitle2: {
    fontSize: "0.875rem",
    fontWeight: 500,
    lineHeight: 1.5,
  },
  body1: {
    fontSize: "1rem",
    fontWeight: 400,
    lineHeight: 1.6,
  },
  body2: {
    fontSize: "0.875rem",
    fontWeight: 400,
    lineHeight: 1.6,
  },
  caption: {
    fontSize: "0.75rem",
    fontWeight: 400,
    lineHeight: 1.5,
  },
  overline: {
    fontSize: "0.625rem",
    fontWeight: 600,
    lineHeight: 1.5,
    letterSpacing: "0.08em",
    textTransform: "uppercase",
  },
  button: {
    fontSize: "0.875rem",
    fontWeight: 500,
    lineHeight: 1.5,
    // No all-caps buttons
    textTransform: "none",
  },
};

export { monospaceFontFamily };
