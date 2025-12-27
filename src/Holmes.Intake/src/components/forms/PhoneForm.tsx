import React from "react";

import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import {
  Box,
  Button,
  Checkbox,
  FormControl,
  FormControlLabel,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { alpha } from "@mui/material/styles";

import { createEmptyPhone, IntakePhone, PhoneType } from "@/types/intake";

interface PhoneFormProps {
  phones: IntakePhone[];
  onChange: (phones: IntakePhone[]) => void;
  minPhones?: number;
  maxPhones?: number;
}

const PhoneForm: React.FC<PhoneFormProps> = ({
  phones,
  onChange,
  minPhones = 1,
  maxPhones = 5,
}) => {
  const handleAddPhone = () => {
    if (phones.length >= maxPhones) return;
    const newPhone = createEmptyPhone();
    // If this is the first phone, make it primary
    if (phones.length === 0) {
      newPhone.isPrimary = true;
    }
    onChange([...phones, newPhone]);
  };

  const handleRemovePhone = (id: string) => {
    if (phones.length <= minPhones) return;
    const remaining = phones.filter((p) => p.id !== id);
    // If we removed the primary, make the first remaining phone primary
    if (remaining.length > 0 && !remaining.some((p) => p.isPrimary)) {
      remaining[0].isPrimary = true;
    }
    onChange(remaining);
  };

  const handleUpdatePhone = (id: string, updates: Partial<IntakePhone>) => {
    onChange(
      phones.map((p) => {
        if (p.id !== id) {
          // If setting another phone as primary, unset this one
          if (updates.isPrimary === true) {
            return { ...p, isPrimary: false };
          }
          return p;
        }
        return { ...p, ...updates };
      }),
    );
  };

  const formatPhoneLabel = (index: number, phone: IntakePhone) => {
    const typeLabels: Record<PhoneType, string> = {
      [PhoneType.Mobile]: "Mobile",
      [PhoneType.Home]: "Home",
      [PhoneType.Work]: "Work",
    };
    const typeLabel = typeLabels[phone.type];
    const primarySuffix = phone.isPrimary ? " (Primary)" : "";
    if (phone.phoneNumber) {
      return `${phone.phoneNumber} - ${typeLabel}${primarySuffix}`;
    }
    return `${typeLabel} Phone ${index + 1}${primarySuffix}`;
  };

  const renderSummary = () => {
    if (phones.length === 0) {
      return null;
    }

    const plural = phones.length === 1 ? "" : "s";
    const hasPrimary = phones.some((p) => p.isPrimary);
    const primaryWarning = hasPrimary ? null : (
      <Typography
        component="span"
        variant="caption"
        color="error"
        sx={{ ml: 1 }}
      >
        (please mark one as primary)
      </Typography>
    );

    return (
      <Box sx={{ pt: 1 }}>
        <Typography variant="caption" color="text.secondary">
          {phones.length} phone number{plural} added
          {primaryWarning}
        </Typography>
      </Box>
    );
  };

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Add phone numbers where you can be reached. Mark one as your primary
          contact number.
        </Typography>
      </Box>

      {phones.length === 0 && (
        <Paper
          variant="outlined"
          sx={{
            p: 3,
            textAlign: "center",
            backgroundColor: (theme) => alpha(theme.palette.action.hover, 0.02),
          }}
        >
          <Typography color="text.secondary" gutterBottom>
            No phone numbers added yet.
          </Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={handleAddPhone}
            size="small"
            sx={{ mt: 2 }}
          >
            Add phone number
          </Button>
        </Paper>
      )}

      {phones.map((phone, index) => (
        <Paper
          key={phone.id}
          variant="outlined"
          sx={{
            p: 2,
            backgroundColor: (theme) =>
              phone.isPrimary
                ? alpha(theme.palette.primary.main, 0.02)
                : alpha(theme.palette.action.hover, 0.02),
          }}
        >
          <Stack spacing={2}>
            <Stack
              direction="row"
              justifyContent="space-between"
              alignItems="center"
            >
              <Typography variant="subtitle2" fontWeight={600}>
                {formatPhoneLabel(index, phone)}
              </Typography>
              {phones.length > minPhones && (
                <IconButton
                  size="small"
                  onClick={() => handleRemovePhone(phone.id)}
                  aria-label="Remove phone"
                >
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              )}
            </Stack>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                label="Phone number"
                value={phone.phoneNumber}
                onChange={(e) =>
                  handleUpdatePhone(phone.id, {
                    phoneNumber: e.target.value
                      .replace(/[^\d+()-\s]/g, "")
                      .slice(0, 20),
                  })
                }
                fullWidth
                required
                inputProps={{ inputMode: "tel" }}
                placeholder="(555) 123-4567"
              />

              <FormControl sx={{ minWidth: 140 }} size="small">
                <InputLabel>Type</InputLabel>
                <Select
                  value={phone.type}
                  label="Type"
                  onChange={(e) =>
                    handleUpdatePhone(phone.id, {
                      type: e.target.value as PhoneType,
                    })
                  }
                >
                  <MenuItem value={PhoneType.Mobile}>Mobile</MenuItem>
                  <MenuItem value={PhoneType.Home}>Home</MenuItem>
                  <MenuItem value={PhoneType.Work}>Work</MenuItem>
                </Select>
              </FormControl>
            </Stack>

            <FormControlLabel
              control={
                <Checkbox
                  checked={phone.isPrimary}
                  onChange={(e) =>
                    handleUpdatePhone(phone.id, {
                      isPrimary: e.target.checked,
                    })
                  }
                  disabled={phone.isPrimary && phones.length > 1}
                />
              }
              label="Primary contact number"
            />
          </Stack>
        </Paper>
      ))}

      {phones.length > 0 && phones.length < maxPhones && (
        <Button
          variant="outlined"
          startIcon={<AddIcon />}
          onClick={handleAddPhone}
          sx={{
            borderStyle: "dashed",
            color: (theme) => theme.palette.text.secondary,
          }}
        >
          Add another phone number
        </Button>
      )}

      {/* Summary */}
      {renderSummary()}
    </Stack>
  );
};

export default PhoneForm;
