import React, { useState } from "react";

import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import BusinessIcon from "@mui/icons-material/Business";
import CategoryIcon from "@mui/icons-material/Category";
import LayersIcon from "@mui/icons-material/Layers";
import {
  Alert,
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  IconButton,
  Stack,
  Tab,
  Tabs,
  Typography,
} from "@mui/material";
import { format } from "date-fns";
import { useNavigate, useParams } from "react-router-dom";

import {
  ServiceCatalogEditor,
  TierConfigurationEditor,
} from "@/components/customers";
import { useCustomer, useCustomerCatalog } from "@/hooks/api";
import { getCustomerStatusColor, getCustomerStatusLabel } from "@/lib/status";

// ============================================================================
// Tab Panel Component
// ============================================================================

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel = ({ children, value, index }: TabPanelProps) => (
  <div role="tabpanel" hidden={value !== index}>
    {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
  </div>
);

// ============================================================================
// Overview Tab Content
// ============================================================================

interface OverviewTabProps {
  customer: NonNullable<ReturnType<typeof useCustomer>["data"]>;
}

const OverviewTab = ({ customer }: OverviewTabProps) => (
  <Stack spacing={3}>
    {/* Basic Info Card */}
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Details
        </Typography>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: "160px 1fr",
            gap: 1.5,
          }}
        >
          <Typography variant="body2" color="text.secondary">
            Customer ID
          </Typography>
          <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
            {customer.id}
          </Typography>

          <Typography variant="body2" color="text.secondary">
            Tenant ID
          </Typography>
          <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
            {customer.tenantId}
          </Typography>

          <Typography variant="body2" color="text.secondary">
            Policy Snapshot
          </Typography>
          <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
            {customer.policySnapshotId}
          </Typography>

          <Typography variant="body2" color="text.secondary">
            Billing Email
          </Typography>
          <Typography variant="body2">
            {customer.billingEmail || "—"}
          </Typography>

          <Typography variant="body2" color="text.secondary">
            Created
          </Typography>
          <Typography variant="body2">
            {format(new Date(customer.createdAt), "MMM d, yyyy 'at' h:mm a")}
          </Typography>

          <Typography variant="body2" color="text.secondary">
            Updated
          </Typography>
          <Typography variant="body2">
            {format(new Date(customer.updatedAt), "MMM d, yyyy 'at' h:mm a")}
          </Typography>
        </Box>
      </CardContent>
    </Card>

    {/* Contacts Card */}
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Contacts ({customer.contacts.length})
        </Typography>
        {customer.contacts.length === 0 ? (
          <Typography color="text.secondary">
            No contacts configured.
          </Typography>
        ) : (
          <Stack spacing={2}>
            {customer.contacts.map((contact) => (
              <Box
                key={contact.id}
                sx={{
                  p: 2,
                  borderRadius: 1,
                  bgcolor: "grey.50",
                }}
              >
                <Typography variant="subtitle2">{contact.name}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {contact.email}
                </Typography>
                {contact.phone && (
                  <Typography variant="body2" color="text.secondary">
                    {contact.phone}
                  </Typography>
                )}
                {contact.role && (
                  <Chip
                    label={contact.role}
                    size="small"
                    variant="outlined"
                    sx={{ mt: 1 }}
                  />
                )}
              </Box>
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>

    {/* Admins Card */}
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Administrators ({customer.admins.length})
        </Typography>
        {customer.admins.length === 0 ? (
          <Typography color="text.secondary">
            No administrators assigned.
          </Typography>
        ) : (
          <Stack spacing={1}>
            {customer.admins.map((admin) => (
              <Box
                key={admin.userId}
                sx={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                  p: 1.5,
                  borderRadius: 1,
                  bgcolor: "grey.50",
                }}
              >
                <Typography variant="body2" sx={{ fontFamily: "monospace" }}>
                  {admin.userId}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  Assigned {format(new Date(admin.assignedAt), "MMM d, yyyy")}
                </Typography>
              </Box>
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>
  </Stack>
);

// ============================================================================
// Customer Detail Page
// ============================================================================

const CustomerDetailPage = () => {
  const navigate = useNavigate();
  const { customerId } = useParams<{ customerId: string }>();
  const [activeTab, setActiveTab] = useState(0);

  // Fetch customer data
  const {
    data: customer,
    isLoading: customerLoading,
    error: customerError,
  } = useCustomer(customerId!);

  // Fetch service catalog
  const {
    data: catalog,
    isLoading: catalogLoading,
    error: catalogError,
  } = useCustomerCatalog(customerId!);

  // Loading state
  if (customerLoading) {
    return (
      <Box
        sx={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          minHeight: 400,
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  // Error state
  if (customerError || !customer) {
    return (
      <Box>
        <IconButton
          onClick={() => navigate("/customers")}
          sx={{ mb: 2 }}
          size="small"
        >
          <ArrowBackIcon />
        </IconButton>
        <Alert severity="error">
          {customerError
            ? "Failed to load customer. Please try again."
            : "Customer not found."}
        </Alert>
      </Box>
    );
  }

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  return (
    <Box>
      {/* Back button */}
      <IconButton
        onClick={() => navigate("/customers")}
        sx={{ mb: 2 }}
        size="small"
      >
        <ArrowBackIcon />
      </IconButton>

      {/* Header */}
      <Box sx={{ mb: 3 }}>
        <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 1 }}>
          <Typography variant="h4" component="h1">
            {customer.name}
          </Typography>
          <Chip
            label={getCustomerStatusLabel(customer.status)}
            color={getCustomerStatusColor(customer.status)}
            size="small"
            variant="outlined"
          />
        </Stack>
        <Typography variant="body2" color="text.secondary">
          {customer.contacts.length} contact
          {customer.contacts.length !== 1 ? "s" : ""} • Policy{" "}
          {customer.policySnapshotId.slice(0, 12)}…
        </Typography>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
        <Tabs value={activeTab} onChange={handleTabChange}>
          <Tab icon={<BusinessIcon />} iconPosition="start" label="Overview" />
          <Tab
            icon={<CategoryIcon />}
            iconPosition="start"
            label="Service Catalog"
          />
          <Tab
            icon={<LayersIcon />}
            iconPosition="start"
            label="Tier Configuration"
          />
        </Tabs>
      </Box>

      {/* Tab Content */}
      <TabPanel value={activeTab} index={0}>
        <OverviewTab customer={customer} />
      </TabPanel>

      <TabPanel value={activeTab} index={1}>
        {catalogLoading ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress />
          </Box>
        ) : catalogError ? (
          <Alert severity="error">
            Failed to load service catalog. Please try again.
          </Alert>
        ) : catalog ? (
          <ServiceCatalogEditor
            customerId={customer.id}
            services={catalog.services}
          />
        ) : (
          <Alert severity="info">
            No service catalog configured for this customer.
          </Alert>
        )}
      </TabPanel>

      <TabPanel value={activeTab} index={2}>
        {catalogLoading ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress />
          </Box>
        ) : catalogError ? (
          <Alert severity="error">
            Failed to load tier configuration. Please try again.
          </Alert>
        ) : catalog ? (
          <TierConfigurationEditor
            customerId={customer.id}
            tiers={catalog.tiers}
            availableServices={catalog.services}
          />
        ) : (
          <Alert severity="info">
            No tier configuration for this customer.
          </Alert>
        )}
      </TabPanel>
    </Box>
  );
};

export default CustomerDetailPage;
