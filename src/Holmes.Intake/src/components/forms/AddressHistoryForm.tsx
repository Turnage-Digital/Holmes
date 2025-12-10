import React from "react";

import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import {
  Box,
  Button,
  Checkbox,
  Divider,
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

import {
  AddressType,
  createEmptyAddress,
  IntakeAddress,
  US_STATES,
} from "@/types/intake";

interface AddressHistoryFormProps {
  addresses: IntakeAddress[];
  onChange: (addresses: IntakeAddress[]) => void;
  minAddresses?: number;
  maxAddresses?: number;
  yearsRequired?: number;
}

const AddressHistoryForm: React.FC<AddressHistoryFormProps> = ({
  addresses,
  onChange,
  minAddresses = 1,
  maxAddresses = 10,
  yearsRequired = 7,
}) => {
  const handleAddAddress = () => {
    if (addresses.length >= maxAddresses) return;
    onChange([...addresses, createEmptyAddress()]);
  };

  const handleRemoveAddress = (id: string) => {
    if (addresses.length <= minAddresses) return;
    onChange(addresses.filter((a) => a.id !== id));
  };

  const handleUpdateAddress = (id: string, updates: Partial<IntakeAddress>) => {
    onChange(
      addresses.map((a) => {
        if (a.id !== id) return a;
        const updated = { ...a, ...updates };
        // If marking as current, clear the toDate
        if (updates.isCurrent === true) {
          updated.toDate = "";
        }
        return updated;
      }),
    );
  };

  const formatAddressLabel = (index: number, address: IntakeAddress) => {
    if (address.isCurrent) return "Current Address";
    if (address.city && address.state) {
      return `${address.city}, ${address.state}`;
    }
    return `Address ${index + 1}`;
  };

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="body2" color="text.secondary" gutterBottom>
          Please provide your address history for the past {yearsRequired}{" "}
          years. Start with your current address.
        </Typography>
      </Box>

      {addresses.map((address, index) => (
        <Paper
          key={address.id}
          variant="outlined"
          sx={{
            p: 2,
            backgroundColor: (theme) =>
              address.isCurrent
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
                {formatAddressLabel(index, address)}
              </Typography>
              {addresses.length > minAddresses && (
                <IconButton
                  size="small"
                  onClick={() => handleRemoveAddress(address.id)}
                  aria-label="Remove address"
                >
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              )}
            </Stack>

            <FormControlLabel
              control={
                <Checkbox
                  checked={address.isCurrent}
                  onChange={(e) =>
                    handleUpdateAddress(address.id, {
                      isCurrent: e.target.checked,
                    })
                  }
                />
              }
              label="This is my current address"
            />

            <TextField
              label="Street address"
              value={address.street1}
              onChange={(e) =>
                handleUpdateAddress(address.id, { street1: e.target.value })
              }
              fullWidth
              required
            />

            <TextField
              label="Apartment, suite, unit (optional)"
              value={address.street2}
              onChange={(e) =>
                handleUpdateAddress(address.id, { street2: e.target.value })
              }
              fullWidth
            />

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                label="City"
                value={address.city}
                onChange={(e) =>
                  handleUpdateAddress(address.id, { city: e.target.value })
                }
                fullWidth
                required
              />
              <FormControl fullWidth required>
                <InputLabel>State</InputLabel>
                <Select
                  value={address.state}
                  label="State"
                  onChange={(e) =>
                    handleUpdateAddress(address.id, {
                      state: e.target.value as string,
                    })
                  }
                >
                  {US_STATES.map((state) => (
                    <MenuItem key={state.value} value={state.value}>
                      {state.label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField
                label="ZIP code"
                value={address.postalCode}
                onChange={(e) =>
                  handleUpdateAddress(address.id, {
                    postalCode: e.target.value
                      .replace(/[^\dA-Za-z-]/g, "")
                      .slice(0, 10),
                  })
                }
                fullWidth
                required
                inputProps={{ maxLength: 10 }}
              />
            </Stack>

            <Divider sx={{ my: 1 }} />

            <Typography variant="body2" color="text.secondary">
              When did you live at this address?
            </Typography>

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5}>
              <TextField
                type="date"
                label="From date"
                value={address.fromDate}
                onChange={(e) =>
                  handleUpdateAddress(address.id, { fromDate: e.target.value })
                }
                fullWidth
                required
                InputLabelProps={{ shrink: true }}
              />
              {!address.isCurrent && (
                <TextField
                  type="date"
                  label="To date"
                  value={address.toDate}
                  onChange={(e) =>
                    handleUpdateAddress(address.id, { toDate: e.target.value })
                  }
                  fullWidth
                  required
                  InputLabelProps={{ shrink: true }}
                />
              )}
            </Stack>

            <FormControl fullWidth size="small">
              <InputLabel>Address type</InputLabel>
              <Select
                value={address.type}
                label="Address type"
                onChange={(e) =>
                  handleUpdateAddress(address.id, {
                    type: e.target.value as AddressType,
                  })
                }
              >
                <MenuItem value={AddressType.Residential}>Residential</MenuItem>
                <MenuItem value={AddressType.Mailing}>Mailing</MenuItem>
                <MenuItem value={AddressType.Business}>Business</MenuItem>
              </Select>
            </FormControl>
          </Stack>
        </Paper>
      ))}

      {addresses.length < maxAddresses && (
        <Button
          variant="outlined"
          startIcon={<AddIcon />}
          onClick={handleAddAddress}
          sx={{
            borderStyle: "dashed",
            color: (theme) => theme.palette.text.secondary,
          }}
        >
          Add another address
        </Button>
      )}
    </Stack>
  );
};

export default AddressHistoryForm;
