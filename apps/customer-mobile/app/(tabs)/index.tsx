import { router } from "expo-router";
import React, { useMemo, useState } from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Screen, SearchInput, SectionHeading, uiStyles } from "../../src/components/ui";
import { categories, demoNotice, listings } from "../../src/data/demo";
import { useAppState } from "../../src/state/AppProvider";
import { colors, radii, spacing } from "../../src/theme/tokens";

export default function HomeScreen() {
  const { language, setLanguage, favorites, toggleFavorite } = useAppState();
  const [query, setQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const isAr = language === "ar";
  const t = isAr
    ? { location: "موقعك الحالي", city: "دكرنس، الدقهلية", greeting: "كل اللي تحتاجه… أقرب ليك", subtitle: "تسوّق واكتشف خدمات منطقتك من مكان واحد", categories: "اكتشف الأقسام", all: "عرض الكل", featured: "اختيارات مميزة", search: "بتدور على إيه؟ متجر، خدمة، منتج...", demo: "بيانات تجريبية" }
    : { location: "Your location", city: "Dekernes, Dakahlia", greeting: "Everything you need, closer", subtitle: "Discover local shops and services in one place", categories: "Explore categories", all: "View all", featured: "Featured nearby", search: "Search shops, services, products...", demo: "Demo data" };
  const filtered = useMemo(() => listings.filter((item) => {
    const haystack = isAr ? item.nameAr + " " + item.categoryAr : item.nameEn + " " + item.categoryEn;
    const matchesQuery = haystack.toLowerCase().includes(query.trim().toLowerCase());
    const matchesCategory = !selectedCategory || item.categoryId === selectedCategory;
    return matchesQuery && matchesCategory;
  }), [isAr, query, selectedCategory]);

  return <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
    <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
      <View style={styles.hero}>
        <View style={[styles.topRow, { flexDirection: isAr ? "row-reverse" : "row" }]}>
          <View style={{ flex: 1, gap: 4 }}>
            <AppText style={styles.locationLabel}>{t.location}</AppText>
            <AppText style={styles.locationValue}>⌖  {t.city}  ▾</AppText>
          </View>
          <Pressable onPress={() => setLanguage(isAr ? "en" : "ar")} accessibilityRole="button" style={styles.languageButton}>
            <AppText style={styles.languageText}>{isAr ? "EN" : "عربي"}</AppText>
          </Pressable>
        </View>
        <AppText style={styles.greeting}>{t.greeting}</AppText>
        <AppText style={styles.subtitle}>{t.subtitle}</AppText>
        <SearchInput value={query} onChangeText={setQuery} placeholder={t.search} returnKeyType="search" />
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.quickFilters}>
          <Pressable onPress={() => setSelectedCategory(null)} style={[styles.filterChip, selectedCategory === null && styles.filterChipActive]}>
            <AppText style={[styles.filterChipText, selectedCategory === null && styles.filterChipTextActive]}>{isAr ? "الكل" : "All"}</AppText>
          </Pressable>
          {categories.map((category) => (
            <Pressable key={category.id} onPress={() => setSelectedCategory(selectedCategory === category.id ? null : category.id)} style={[styles.filterChip, selectedCategory === category.id && styles.filterChipActive]}>
              <AppText style={[styles.filterChipText, selectedCategory === category.id && styles.filterChipTextActive]}>{category.icon} {isAr ? category.nameAr : category.nameEn}</AppText>
            </Pressable>
          ))}
        </ScrollView>
      </View>

      <View style={styles.demoNotice}><AppText style={styles.demoNoticeText}>ⓘ {demoNotice[language]}</AppText></View>

      <View style={styles.section}>
        <SectionHeading title={t.categories} action={t.all} onAction={() => router.push("/(tabs)/categories")} />
        <View style={styles.categoryGrid}>
          {categories.slice(0, 6).map((category) => (
            <Pressable key={category.id} accessibilityRole="button" onPress={() => router.push({ pathname: "/category/[id]", params: { id: category.id } })} style={({ pressed }) => [styles.categoryCard, pressed && { opacity: 0.75 }]}>
              <View style={styles.categoryIcon}><AppText style={{ fontSize: 25 }}>{category.icon}</AppText></View>
              <AppText style={styles.categoryName}>{isAr ? category.nameAr : category.nameEn}</AppText>
            </Pressable>
          ))}
        </View>
      </View>

      <View style={styles.section}>
        <SectionHeading title={t.featured} action={t.all} onAction={() => router.push("/(tabs)/categories")} />
        <View style={{ gap: spacing.md }}>
          {filtered.map((item) => (
            <Pressable key={item.id} onPress={() => router.push({ pathname: "/listing/[id]", params: { id: item.id } })} style={uiStyles.card}>
              <View style={[styles.listingRow, { flexDirection: isAr ? "row-reverse" : "row" } ]}>
                <View style={styles.listingIcon}><AppText style={{ fontSize: 28 }}>{item.icon}</AppText></View>
                <View style={{ flex: 1, gap: 5 }}>
                  <AppText style={styles.listingName}>{isAr ? item.nameAr : item.nameEn}</AppText>
                  <AppText style={uiStyles.muted}>{isAr ? item.categoryAr : item.categoryEn} · {isAr ? item.distanceAr : item.distanceEn}</AppText>
                  <AppText style={styles.rating}>★ {item.rating} <AppText style={uiStyles.muted}>({item.reviews})</AppText></AppText>
                  <AppText style={styles.price}>{isAr ? item.priceAr : item.priceEn}</AppText>
                </View>
                <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "إضافة للمفضلة" : "Save to favorites"} hitSlop={8} onPress={(event) => { event.stopPropagation(); toggleFavorite(item.id); }} style={styles.favoriteButton}>
                  <AppText style={{ fontSize: 22, color: favorites.includes(item.id) ? colors.danger : colors.muted }}>{favorites.includes(item.id) ? "♥" : "♡"}</AppText>
                </Pressable>
              </View>
            </Pressable>
          ))}
          {filtered.length === 0 ? <View style={uiStyles.card}>
            <AppText>{isAr ? "مفيش نتائج مطابقة للبحث أو القسم المختار." : "No listings match your search or selected category."}</AppText>
            <Pressable onPress={() => { setQuery(""); setSelectedCategory(null); }} style={styles.clearFilters}>
              <AppText style={styles.clearFiltersText}>{isAr ? "مسح البحث والفلاتر" : "Clear search and filters"}</AppText>
            </Pressable>
          </View> : null}
        </View>
      </View>
      <AppText style={styles.footer}>{t.demo}</AppText>
    </ScrollView>
  </Screen>;
}

