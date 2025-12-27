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
  Typography
} from "@mui/material";
import { format } from "date-fns";
import { useNavigate, useParams } from "react-router-dom";

import { ServiceCatalogEditor, TierConfigurationEditor } from "@/components/customers";
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
            gap: 1.5
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
        {(() => {
          const contactsDisplay =
            customer.contacts.length > 0 ? (
              <Stack spacing={2}>
                {customer.contacts.map((contact) => {
                  const phoneDisplay = contact.phone ? (
                    <Typography variant="body2" color="text.secondary">
                      {contact.phone}
                    </Typography>
                  ) : null;
                  const roleDisplay = contact.role ? (
                    <Chip
                      label={contact.role}
                      size="small"
                      variant="outlined"
                      sx={{ mt: 1 }}
                    />
                  ) : null;
                  return (
                    <Box
                      key={contact.id}
                      sx={{
                        p: 2,
                        borderRadius: 1,
                        bgcolor: "grey.50"
                      }}
                    >
                      <Typography variant="subtitle2">
                        {contact.name}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {contact.email}
                      </Typography>
                      {phoneDisplay}
                      {roleDisplay}
                    </Box>
                  );
                })}
              </Stack>
            ) : (
              <Typography color="text.secondary">
                No contacts configured.
              </Typography>
            );
          return contactsDisplay;
        })()}
      </CardContent>
    </Card>

    {/* Admins Card */}
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Administrators ({customer.admins.length})
        </Typography>
        {(() => {
          const adminsDisplay =
            customer.admins.length > 0 ? (
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
                      bgcolor: "grey.50"
                    }}
                  >
                    <Typography
                      variant="body2"
                      sx={{ fontFamily: "monospace" }}
                    >
                      {admin.userId}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Assigned{" "}
                      {format(new Date(admin.assignedAt), "MMM d, yyyy")}
                    </Typography>
                  </Box>
                ))}
              </Stack>
            ) : (
              <Typography color="text.secondary">
                No administrators assigned.
              </Typography>
            );
          return adminsDisplay;
        })()}
      </CardContent>
    </Card>
  </Stack>
);

// ============================================================================
// Service Catalog Content Component
// ============================================================================

interface CatalogContentProps {
  customerId: string;
  catalog: ReturnType<typeof useCustomerCatalog>["data"];
  catalogLoading: boolean;
  catalogError: ReturnType<typeof useCustomerCatalog>["error"];
}

const ServiceCatalogContent = ({
                                 customerId,
                                 catalog,
                                 catalogLoading,
                                 catalogError
                               }: CatalogContentProps) => {
  if (catalogLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (catalog) {
    return (
      <ServiceCatalogEditor
        customerId={customerId}
        services={catalog.services}
      />
    );
  }

  if (catalogError) {
    return (
      <Alert severity="error">
        Failed to load service catalog. Please try again.
      </Alert>
    );
  }

  return (
    <Alert severity="info">
      No service catalog configured for this customer.
    </Alert>
  );
};

// ============================================================================
// Tier Configuration Content Component
// ============================================================================

const TierConfigurationContent = ({
                                    customerId,
                                    catalog,
                                    catalogLoading,
                                    catalogError
                                  }: CatalogContentProps) => {
  if (catalogLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (catalog) {
    return (
      <TierConfigurationEditor
        customerId={customerId}
        tiers={catalog.tiers}
        availableServices={catalog.services}
      />
    );
  }

  if (catalogError) {
    return (
      <Alert severity="error">
        Failed to load tier configuration. Please try again.
      </Alert>
    );
  }

  return (
    <Alert severity="info">No tier configuration for this customer.</Alert>
  );
};

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
    error: customerError
  } = useCustomer(customerId!);

  // Fetch service catalog
  const {
    data: catalog,
    isLoading: catalogLoading,
    error: catalogError
  } = useCustomerCatalog(customerId!);

  // Loading state
  if (customerLoading) {
    return (
      <Box
        sx={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          minHeight: 400
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  // Error state
  if (customerError || !customer) {
    const errorMessage = customerError
      ? "Failed to load customer. Please try again."
      : "Customer not found.";
    return (
      <Box>
        <IconButton
          onClick={() => navigate("/customers")}
          sx={{ mb: 2 }}
          size="small"
        >
          <ArrowBackIcon />
        </IconButton>
        <Alert severity="error">{errorMessage}</Alert>
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
        {(() => {
          const contactSuffix = customer.contacts.length === 1 ? "" : "s";
          return (
            <Typography variant="body2" color="text.secondary">
              {customer.contacts.length} contact{contactSuffix} • Policy{" "}
              {customer.policySnapshotId.slice(0, 12)}…
            </Typography>
          );
        })()}
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
        <ServiceCatalogContent
          customerId={customer.id}
          catalog={catalog}
          catalogLoading={catalogLoading}
          catalogError={catalogError}
        />
      </TabPanel>

      <TabPanel value={activeTab} index={2}>
        <TierConfigurationContent
          customerId={customer.id}
          catalog={catalog}
          catalogLoading={catalogLoading}
          catalogError={catalogError}
        />
      </TabPanel>
    </Box>
  );
};

export default CustomerDetailPage;
