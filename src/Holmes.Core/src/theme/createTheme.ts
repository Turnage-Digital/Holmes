import { alpha, createTheme as createMuiTheme, Theme } from "@mui/material";

import { colors, typography } from "./tokens";

export type { Theme };

export const createTheme = (): Theme =>
  createMuiTheme({
    cssVariables: true,
    palette: {
      mode: "light",
      primary: {
        light: colors.primary.light,
        main: colors.primary.main,
        dark: colors.primary.dark,
        contrastText: colors.primary.contrastText,
      },
      secondary: {
        light: colors.secondary[400],
        main: colors.secondary[500],
        dark: colors.secondary[700],
        contrastText: "#ffffff",
      },
      error: {
        light: colors.error[400],
        main: colors.error[500],
        dark: colors.error[700],
        contrastText: "#ffffff",
      },
      warning: {
        light: colors.warning[400],
        main: colors.warning[500],
        dark: colors.warning[700],
        contrastText: colors.gray[900],
      },
      success: {
        light: colors.success[400],
        main: colors.success[500],
        dark: colors.success[700],
        contrastText: "#ffffff",
      },
      background: {
        default: colors.gray[50],
        paper: "#ffffff",
      },
      text: {
        primary: colors.gray[900],
        secondary: colors.gray[600],
        disabled: colors.gray[400],
      },
      divider: colors.gray[200],
      action: {
        active: colors.gray[600],
        hover: alpha(colors.gray[500], 0.04),
        selected: alpha(colors.primary[500], 0.08),
        disabled: colors.gray[300],
        disabledBackground: colors.gray[100],
      },
    },
    shape: {
      borderRadius: 3,
    },
    typography: {
      fontFamily: typography.fontFamily,
      fontWeightLight: 300,
      fontWeightRegular: 400,
      fontWeightMedium: 500,
      fontWeightBold: 600,
      h1: {
        fontSize: "2.5rem",
        fontWeight: 600,
        lineHeight: 1.2,
        letterSpacing: "-0.025em",
      },
      h2: {
        fontSize: "2rem",
        fontWeight: 600,
        lineHeight: 1.25,
        letterSpacing: "-0.02em",
      },
      h3: {
        fontSize: "1.75rem",
        fontWeight: 600,
        lineHeight: 1.3,
        letterSpacing: "-0.015em",
      },
      h4: {
        fontSize: "1.5rem",
        fontWeight: 600,
        lineHeight: 1.35,
        letterSpacing: "-0.01em",
      },
      h5: {
        fontSize: "1.25rem",
        fontWeight: 600,
        lineHeight: 1.4,
        letterSpacing: "-0.005em",
      },
      h6: {
        fontSize: "1.125rem",
        fontWeight: 600,
        lineHeight: 1.45,
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
        lineHeight: 1.5,
      },
      body2: {
        fontSize: "0.875rem",
        fontWeight: 400,
        lineHeight: 1.5,
      },
      caption: {
        fontSize: "0.75rem",
        fontWeight: 400,
        lineHeight: 1.4,
        letterSpacing: "0.025em",
      },
      overline: {
        fontSize: "0.75rem",
        fontWeight: 500,
        lineHeight: 1.4,
        letterSpacing: "0.1em",
        textTransform: "uppercase",
      },
    },
    spacing: 8,
    shadows: [
      "none",
      "0px 1px 3px rgba(0, 0, 0, 0.08), 0px 1px 2px rgba(0, 0, 0, 0.12)",
      "0px 2px 6px rgba(0, 0, 0, 0.08), 0px 2px 4px rgba(0, 0, 0, 0.12)",
      "0px 4px 12px rgba(0, 0, 0, 0.08), 0px 4px 8px rgba(0, 0, 0, 0.12)",
      "0px 8px 24px rgba(0, 0, 0, 0.08), 0px 8px 16px rgba(0, 0, 0, 0.12)",
      "0px 12px 32px rgba(0, 0, 0, 0.08), 0px 12px 24px rgba(0, 0, 0, 0.12)",
      "0px 16px 40px rgba(0, 0, 0, 0.1), 0px 16px 32px rgba(0, 0, 0, 0.14)",
      "0px 20px 48px rgba(0, 0, 0, 0.1), 0px 20px 40px rgba(0, 0, 0, 0.14)",
      "0px 24px 56px rgba(0, 0, 0, 0.12), 0px 24px 48px rgba(0, 0, 0, 0.16)",
      "0px 28px 64px rgba(0, 0, 0, 0.12), 0px 28px 56px rgba(0, 0, 0, 0.16)",
      "0px 32px 72px rgba(0, 0, 0, 0.14), 0px 32px 64px rgba(0, 0, 0, 0.18)",
      "0px 36px 80px rgba(0, 0, 0, 0.14), 0px 36px 72px rgba(0, 0, 0, 0.18)",
      "0px 40px 88px rgba(0, 0, 0, 0.16), 0px 40px 80px rgba(0, 0, 0, 0.2)",
      "0px 44px 96px rgba(0, 0, 0, 0.16), 0px 44px 88px rgba(0, 0, 0, 0.2)",
      "0px 48px 104px rgba(0, 0, 0, 0.18), 0px 48px 96px rgba(0, 0, 0, 0.22)",
      "0px 52px 112px rgba(0, 0, 0, 0.18), 0px 52px 104px rgba(0, 0, 0, 0.22)",
      "0px 56px 120px rgba(0, 0, 0, 0.2), 0px 56px 112px rgba(0, 0, 0, 0.24)",
      "0px 60px 128px rgba(0, 0, 0, 0.2), 0px 60px 120px rgba(0, 0, 0, 0.24)",
      "0px 64px 136px rgba(0, 0, 0, 0.22), 0px 64px 128px rgba(0, 0, 0, 0.26)",
      "0px 68px 144px rgba(0, 0, 0, 0.22), 0px 68px 136px rgba(0, 0, 0, 0.26)",
      "0px 72px 152px rgba(0, 0, 0, 0.24), 0px 72px 144px rgba(0, 0, 0, 0.28)",
      "0px 76px 160px rgba(0, 0, 0, 0.24), 0px 76px 152px rgba(0, 0, 0, 0.28)",
      "0px 80px 168px rgba(0, 0, 0, 0.26), 0px 80px 160px rgba(0, 0, 0, 0.3)",
      "0px 84px 176px rgba(0, 0, 0, 0.26), 0px 84px 168px rgba(0, 0, 0, 0.3)",
      "0px 88px 184px rgba(0, 0, 0, 0.28), 0px 88px 176px rgba(0, 0, 0, 0.32)",
    ],
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            backgroundColor: colors.gray[50],
            color: colors.gray[900],
          },
        },
      },
      MuiAppBar: {
        styleOverrides: {
          root: {
            backgroundColor: "#ffffff",
            color: colors.gray[900],
            boxShadow: "none",
            borderBottom: `1px solid ${colors.gray[200]}`,
          },
        },
      },
      MuiToolbar: {
        styleOverrides: {
          root: {
            minHeight: 64,
            paddingInline: "1.5rem",
          },
        },
      },
      MuiDrawer: {
        styleOverrides: {
          paper: {
            borderRadius: 0,
          },
        },
      },
      MuiDialog: {
        styleOverrides: {
          paper: {
            borderRadius: 8,
            boxShadow: "0px 12px 32px rgba(33, 33, 33, 0.16)",
          },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: {
            borderRadius: 6,
            textTransform: "none",
            fontWeight: 600,
            fontSize: "0.9rem",
            boxShadow: "none",
            transition: "all 0.2s cubic-bezier(0.4, 0, 0.2, 1)",
            "&:hover": {
              boxShadow: "0px 2px 8px rgba(62, 147, 144, 0.18)",
              transform: "translateY(-1px)",
            },
            "&:active": {
              transform: "translateY(0)",
            },
          },
          contained: {
            "&:hover": {
              boxShadow: "0px 4px 12px rgba(53, 122, 119, 0.24)",
            },
          },
          outlined: {
            borderWidth: 1,
            "&:hover": {
              borderWidth: 1,
              backgroundColor: alpha(colors.primary.main, 0.05),
            },
          },
        },
      },
      MuiIconButton: {
        styleOverrides: {
          root: {
            borderRadius: 6,
          },
        },
      },
      MuiPaper: {
        styleOverrides: {
          root: {
            borderRadius: 6,
            border: `1px solid ${colors.gray[200]}`,
            boxShadow: "none",
          },
          elevation1: {
            boxShadow: [
              "0px 1px 3px rgba(0, 0, 0, 0.08)",
              "0px 1px 2px rgba(0, 0, 0, 0.12)",
            ].join(", "),
          },
          elevation2: {
            boxShadow: [
              "0px 2px 6px rgba(0, 0, 0, 0.08)",
              "0px 2px 4px rgba(0, 0, 0, 0.12)",
            ].join(", "),
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: 6,
            border: `1px solid ${alpha(colors.gray[900], 0.08)}`,
            boxShadow: "none",
            transition: "border-color 0.2s ease, box-shadow 0.2s ease",
            "&:hover": {
              borderColor: alpha(colors.primary.main, 0.24),
              boxShadow: "0px 8px 24px rgba(33, 33, 33, 0.12)",
            },
          },
        },
      },
      MuiTextField: {
        styleOverrides: {
          root: {
            "& .MuiOutlinedInput-root": {
              borderRadius: 6,
              backgroundColor: "#ffffff",
              "& fieldset": {
                borderColor: colors.gray[300],
              },
              "&:hover fieldset": {
                borderColor: colors.primary.main,
              },
              "&.Mui-focused fieldset": {
                borderColor: colors.primary.dark,
                boxShadow: `0 0 0 3px ${alpha(colors.primary.main, 0.12)}`,
              },
              "&.Mui-disabled": {
                backgroundColor: colors.gray[100],
              },
            },
            "& .MuiInputLabel-root": {
              color: colors.gray[600],
              fontWeight: 500,
              "&.Mui-focused": {
                color: colors.primary.main,
              },
            },
          },
        },
      },
      MuiSelect: {
        styleOverrides: {
          select: {
            borderRadius: 6,
          },
          outlined: {
            backgroundColor: "#ffffff",
          },
        },
      },
      MuiFormControl: {
        styleOverrides: {
          root: {
            "& .MuiFormLabel-root": {
              color: colors.gray[700],
              fontWeight: 500,
              "&.Mui-focused": {
                color: colors.primary.main,
              },
            },
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            borderRadius: 6,
            fontWeight: 500,
            fontSize: "0.75rem",
          },
          colorPrimary: {
            backgroundColor: alpha(colors.primary.main, 0.1),
            color: colors.primary.dark,
            "&:hover": {
              backgroundColor: alpha(colors.primary.main, 0.15),
            },
          },
        },
      },
      MuiToggleButtonGroup: {
        styleOverrides: {
          root: {
            backgroundColor: colors.gray[100],
            borderRadius: 6,
            padding: "0.25rem",
            gap: "0.25rem",
          },
          grouped: {
            border: "none",
            margin: 0,
          },
        },
      },
      MuiToggleButton: {
        styleOverrides: {
          root: {
            borderRadius: 6,
            textTransform: "none",
            fontWeight: 600,
            color: colors.gray[600],
            "&.Mui-selected": {
              backgroundColor: alpha(colors.primary.main, 0.12),
              color: colors.primary.dark,
            },
            "&:hover": {
              backgroundColor: alpha(colors.primary.main, 0.08),
            },
          },
        },
      },
      MuiListItemButton: {
        styleOverrides: {
          root: {
            borderRadius: 6,
            paddingBlock: "0.75rem",
            paddingInline: "0.9rem",
            transition: "background-color 0.2s ease, border-color 0.2s ease",
            "&:hover": {
              backgroundColor: alpha(colors.primary.main, 0.06),
            },
            "&.Mui-selected": {
              backgroundColor: alpha(colors.primary.main, 0.12),
            },
          },
        },
      },
      MuiSkeleton: {
        styleOverrides: {
          root: {
            borderRadius: 6,
          },
        },
      },
      MuiDivider: {
        styleOverrides: {
          root: {
            borderColor: colors.gray[200],
          },
        },
      },
      MuiLink: {
        styleOverrides: {
          root: {
            textDecoration: "none",
            transition: "color 0.2s ease-in-out",
            "&:hover": {
              textDecoration: "underline",
            },
          },
        },
      },
    },
  });