const styles = StyleSheet.create({
  content: { paddingBottom: 24, gap: spacing.lg },
  quickFilters: { gap: spacing.sm, paddingTop: 2 },
  filterChip: { borderWidth: 1, borderColor: "#83BFB5", borderRadius: radii.pill, paddingVertical: 8, paddingHorizontal: 12, backgroundColor: "#FFFFFF14" },
  filterChipActive: { backgroundColor: "#FFFFFF", borderColor: "#FFFFFF" },
  filterChipText: { color: "#FFFFFF", fontSize: 11, fontWeight: "700" },
  filterChipTextActive: { color: colors.primaryDark },
  clearFilters: { alignSelf: "flex-start", paddingTop: spacing.md },
  clearFiltersText: { color: colors.primary, fontWeight: "800", fontSize: 12 },
  hero: { backgroundColor: colors.primary, paddingHorizontal: spacing.lg, paddingTop: 20, paddingBottom: 24, borderBottomLeftRadius: 28, borderBottomRightRadius: 28, gap: spacing.md },
  topRow: { flexDirection: "row", alignItems: "center", gap: spacing.md, marginBottom: 8 },
  locationLabel: { color: "#C8E9E3", fontSize: 11 },
  locationValue: { color: "#FFFFFF", fontWeight: "700", fontSize: 14 },
  languageButton: { paddingHorizontal: 12, paddingVertical: 8, borderRadius: radii.pill, borderColor: "#83BFB5", borderWidth: 1 },
  languageText: { color: "#FFFFFF", fontWeight: "800", fontSize: 12 },
  greeting: { color: "#FFFFFF", fontSize: 24, lineHeight: 33, fontWeight: "900" },
  subtitle: { color: "#D9F0EB", fontSize: 13, lineHeight: 20 },
  demoNotice: { marginHorizontal: spacing.lg, padding: spacing.md, backgroundColor: "#FFF4E4", borderRadius: radii.sm },
  demoNoticeText: { color: "#79521F", fontSize: 11, lineHeight: 17 },
  section: { paddingHorizontal: spacing.lg, gap: spacing.md },
  categoryGrid: { flexDirection: "row", flexWrap: "wrap", gap: spacing.sm },
  categoryCard: { userSelect: "none", width: "31.5%", minHeight: 105, backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: radii.md, alignItems: "center", justifyContent: "center", padding: 8, gap: 8 },
  categoryIcon: { width: 48, height: 48, borderRadius: 16, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  categoryName: { fontSize: 11, fontWeight: "700", textAlign: "center", lineHeight: 16 },
  listingRow: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  listingIcon: { width: 62, height: 62, borderRadius: 18, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  listingName: { fontSize: 14, fontWeight: "800" },
  rating: { color: "#D88A20", fontWeight: "800", fontSize: 12 },
  price: { color: colors.primaryDark, fontWeight: "700", fontSize: 12 },
  favoriteButton: { padding: 4 },
  footer: { color: colors.muted, textAlign: "center", fontSize: 10, paddingHorizontal: spacing.lg },
});