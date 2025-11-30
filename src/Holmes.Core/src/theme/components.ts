import { Components, Theme } from "@mui/material";

export const components: Components<Omit<Theme, "components">> = {
  // ============================================================================
  // App Bar
  // ============================================================================
  MuiAppBar: {
    styleOverrides: {
      root: {
        boxShadow: "none",
        borderBottom: "1px solid",
        borderColor: "rgba(255, 255, 255, 0.12)",
      },
    },
    defaultProps: {
      elevation: 0,
    },
  },

  // ============================================================================
  // Buttons
  // ============================================================================
  MuiButton: {
    styleOverrides: {
      root: {
        borderRadius: 6,
        fontWeight: 500,
        textTransform: "none",
        boxShadow: "none",
        "&:hover": {
          boxShadow: "none",
        },
      },
      contained: {
        "&:hover": {
          boxShadow: "none",
        },
      },
      containedPrimary: {
        "&:hover": {
          backgroundColor: "#2D5A8A",
        },
      },
      outlined: {
        borderWidth: 1,
        "&:hover": {
          borderWidth: 1,
        },
      },
      sizeSmall: {
        padding: "4px 12px",
        fontSize: "0.8125rem",
      },
      sizeMedium: {
        padding: "8px 16px",
      },
      sizeLarge: {
        padding: "10px 24px",
        fontSize: "0.9375rem",
      },
    },
    defaultProps: {
      disableElevation: true,
    },
  },

  MuiIconButton: {
    styleOverrides: {
      root: {
        borderRadius: 6,
      },
    },
  },

  // ============================================================================
  // Cards
  // ============================================================================
  MuiCard: {
    styleOverrides: {
      root: {
        borderRadius: 8,
        boxShadow: "none",
        border: "1px solid #DDE1E5",
      },
    },
    defaultProps: {
      variant: "outlined",
    },
  },

  MuiCardHeader: {
    styleOverrides: {
      root: {
        padding: "16px 20px",
      },
      title: {
        fontSize: "1rem",
        fontWeight: 600,
      },
      subheader: {
        fontSize: "0.875rem",
      },
    },
  },

  MuiCardContent: {
    styleOverrides: {
      root: {
        padding: "16px 20px",
        "&:last-child": {
          paddingBottom: "16px",
        },
      },
    },
  },

  MuiCardActions: {
    styleOverrides: {
      root: {
        padding: "12px 20px",
      },
    },
  },

  // ============================================================================
  // Chips
  // ============================================================================
  MuiChip: {
    styleOverrides: {
      root: {
        borderRadius: 4,
        fontWeight: 500,
        fontSize: "0.75rem",
      },
      sizeSmall: {
        height: 24,
      },
      sizeMedium: {
        height: 28,
      },
      outlined: {
        borderWidth: 1,
      },
    },
  },

  // ============================================================================
  // Dialogs
  // ============================================================================
  MuiDialog: {
    styleOverrides: {
      paper: {
        borderRadius: 8,
        boxShadow: "0 8px 32px rgba(0, 0, 0, 0.12)",
      },
    },
  },

  MuiDialogTitle: {
    styleOverrides: {
      root: {
        fontSize: "1.125rem",
        fontWeight: 600,
        padding: "20px 24px 16px",
      },
    },
  },

  MuiDialogContent: {
    styleOverrides: {
      root: {
        padding: "16px 24px",
      },
    },
  },

  MuiDialogActions: {
    styleOverrides: {
      root: {
        padding: "16px 24px 20px",
      },
    },
  },

  // ============================================================================
  // Form Inputs
  // ============================================================================
  MuiTextField: {
    defaultProps: {
      variant: "outlined",
      size: "medium",
    },
  },

  MuiOutlinedInput: {
    styleOverrides: {
      root: {
        borderRadius: 6,
        "&:hover .MuiOutlinedInput-notchedOutline": {
          borderColor: "#5C7A94",
        },
        "&.Mui-focused .MuiOutlinedInput-notchedOutline": {
          borderWidth: 2,
        },
      },
      notchedOutline: {
        borderColor: "#DDE1E5",
      },
    },
  },

  MuiInputLabel: {
    styleOverrides: {
      root: {
        fontSize: "0.875rem",
        fontWeight: 500,
      },
    },
  },

  MuiFormHelperText: {
    styleOverrides: {
      root: {
        fontSize: "0.75rem",
        marginTop: 4,
      },
    },
  },

  MuiSelect: {
    styleOverrides: {
      select: {
        borderRadius: 6,
      },
    },
  },

  // ============================================================================
  // Tables & Data Grids
  // ============================================================================
  MuiTableHead: {
    styleOverrides: {
      root: {
        backgroundColor: "#F1F3F5",
        "& .MuiTableCell-head": {
          fontWeight: 600,
          color: "#3C4043",
          borderBottom: "1px solid #DDE1E5",
        },
      },
    },
  },

  MuiTableRow: {
    styleOverrides: {
      root: {
        "&:nth-of-type(odd)": {
          backgroundColor: "#F8F9FA",
        },
        "&:hover": {
          backgroundColor: "rgba(30, 58, 95, 0.04)",
        },
      },
    },
  },

  MuiTableCell: {
    styleOverrides: {
      root: {
        borderBottom: "1px solid #E8EAED",
        padding: "12px 16px",
      },
    },
  },

  // ============================================================================
  // Tabs
  // ============================================================================
  MuiTabs: {
    styleOverrides: {
      root: {
        minHeight: 44,
      },
      indicator: {
        height: 2,
      },
    },
  },

  MuiTab: {
    styleOverrides: {
      root: {
        textTransform: "none",
        fontWeight: 500,
        fontSize: "0.875rem",
        minHeight: 44,
        padding: "12px 16px",
        "&.Mui-selected": {
          fontWeight: 600,
        },
      },
    },
  },

  // ============================================================================
  // Alerts
  // ============================================================================
  MuiAlert: {
    styleOverrides: {
      root: {
        borderRadius: 6,
      },
      standardSuccess: {
        backgroundColor: "rgba(46, 125, 50, 0.08)",
        color: "#1B5E20",
      },
      standardError: {
        backgroundColor: "rgba(211, 47, 47, 0.08)",
        color: "#C62828",
      },
      standardWarning: {
        backgroundColor: "rgba(237, 108, 2, 0.08)",
        color: "#E65100",
      },
      standardInfo: {
        backgroundColor: "rgba(2, 136, 209, 0.08)",
        color: "#01579B",
      },
    },
  },

  // ============================================================================
  // Tooltips
  // ============================================================================
  MuiTooltip: {
    styleOverrides: {
      tooltip: {
        backgroundColor: "#3C4043",
        fontSize: "0.75rem",
        padding: "6px 12px",
        borderRadius: 4,
      },
    },
  },

  // ============================================================================
  // Dividers
  // ============================================================================
  MuiDivider: {
    styleOverrides: {
      root: {
        borderColor: "#E8EAED",
      },
    },
  },

  // ============================================================================
  // Paper
  // ============================================================================
  MuiPaper: {
    styleOverrides: {
      root: {
        backgroundImage: "none", // Remove default gradient
      },
      rounded: {
        borderRadius: 8,
      },
      outlined: {
        borderColor: "#DDE1E5",
      },
    },
    defaultProps: {
      elevation: 0,
    },
  },

  // ============================================================================
  // Toggle Buttons
  // ============================================================================
  MuiToggleButton: {
    styleOverrides: {
      root: {
        borderRadius: 6,
        textTransform: "none",
        fontWeight: 500,
        fontSize: "0.8125rem",
        padding: "6px 12px",
        borderColor: "#DDE1E5",
        "&.Mui-selected": {
          backgroundColor: "rgba(30, 58, 95, 0.08)",
          color: "#1E3A5F",
          borderColor: "#1E3A5F",
          "&:hover": {
            backgroundColor: "rgba(30, 58, 95, 0.12)",
          },
        },
      },
    },
  },

  MuiToggleButtonGroup: {
    styleOverrides: {
      root: {
        backgroundColor: "#FFFFFF",
      },
      grouped: {
        "&:not(:first-of-type)": {
          borderLeft: "1px solid #DDE1E5",
          marginLeft: 0,
        },
      },
    },
  },

  // ============================================================================
  // Skeleton
  // ============================================================================
  MuiSkeleton: {
    styleOverrides: {
      root: {
        backgroundColor: "#E8EAED",
      },
    },
  },

  // ============================================================================
  // List Items
  // ============================================================================
  MuiListItemButton: {
    styleOverrides: {
      root: {
        borderRadius: 6,
        "&.Mui-selected": {
          backgroundColor: "rgba(30, 58, 95, 0.08)",
        },
        "&:hover": {
          backgroundColor: "rgba(30, 58, 95, 0.04)",
        },
      },
    },
  },

  // ============================================================================
  // Snackbar
  // ============================================================================
  MuiSnackbar: {
    defaultProps: {
      anchorOrigin: {
        vertical: "bottom",
        horizontal: "center",
      },
    },
  },

  // ============================================================================
  // Backdrop (for dialogs)
  // ============================================================================
  MuiBackdrop: {
    styleOverrides: {
      root: {
        backgroundColor: "rgba(0, 0, 0, 0.4)",
      },
    },
  },

  // ============================================================================
  // Circular Progress
  // ============================================================================
  MuiCircularProgress: {
    defaultProps: {
      thickness: 4,
    },
  },
};
