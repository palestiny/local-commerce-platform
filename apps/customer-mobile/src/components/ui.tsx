import React from "react";
import { Pressable, StyleSheet, Text, TextInput, View, type TextInputProps, type ViewStyle } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { colors, radii, spacing } from "../theme/tokens";
import { useAppState } from "../state/AppProvider";

export function AppText({ children, style, ...props }: React.ComponentProps<typeof Text>) {
  const { language } = useAppState();
  return <Text {...props} style={[styles.text, { textAlign: language === "ar" ? "right" : "left", writingDirection: language === "ar" ? "rtl" : "ltr" }, style]}>{children}</Text>;
}

export function Screen({ children, style }: React.PropsWithChildren<{ style?: ViewStyle }>) {
  const insets = useSafeAreaInsets();
  return <View style={[styles.screen, { paddingTop: spacing.lg + insets.top }, style]}>{children}</View>;
}

export function SectionHeading({ title, action, onAction }: { title: string; action?: string; onAction?: () => void }) {
  const { language } = useAppState();
  return <View style={[styles.sectionHeading, { flexDirection: language === "ar" ? "row-reverse" : "row" }]}>
    <AppText style={styles.sectionTitle}>{title}</AppText>
    {action ? <Pressable onPress={onAction} hitSlop={8}><AppText style={styles.action}>{action}</AppText></Pressable> : null}
  </View>;
}

export function SearchInput(props: TextInputProps) {
  const { language } = useAppState();
  return <View style={[styles.search, { flexDirection: language === "ar" ? "row-reverse" : "row" }]}>
    <AppText style={styles.searchIcon}>⌕</AppText>
    <TextInput
      {...props}
      placeholderTextColor={colors.muted}
      style={[styles.searchInput, { textAlign: language === "ar" ? "right" : "left" }]}
      accessibilityRole="search"
    />
  </View>;
}

export function PrimaryButton({ title, onPress, disabled }: { title: string; onPress: () => void; disabled?: boolean }) {
  return <Pressable accessibilityRole="button" disabled={disabled} onPress={onPress} style={({ pressed }) => [styles.primaryButton, pressed && styles.pressed, disabled && { opacity: 0.55 }]}>
    <AppText style={styles.primaryButtonText}>{title}</AppText>
  </Pressable>;
}

export const uiStyles = StyleSheet.create({
  card: { backgroundColor: colors.surface, borderRadius: radii.md, borderWidth: 1, borderColor: colors.border, padding: spacing.md },
  muted: { color: colors.muted, fontSize: 12 },
});

const styles = StyleSheet.create({
  screen: { flex: 1, paddingHorizontal: spacing.lg, paddingTop: spacing.lg, gap: spacing.lg },
  text: { color: colors.text, fontSize: 14, includeFontPadding: false },
  sectionHeading: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", gap: spacing.sm },
  sectionTitle: { fontSize: 18, fontWeight: "800" },
  action: { color: colors.primary, fontSize: 13, fontWeight: "700" },
  search: { minHeight: 48, flexDirection: "row", alignItems: "center", gap: spacing.sm, borderWidth: 1, borderColor: colors.border, borderRadius: radii.md, backgroundColor: colors.surface, paddingHorizontal: spacing.md },
  searchIcon: { color: colors.primary, fontSize: 24 },
  searchInput: { flex: 1, minHeight: 44, color: colors.text, fontSize: 14 },
  primaryButton: { backgroundColor: colors.primary, borderRadius: radii.md, paddingVertical: 14, paddingHorizontal: spacing.lg, alignItems: "center", justifyContent: "center" },
  primaryButtonText: { color: "#FFFFFF", fontWeight: "800" },
  pressed: { opacity: 0.82, transform: [{ scale: 0.99 }] },
});
