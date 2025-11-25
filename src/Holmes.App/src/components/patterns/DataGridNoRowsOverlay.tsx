import React from "react";

import { Box, Typography } from "@mui/material";
import { GridOverlay } from "@mui/x-data-grid";

interface DataGridNoRowsOverlayProps {
  message: string;
}

const DataGridNoRowsOverlay = ({ message }: DataGridNoRowsOverlayProps) => (
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
      <Typography variant="body2">{message}</Typography>
    </Box>
  </GridOverlay>
);

export default DataGridNoRowsOverlay;
