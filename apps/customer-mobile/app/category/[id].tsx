import { router, useLocalSearchParams } from "expo-router";
import React from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Screen, uiStyles } from "../../src/components/ui";
import { categories, listings } from "../../src/data/demo";
import { useAppState } from "../../src/state/AppProvider";
import { colors, radii, spacing } from "../../src/theme/tokens";

export default function CategoryDetailsScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { language } = useAppState();
  const isAr = language === "ar";
  const category = categories.find((item) => item.id === id);
  const categoryListings = listings.filter((item) => item.categoryId === id);

  if (!category) {
    return (
      <Screen>
        <AppText>{isAr ? "القسم غير موجود." : "Category not found."}</AppText>
        <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.backButton}>
          <AppText style={styles.backText}>{isAr ? "رجوع" : "Go back"}</AppText>
        </Pressable>
      </Screen>
    );
  }

  return (
    <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
      <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
        <View style={styles.hero}>
          <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "رجوع" : "Go back"} onPress={() => router.back()} style={styles.backButton}>
            <AppText style={styles.backText}>‹</AppText>
          </Pressable>
          <View style={styles.categoryIcon}><AppText style={{ fontSize: 38 }}>{category.icon}</AppText></View>
          <AppText selectable={false} style={styles.title}>{isAr ? category.nameAr : category.nameEn}</AppText>
          <AppText selectable={false} style={styles.subtitle}>{isAr ? category.descriptionAr : category.descriptionEn}</AppText>
        </View>

        <View style={styles.section}>
          <AppText style={styles.sectionTitle}>{isAr ? "الأنشطة المتاحة" : "Available listings"}</AppText>
          {categoryListings.length > 0 ? categoryListings.map((item) => (
            <Pressable
              key={item.id}
              accessibilityRole="button"
              onPress={() => router.push({ pathname: "/listing/[id]", params: { id: item.id } })}
              style={({ pressed }) => [uiStyles.card, styles.listingCard, pressed && styles.pressed]}
            >
              <View style={[styles.listingRow, { flexDirection: isAr ? "row-reverse" : "row" }]}>
                <View style={styles.listingIcon}><AppText style={{ fontSize: 28 }}>{item.icon}</AppText></View>
                <View style={{ flex: 1, gap: 5 }}>
                  <AppText selectable={false} style={styles.listingName}>{isAr ? item.nameAr : item.nameEn}</AppText>
                  <AppText selectable={false} style={styles.muted}>{isAr ? item.distanceAr : item.distanceEn} · ★ {item.rating}</AppText>
                  <AppText selectable={false} style={styles.price}>{isAr ? item.priceAr : item.priceEn}</AppText>
                </View>
                <AppText selectable={false} style={styles.chevron}>‹</AppText>
              </View>
            </Pressable>
          )) : (
            <View style={uiStyles.card}>
              <AppText>{isAr ? "مفيش أنشطة مسجلة في القسم ده حاليًا. هنضيفها لما تتوفر بيانات حقيقية." : "No listings are available in this category yet."}</AppText>
            </View>
          )}
          <AppText style={styles.notice}>{isAr ? "المحتوى الحالي تجريبي، والأنشطة والأسعار غير مؤكدة." : "Demo content only. Listings and prices are not verified."}</AppText>
        </View>
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  content: { paddingBottom: 28, gap: spacing.lg },
  hero: { minHeight: 230, backgroundColor: colors.primary, borderBottomLeftRadius: 28, borderBottomRightRadius: 28, alignItems: "center", justifyContent: "center", padding: spacing.xl, paddingTop: 64, gap: spacing.sm },
  backButton: { position: "absolute", top: 18, left: 18, minWidth: 42, minHeight: 42, borderRadius: 21, backgroundColor: "#FFFFFF25", alignItems: "center", justifyContent: "center", paddingHorizontal: 12 },
  backText: { color: "#FFFFFF", fontSize: 24, fontWeight: "800" },
  categoryIcon: { width: 72, height: 72, borderRadius: 22, backgroundColor: "#FFFFFF20", alignItems: "center", justifyContent: "center" },
  title: { color: "#FFFFFF", fontSize: 23, fontWeight: "900", textAlign: "center" },
  subtitle: { color: "#D9F0EB", fontSize: 13, lineHeight: 20, textAlign: "center" },
  section: { paddingHorizontal: spacing.lg, gap: spacing.md },
  sectionTitle: { fontSize: 18, fontWeight: "900" },
  listingCard: { userSelect: "none" },
  listingRow: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  listingIcon: { width: 58, height: 58, borderRadius: 16, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  listingName: { fontSize: 14, fontWeight: "800" },
  muted: { color: colors.muted, fontSize: 12 },
  price: { color: colors.primaryDark, fontSize: 12, fontWeight: "800" },
  chevron: { color: colors.primary, fontSize: 24 },
  notice: { color: colors.muted, fontSize: 10, textAlign: "center", lineHeight: 16 },
  pressed: { opacity: 0.75 },
});
