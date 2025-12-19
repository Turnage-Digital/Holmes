import { useMemo } from "react";

import { IntakeSectionConfig } from "@/types/api";

/**
 * Intake section identifiers that map to form steps.
 */
export type IntakeSectionId =
  | "addresses"
  | "employment"
  | "education"
  | "references"
  | "phone";

interface SectionVisibility {
  /** Check if a specific section should be visible */
  isVisible: (section: IntakeSectionId) => boolean;
  /** List of all visible section IDs */
  visibleSections: IntakeSectionId[];
}

/**
 * Map from backend section names to frontend section IDs.
 * Backend uses PascalCase, frontend uses lowercase.
 */
const SECTION_MAP: Partial<Record<string, IntakeSectionId>> = {
  Employment: "employment",
  Education: "education",
  References: "references",
  Phone: "phone",
  // Addresses is always visible so not included here
};

/**
 * Sections that are always shown regardless of policy.
 * Addresses are required for regulatory compliance (county determination).
 */
const ALWAYS_VISIBLE: IntakeSectionId[] = ["addresses"];

/**
 * All possible sections in the order they should appear.
 */
const ALL_SECTIONS: IntakeSectionId[] = [
  "addresses",
  "employment",
  "education",
  "references",
];

/**
 * Hook to determine which intake form sections should be visible
 * based on the section configuration from the backend.
 *
 * @param config - Section configuration from bootstrap response
 * @returns Object with isVisible function and list of visible sections
 */
export function useSectionVisibility(
  config: IntakeSectionConfig | undefined,
): SectionVisibility {
  return useMemo(() => {
    // If no config, show all sections (backwards compatibility)
    if (!config || config.requiredSections.length === 0) {
      return {
        isVisible: () => true,
        visibleSections: ALL_SECTIONS,
      };
    }

    const requiredSet = new Set<IntakeSectionId>(ALWAYS_VISIBLE);

    for (const section of config.requiredSections) {
      const mapped = SECTION_MAP[section];
      if (mapped) {
        requiredSet.add(mapped);
      }
    }

    // Maintain order from ALL_SECTIONS
    const visibleSections = ALL_SECTIONS.filter((s) => requiredSet.has(s));

    return {
      isVisible: (section: IntakeSectionId) => requiredSet.has(section),
      visibleSections,
    };
  }, [config]);
}
