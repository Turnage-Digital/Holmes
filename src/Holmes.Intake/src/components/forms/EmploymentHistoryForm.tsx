import React from "react";

import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import {
  Box,
  Button,
  Checkbox,
  Divider,
  FormControlLabel,
  IconButton,
  Paper,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";

import { createEmptyEmployment, IntakeEmployment } from "@/types/intake";

interface EmploymentHistoryFormProps {
  employments: IntakeEmployment[];
  onChange: (employments: IntakeEmployment[]) => void;
  minEmployments?: number;
  maxEmployments?: number;
  yearsRequired?: number;
}

const EmploymentHistoryForm: React.FC<EmploymentHistoryFormProps> = ({
  employments,
  onChange,
  minEmployments = 0,
  maxEmployments = 10,
  yearsRequired = 7,
}) => {
  const handleAddEmployment = () => {
    if (employments.length >= maxEmployments) return;
    onChange([...employments, createEmptyEmployment()]);
  };

  const handleRemoveEmployment = (id: string) => {
    if (employments.length <= minEmployments) return;
    onChange(employments.filter((e) => e.id !== id));
  };

  const handleUpdateEmployment = (
    id: string,
    updates: Partial<IntakeEmployment>,
  ) => {
    onChange(
      employments.map((e) => {
        if (e.id !== id) return e;
        const updated = { ...e, ...updates };
        // If marking as current, clear the endDate
        if (updates.isCurrent === true) {
          updated.endDate = "";
          updated.reasonForLeaving = "";
        }
        return updated;
      }),
    );
  };

  const formatEmploymentLabel = (
    index: number,
    employment: IntakeEmployment,
  ) => {
    if (employment.isCurrent) return "Current Employer";
    if (employment.employerName) return employment.employerName;
    return `Employment ${index + 1}`;
  };

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Please provide your employment history for the past {yearsRequired}{" "}
          years. Start with your current or most recent employer.
        </Typography>
      </Box>

      {employments.length === 0 && (
        <Paper
          variant="outlined"
          sx={{
            p: 3,
            textAlign: "center",
            backgroundColor: (theme) => alpha(theme.palette.action.hover, 0.02),
          }}
        >
          <Typography color="text.secondary" gutterBottom>
            No employment history added yet.
          </Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleAddEmployment}
            size="small"
          >
            Add employer
          </Button>
        </Paper>
      )}

      {employments.map((employment, index) => (
        <Paper
          key={employment.id}
          variant="outlined"
          sx={{
            p: 2,
            backgroundColor: (theme) =>
              employment.isCurrent
                ? alpha(theme.palette.primary.main, 0.02)
                : "transparent",
          }}
        >
          <Stack spacing={2}>
            <Stack
              direction="row"
              justifyContent="space-between"
              alignItems="center"
            >
              <Typography variant="subtitle2" fontWeight={600}>
                {formatEmploymentLabel(index, employment)}
              </Typography>
              {employments.length > minEmployments && (
                <IconButton
                  size="small"
                  onClick={() => handleRemoveEmployment(employment.id)}
                  aria-label="Remove employment"
                >
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              )}
            </Stack>

            <FormControlLabel
              control={
                <Checkbox
                  checked={employment.isCurrent}
                  onChange={(e) =>
                    handleUpdateEmployment(employment.id, {
                      isCurrent: e.target.checked,
                    })
                  }
                />
              }
              label="This is my current employer"
            />

            <TextField
              label="Employer name"
              value={employment.employerName}
              onChange={(e) =>
                handleUpdateEmployment(employment.id, {
                  employerName: e.target.value,
                })
              }
              fullWidth
              required
            />

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                label="Job title"
                value={employment.jobTitle}
                onChange={(e) =>
                  handleUpdateEmployment(employment.id, {
                    jobTitle: e.target.value,
                  })
                }
                fullWidth
              />
              <TextField
                label="Employer phone"
                value={employment.employerPhone}
                onChange={(e) =>
                  handleUpdateEmployment(employment.id, {
                    employerPhone: e.target.value
                      .replace(/[^\d+()-\s]/g, "")
                      .slice(0, 20),
                  })
                }
                fullWidth
                inputProps={{ inputMode: "tel" }}
              />
            </Stack>

            <TextField
              label="Employer address"
              value={employment.employerAddress}
              onChange={(e) =>
                handleUpdateEmployment(employment.id, {
                  employerAddress: e.target.value,
                })
              }
              fullWidth
              placeholder="City, State or full address"
            />

            <Divider sx={{ my: 1 }} />

            <Typography variant="body2" color="text.secondary">
              Supervisor information (for verification)
            </Typography>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                label="Supervisor name"
                value={employment.supervisorName}
                onChange={(e) =>
                  handleUpdateEmployment(employment.id, {
                    supervisorName: e.target.value,
                  })
                }
                fullWidth
              />
              <TextField
                label="Supervisor phone"
                value={employment.supervisorPhone}
                onChange={(e) =>
                  handleUpdateEmployment(employment.id, {
                    supervisorPhone: e.target.value
                      .replace(/[^\d+()-\s]/g, "")
                      .slice(0, 20),
                  })
                }
                fullWidth
                inputProps={{ inputMode: "tel" }}
              />
            </Stack>

            <FormControlLabel
              control={
                <Checkbox
                  checked={employment.canContact}
                  onChange={(e) =>
                    handleUpdateEmployment(employment.id, {
                      canContact: e.target.checked,
                    })
                  }
                />
              }
              label="May we contact this employer?"
            />

            <Divider sx={{ my: 1 }} />

            <Typography variant="body2" color="text.secondary">
              Employment dates
            </Typography>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                type="date"
                label="Start date"
                value={employment.startDate}
                onChange={(e) =>
                  handleUpdateEmployment(employment.id, {
                    startDate: e.target.value,
                  })
                }
                fullWidth
                required
                InputLabelProps={{ shrink: true }}
              />
              {!employment.isCurrent && (
                <TextField
                  type="date"
                  label="End date"
                  value={employment.endDate}
                  onChange={(e) =>
                    handleUpdateEmployment(employment.id, {
                      endDate: e.target.value,
                    })
                  }
                  fullWidth
                  required
                  InputLabelProps={{ shrink: true }}
                />
              )}
            </Stack>

            {!employment.isCurrent && (
              <TextField
                label="Reason for leaving"
                value={employment.reasonForLeaving}
                onChange={(e) =>
                  handleUpdateEmployment(employment.id, {
                    reasonForLeaving: e.target.value,
                  })
                }
                fullWidth
                multiline
                rows={2}
              />
            )}
          </Stack>
        </Paper>
      ))}

      {employments.length > 0 && employments.length < maxEmployments && (
        <Button
          variant="outlined"
          startIcon={<AddIcon />}
          onClick={handleAddEmployment}
          sx={{
            borderStyle: "dashed",
            color: (theme) => theme.palette.text.secondary,
          }}
        >
          Add another employer
        </Button>
      )}
    </Stack>
  );
};

export default EmploymentHistoryForm;
