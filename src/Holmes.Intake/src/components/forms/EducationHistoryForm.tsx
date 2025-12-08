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
  Typography
} from "@mui/material";
import { alpha } from "@mui/material/styles";

import { createEmptyEducation, IntakeEducation } from "@/types/intake";

interface EducationHistoryFormProps {
  educations: IntakeEducation[];
  onChange: (educations: IntakeEducation[]) => void;
  minEducations?: number;
  maxEducations?: number;
}

const EducationHistoryForm: React.FC<EducationHistoryFormProps> = ({
                                                                     educations,
                                                                     onChange,
                                                                     minEducations = 0,
                                                                     maxEducations = 10
                                                                   }) => {
  const handleAddEducation = () => {
    if (educations.length >= maxEducations) return;
    onChange([...educations, createEmptyEducation()]);
  };

  const handleRemoveEducation = (id: string) => {
    if (educations.length <= minEducations) return;
    onChange(educations.filter((e) => e.id !== id));
  };

  const handleUpdateEducation = (
    id: string,
    updates: Partial<IntakeEducation>
  ) => {
    onChange(
      educations.map((e) => {
        if (e.id !== id) return e;
        return { ...e, ...updates };
      })
    );
  };

  const formatEducationLabel = (index: number, education: IntakeEducation) => {
    if (education.institutionName) return education.institutionName;
    return `Education ${index + 1}`;
  };

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Please provide information about your educational background,
          including colleges, universities, trade schools, and other relevant
          institutions.
        </Typography>
      </Box>

      {educations.length === 0 && (
        <Paper
          variant="outlined"
          sx={{
            p: 3,
            textAlign: "center",
            backgroundColor: (theme) => alpha(theme.palette.action.hover, 0.02)
          }}
        >
          <Typography color="text.secondary" gutterBottom>
            No education history added yet.
          </Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleAddEducation}
            size="small"
          >
            Add school or institution
          </Button>
        </Paper>
      )}

      {educations.map((education, index) => (
        <Paper
          key={education.id}
          variant="outlined"
          sx={{
            p: 2,
            backgroundColor: (theme) =>
              education.graduated
                ? alpha(theme.palette.success.main, 0.02)
                : "transparent"
          }}
        >
          <Stack spacing={2}>
            <Stack
              direction="row"
              justifyContent="space-between"
              alignItems="center"
            >
              <Typography variant="subtitle2" fontWeight={600}>
                {formatEducationLabel(index, education)}
              </Typography>
              {educations.length > minEducations && (
                <IconButton
                  size="small"
                  onClick={() => handleRemoveEducation(education.id)}
                  aria-label="Remove education"
                >
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              )}
            </Stack>

            <TextField
              label="School or institution name"
              value={education.institutionName}
              onChange={(e) =>
                handleUpdateEducation(education.id, {
                  institutionName: e.target.value
                })
              }
              fullWidth
              required
            />

            <TextField
              label="Institution address"
              value={education.institutionAddress}
              onChange={(e) =>
                handleUpdateEducation(education.id, {
                  institutionAddress: e.target.value
                })
              }
              fullWidth
              placeholder="City, State or full address"
            />

            <Divider sx={{ my: 1 }} />

            <Typography variant="body2" color="text.secondary">
              Degree information
            </Typography>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                label="Degree or certification"
                value={education.degree}
                onChange={(e) =>
                  handleUpdateEducation(education.id, {
                    degree: e.target.value
                  })
                }
                fullWidth
                placeholder="e.g., Bachelor of Science, Associate Degree"
              />
              <TextField
                label="Major or field of study"
                value={education.major}
                onChange={(e) =>
                  handleUpdateEducation(education.id, {
                    major: e.target.value
                  })
                }
                fullWidth
                placeholder="e.g., Computer Science"
              />
            </Stack>

            <FormControlLabel
              control={
                <Checkbox
                  checked={education.graduated}
                  onChange={(e) =>
                    handleUpdateEducation(education.id, {
                      graduated: e.target.checked
                    })
                  }
                />
              }
              label="I graduated or completed this program"
            />

            <Divider sx={{ my: 1 }} />

            <Typography variant="body2" color="text.secondary">
              Dates attended
            </Typography>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                type="date"
                label="From date"
                value={education.attendedFrom}
                onChange={(e) =>
                  handleUpdateEducation(education.id, {
                    attendedFrom: e.target.value
                  })
                }
                fullWidth
                InputLabelProps={{ shrink: true }}
              />
              <TextField
                type="date"
                label="To date"
                value={education.attendedTo}
                onChange={(e) =>
                  handleUpdateEducation(education.id, {
                    attendedTo: e.target.value
                  })
                }
                fullWidth
                InputLabelProps={{ shrink: true }}
              />
            </Stack>

            {education.graduated && (
              <TextField
                type="date"
                label="Graduation date"
                value={education.graduationDate}
                onChange={(e) =>
                  handleUpdateEducation(education.id, {
                    graduationDate: e.target.value
                  })
                }
                fullWidth
                InputLabelProps={{ shrink: true }}
              />
            )}
          </Stack>
        </Paper>
      ))}

      {educations.length > 0 && educations.length < maxEducations && (
        <Button
          variant="outlined"
          startIcon={<AddIcon />}
          onClick={handleAddEducation}
          sx={{
            borderStyle: "dashed",
            color: (theme) => theme.palette.text.secondary
          }}
        >
          Add another school
        </Button>
      )}
    </Stack>
  );
};

export default EducationHistoryForm;
