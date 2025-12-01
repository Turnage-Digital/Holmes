import {
  createTheme as muiCreateTheme,
  Theme,
  ThemeOptions,
} from "@mui/material/styles";

import { components } from "./components";
import { palette, statusColors } from "./palette";
import { typography } from "./typography";

// Extend the theme to include custom properties
declare module "@mui/material/styles" {
  interface Theme {
    statusColors: typeof statusColors;
  }
  interface ThemeOptions {
    statusColors?: typeof statusColors;
  }
}

export interface CreateThemeOptions {
  /**
   * Override any theme options
   */
  overrides?: ThemeOptions;
}

/**
 * Creates the Holmes application theme.
 *
 * This theme is designed for enterprise compliance software:
 * - Clinical, professional palette (navy blue primary)
 * - Clear typography hierarchy
 * - Consistent component styling
 * - Status-specific colors for order pipeline
 *
 * @example
 * ```tsx
 * import { createTheme } from "@holmes/ui-core";
 * import { ThemeProvider } from "@mui/material";
 *
 * const theme = createTheme();
 *
 * function App() {
 *   return (
 *     <ThemeProvider theme={theme}>
 *       <YourApp />
 *     </ThemeProvider>
 *   );
 * }
 * ```
 */
export const createTheme = (options?: CreateThemeOptions): Theme => {
  const baseTheme: ThemeOptions = {
    palette,
    typography,
    shape: {
      borderRadius: 8,
    },
    spacing: 8,
    components,
    statusColors,
  };

  // Merge with any overrides
  const mergedOptions = options?.overrides
    ? {
        ...baseTheme,
        ...options.overrides,
        palette: {
          ...baseTheme.palette,
          ...options.overrides.palette,
        },
        typography: {
          ...baseTheme.typography,
          ...options.overrides.typography,
        },
        components: {
          ...baseTheme.components,
          ...options.overrides.components,
        },
      }
    : baseTheme;

  return muiCreateTheme(mergedOptions);
};

// Re-export for convenience
export { palette, statusColors, typography, components };
export type { Theme };
