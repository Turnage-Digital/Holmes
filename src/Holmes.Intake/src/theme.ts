import {createTheme} from "@mui/material";

const theme = createTheme({
    cssVariables: true,
    typography: {
        fontFamily:
            "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
        h4: {
            fontWeight: 600,
        },
        body1: {
            fontSize: "1rem",
            lineHeight: 1.6,
        },
    },
    shape: {
        borderRadius: 14,
    },
    palette: {
        background: {
            default: "#f4f5f7",
        },
        primary: {
            main: "#111827",
        },
        secondary: {
            main: "#0891b2",
        },
    },
    components: {
        MuiButton: {
            defaultProps: {
                disableElevation: true,
            },
            styleOverrides: {
                root: {
                    textTransform: "none",
                    fontWeight: 600,
                    borderRadius: 999,
                },
            },
        },
        MuiPaper: {
            styleOverrides: {
                root: {
                    borderRadius: 20,
                },
            },
        },
    },
});

export default theme;
