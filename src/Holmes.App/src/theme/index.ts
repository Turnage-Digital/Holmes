import {createTheme} from "@mui/material/styles";

import {colorTokens, spacingTokens, typographyTokens} from "./tokens";

export const appTheme = createTheme({
    palette: {
        mode: "light",
        primary: {
            main: colorTokens.brandPrimary,
            contrastText: "#ffffff",
        },
        secondary: {
            main: colorTokens.brandSecondary,
            contrastText: "#1a202c",
        },
        success: {
            main: colorTokens.success,
        },
        warning: {
            main: colorTokens.warning,
        },
        error: {
            main: colorTokens.danger,
        },
        text: {
            primary: colorTokens.textPrimary,
            secondary: colorTokens.textSecondary,
        },
        background: {
            default: colorTokens.surfaceMuted,
            paper: colorTokens.surface,
        },
        divider: colorTokens.divider,
    },
    spacing: spacingTokens.unit,
    typography: {
        fontFamily: typographyTokens.fontFamily,
        h1: {
            fontSize: "2.75rem",
            fontWeight: 600,
        },
        h2: {
            fontSize: "2.25rem",
            fontWeight: 600,
        },
        h3: {
            fontSize: "1.75rem",
            fontWeight: 600,
        },
        h4: {
            fontSize: "1.5rem",
            fontWeight: 600,
        },
        subtitle1: {
            color: colorTokens.textSecondary,
        },
        body2: {
            color: colorTokens.textSecondary,
        },
    },
    components: {
        MuiButton: {
            defaultProps: {
                variant: "contained",
            },
            styleOverrides: {
                root: {
                    borderRadius: 8,
                    textTransform: "none",
                    fontWeight: 600,
                },
            },
        },
        MuiCard: {
            styleOverrides: {
                root: {
                    borderRadius: 16,
                },
            },
        },
        MuiTabs: {
            styleOverrides: {
                root: {
                    paddingLeft: spacingTokens.gutter,
                    paddingRight: spacingTokens.gutter,
                    backgroundColor: colorTokens.surface,
                },
                indicator: {
                    height: 3,
                    borderRadius: 3,
                },
            },
        },
    },
});

export * from "./tokens";
