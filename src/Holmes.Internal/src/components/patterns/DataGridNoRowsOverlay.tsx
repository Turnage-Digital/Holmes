import React from "react";

import { Box, Typography } from "@mui/material";
import { GridOverlay } from "@mui/x-data-grid";

const DataGridNoRowsOverlay = () => (
  <GridOverlay>
    <Box
      sx={{
        px: 2,
        py: 4,
        textAlign: "center",
        color: "text.secondary",
        fontSize: 14,
      }}
    >
      <Typography variant="body2">No rows to display</Typography>
    </Box>
  </GridOverlay>
);

export default DataGridNoRowsOverlay;
