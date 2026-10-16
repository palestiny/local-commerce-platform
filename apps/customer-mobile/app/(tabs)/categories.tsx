import { router, useLocalSearchParams } from "expo-router";
import React, { useEffect, useMemo, useState } from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Screen, SearchInput, uiStyles } from "../../src/components/ui";
import { categories, listings } from "../../src/data/demo";
import { useAppState } from "../../src/state/AppProvider";
import { colors, radii, spacing } from "../../src/theme/tokens";

export default function CategoriesScreen() {
  const { language } = useAppState();
  const params = useLocalSearchParams<{ category?: string }>();
  const [query, setQuery] = useState("");
  const [selected, setSelected] = useState<string | null>(params.category ?? null);
  const isAr = language === "ar";
  useEffect(() => { if (params.category) setSelected(params.category); }, [params.category]);
  const visibleCategories = useMemo(() => categories.filter((item) => {
    const name = isAr ? item.nameAr : item.nameEn;
    return name.toLowerCase().includes(query.toLowerCase());
  }), [isAr, query]);
  const visibleListings = listings.filter((item) => (!selected || item.categoryId === selected) && (isAr ? item.nameAr : item.nameEn).toLowerCase().includes(query.toLowerCase()));
  return <Screen>
    <View style={styles.header}>
      <AppText style={styles.title}>{isAr ? "كل الأقسام" : "All categories"}</AppText>
      <AppText style={styles.subtitle}>{isAr ? "اختار اللي بتدور عليه" : "Find what you need"}</AppText>
    </View>
    <SearchInput value={query} onChangeText={setQuery} placeholder={isAr ? "ابحث داخل الأقسام والأنشطة" : "Search categories and listings"} />
    <ScrollView showsVerticalScrollIndicator={false} contentContainerStyle={{ gap: spacing.lg, paddingBottom: 24 }}>
      <View style={styles.grid}>
        {visibleCategories.map((item) => <Pressable key={item.id} onPress={() => setSelected(selected === item.id ? null : item.id)} style={[styles.category, selected === item.id && styles.selectedCategory]}>
          <View style={styles.icon}><AppText style={{ fontSize: 25 }}>{item.icon}</AppText></View>
          <AppText style={[styles.categoryName, selected === item.id && { color: colors.primary }]}>{isAr ? item.nameAr : item.nameEn}</AppText>
          <AppText style={styles.description}>{isAr ? item.descriptionAr : item.descriptionEn}</AppText>
        </Pressable>)}
      </View>
      <View style={{ gap: spacing.md }}>
        <AppText style={styles.sectionTitle}>{isAr ? "أنشطة مقترحة" : "Suggested listings"}</AppText>
        {visibleListings.map((item) => <Pressable key={item.id} onPress={() => router.push({ pathname: "/listing/[id]", params: { id: item.id } })} style={uiStyles.card}>
          <View style={styles.listingRow}>
            <View style={styles.listingIcon}><AppText style={{ fontSize: 25 }}>{item.icon}</AppText></View>
            <View style={{ flex: 1, gap: 5 }}>
              <AppText style={styles.categoryName}>{isAr ? item.nameAr : item.nameEn}</AppText>
              <AppText style={styles.description}>{isAr ? item.distanceAr : item.distanceEn} · ★ {item.rating}</AppText>
            </View>
            <AppText style={{ color: colors.primary, fontSize: 22 }}>‹</AppText>
          </View>
        </Pressable>)}
        {visibleListings.length === 0 ? <View style={uiStyles.card}><AppText>{isAr ? "مفيش أنشطة في القسم ده حاليًا ضمن البيانات التجريبية." : "No demo listings in this category yet."}</AppText></View> : null}
      </View>
    </ScrollView>
  </Screen>;
}

const styles = StyleSheet.create({
  header: { gap: 5 },
  title: { fontSize: 24, fontWeight: "900" },
  subtitle: { color: colors.muted, fontSize: 13 },
  grid: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
  category: { width: "48%", backgroundColor: colors.surface, borderRadius: radii.md, padding: spacing.md, borderWidth: 1, borderColor: colors.border, gap: 7 },
  selectedCategory: { borderColor: colors.primary, backgroundColor: colors.primarySoft },
  icon: { width: 48, height: 48, borderRadius: 15, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  categoryName: { fontWeight: "800", fontSize: 13 },
  description: { color: colors.muted, fontSize: 11, lineHeight: 16 },
  sectionTitle: { fontSize: 17, fontWeight: "800" },
  listingRow: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  listingIcon: { width: 54, height: 54, borderRadius: 15, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
});