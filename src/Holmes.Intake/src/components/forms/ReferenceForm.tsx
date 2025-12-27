import React from "react";

import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import {
  Box,
  Button,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography
} from "@mui/material";
import { alpha } from "@mui/material/styles";

import { createEmptyReference, IntakeReference, ReferenceType } from "@/types/intake";

interface ReferenceFormProps {
  references: IntakeReference[];
  onChange: (references: IntakeReference[]) => void;
  minReferences?: number;
  maxReferences?: number;
  requiredTypes?: ReferenceType[];
}

const ReferenceForm: React.FC<ReferenceFormProps> = ({
                                                       references,
                                                       onChange,
                                                       minReferences = 0,
                                                       maxReferences = 5,
                                                       requiredTypes = []
                                                     }) => {
  const handleAddReference = (type?: ReferenceType) => {
    if (references.length >= maxReferences) return;
    const newRef = createEmptyReference();
    if (type !== undefined) {
      newRef.type = type;
    }
    onChange([...references, newRef]);
  };

  const handleRemoveReference = (id: string) => {
    if (references.length <= minReferences) return;
    onChange(references.filter((r) => r.id !== id));
  };

  const handleUpdateReference = (
    id: string,
    updates: Partial<IntakeReference>
  ) => {
    onChange(references.map((r) => (r.id === id ? { ...r, ...updates } : r)));
  };

  const formatReferenceLabel = (index: number, reference: IntakeReference) => {
    const typeLabel =
      reference.type === ReferenceType.Professional
        ? "Professional"
        : "Personal";
    if (reference.name) return `${reference.name} (${typeLabel})`;
    return `${typeLabel} Reference ${index + 1}`;
  };

  const personalCount = references.filter(
    (r) => r.type === ReferenceType.Personal
  ).length;
  const professionalCount = references.filter(
    (r) => r.type === ReferenceType.Professional
  ).length;

  const needsPersonal = requiredTypes.includes(ReferenceType.Personal);
  const needsProfessional = requiredTypes.includes(ReferenceType.Professional);

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Please provide contact information for references who can speak to
          your character and work history.
        </Typography>
        {(needsPersonal || needsProfessional) && (
          <Typography variant="body2" color="text.secondary">
            Required: {needsPersonal && `at least 1 personal reference`}
            {needsPersonal && needsProfessional && " and "}
            {needsProfessional && `at least 1 professional reference`}
          </Typography>
        )}
      </Box>

      {references.length === 0 && (
        <Paper
          variant="outlined"
          sx={{
            p: 3,
            textAlign: "center",
            backgroundColor: (theme) => alpha(theme.palette.action.hover, 0.02)
          }}
        >
          <Typography color="text.secondary" gutterBottom>
            No references added yet.
          </Typography>
          <Stack
            direction="row"
            spacing={1}
            justifyContent="center"
            sx={{ mt: 2 }}
          >
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => handleAddReference(ReferenceType.Personal)}
              size="small"
            >
              Add personal reference
            </Button>
            <Button
              variant="outlined"
              startIcon={<AddIcon />}
              onClick={() => handleAddReference(ReferenceType.Professional)}
              size="small"
            >
              Add professional reference
            </Button>
          </Stack>
        </Paper>
      )}

      {references.map((reference, index) => {
        const relationshipPlaceholder =
          reference.type === ReferenceType.Professional
            ? "e.g., Former supervisor, Colleague"
            : "e.g., Friend, Neighbor, Family member";

        return (
          <Paper
            key={reference.id}
            variant="outlined"
            sx={{
              p: 2,
              backgroundColor: (theme) =>
                reference.type === ReferenceType.Professional
                  ? alpha(theme.palette.info.main, 0.02)
                  : alpha(theme.palette.success.main, 0.02)
            }}
          >
            <Stack spacing={2}>
              <Stack
                direction="row"
                justifyContent="space-between"
                alignItems="center"
              >
                <Typography variant="subtitle2" fontWeight={600}>
                  {formatReferenceLabel(index, reference)}
                </Typography>
                {references.length > minReferences && (
                  <IconButton
                    size="small"
                    onClick={() => handleRemoveReference(reference.id)}
                    aria-label="Remove reference"
                  >
                    <DeleteOutlineIcon fontSize="small" />
                  </IconButton>
                )}
              </Stack>

              <FormControl fullWidth size="small">
                <InputLabel>Reference type</InputLabel>
                <Select
                  value={reference.type}
                  label="Reference type"
                  onChange={(e) =>
                    handleUpdateReference(reference.id, {
                      type: e.target.value as ReferenceType
                    })
                  }
                >
                  <MenuItem value={ReferenceType.Personal}>
                    Personal (friend, family, neighbor)
                  </MenuItem>
                  <MenuItem value={ReferenceType.Professional}>
                    Professional (supervisor, colleague, client)
                  </MenuItem>
                </Select>
              </FormControl>

              <TextField
                label="Full name"
                value={reference.name}
                onChange={(e) =>
                  handleUpdateReference(reference.id, {
                    name: e.target.value
                  })
                }
                fullWidth
                required
              />

              <TextField
                label="Relationship"
                value={reference.relationship}
                onChange={(e) =>
                  handleUpdateReference(reference.id, {
                    relationship: e.target.value
                  })
                }
                fullWidth
                placeholder={relationshipPlaceholder}
              />

              <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
                <TextField
                  label="Phone number"
                  value={reference.phone}
                  onChange={(e) =>
                    handleUpdateReference(reference.id, {
                      phone: e.target.value
                        .replace(/[^\d+()-\s]/g, "")
                        .slice(0, 20)
                    })
                  }
                  fullWidth
                  required
                  inputProps={{ inputMode: "tel" }}
                />
                <TextField
                  label="Email (optional)"
                  type="email"
                  value={reference.email}
                  onChange={(e) =>
                    handleUpdateReference(reference.id, {
                      email: e.target.value
                    })
                  }
                  fullWidth
                />
              </Stack>

              <TextField
                label="How many years have they known you?"
                type="number"
                value={reference.yearsKnown ?? ""}
                onChange={(e) =>
                  handleUpdateReference(reference.id, {
                    yearsKnown: e.target.value
                      ? parseInt(e.target.value, 10)
                      : null
                  })
                }
                inputProps={{ min: 0, max: 100 }}
                sx={{ maxWidth: 200 }}
              />
            </Stack>
          </Paper>
        );
      })}

      {references.length > 0 && references.length < maxReferences && (
        <Stack direction="row" spacing={1}>
          <Button
            variant="outlined"
            startIcon={<AddIcon />}
            onClick={() => handleAddReference(ReferenceType.Personal)}
            sx={{
              borderStyle: "dashed",
              color: (theme) => theme.palette.text.secondary,
              flex: 1
            }}
          >
            Add personal reference
          </Button>
          <Button
            variant="outlined"
            startIcon={<AddIcon />}
            onClick={() => handleAddReference(ReferenceType.Professional)}
            sx={{
              borderStyle: "dashed",
              color: (theme) => theme.palette.text.secondary,
              flex: 1
            }}
          >
            Add professional reference
          </Button>
        </Stack>
      )}

      {/* Summary of references by type */}
      {references.length > 0 && (
        <Box sx={{ pt: 1 }}>
          <Typography variant="caption" color="text.secondary">
            {personalCount} personal, {professionalCount} professional
            {needsPersonal && personalCount === 0 && (
              <Typography
                component="span"
                variant="caption"
                color="error"
                sx={{ ml: 1 }}
              >
                (needs personal reference)
              </Typography>
            )}
            {needsProfessional && professionalCount === 0 && (
              <Typography
                component="span"
                variant="caption"
                color="error"
                sx={{ ml: 1 }}
              >
                (needs professional reference)
              </Typography>
            )}
          </Typography>
        </Box>
      )}
    </Stack>
  );
};

export default ReferenceForm;
