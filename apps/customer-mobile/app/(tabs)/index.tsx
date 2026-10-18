import { router } from "expo-router";
import React, { useMemo, useState } from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { AppText, Screen, SearchInput, SectionHeading, uiStyles } from "../../src/components/ui";
import { categories, demoNotice, listings } from "../../src/data/demo";
import { useAppState } from "../../src/state/AppProvider";
import { colors, radii, spacing } from "../../src/theme/tokens";

export default function HomeScreen() {
  const { language, setLanguage, favorites, toggleFavorite } = useAppState();
  const [query, setQuery] = useState("");
  const insets = useSafeAreaInsets();
  const isAr = language === "ar";
  const t = isAr
    ? { brand: "بلدنا", greeting: "كل اللي تحتاجه… أقرب ليك", subtitle: "متاجر وخدمات وأخبار وعروض من مكان واحد", categories: "اكتشف الأقسام", all: "عرض الكل", featured: "اختيارات مميزة", search: "بتدور على إيه؟ متجر، خدمة، منتج...", demo: "بيانات تجريبية", searchResults: "نتائج البحث", offers: "عروض مميزة", news: "آخر الأخبار", sponsored: "إعلان ممول", noResults: "مفيش نتائج مطابقة لبحثك." }
    : { brand: "Baladna", greeting: "Everything you need, closer", subtitle: "Local shops, services, news and offers in one place", categories: "Explore categories", all: "View all", featured: "Featured places", search: "Search shops, services, products...", demo: "Demo data", searchResults: "Search results", offers: "Featured offers", news: "Latest news", sponsored: "Sponsored", noResults: "No results match your search." };
  const filtered = useMemo(() => {
    const normalizedQuery = normalizeSearchText(query);
    return listings.filter((item) => {
      const category = categories.find((entry) => entry.id === item.categoryId);
      const searchableText = [
        item.nameAr, item.nameEn, item.categoryAr, item.categoryEn,
        category?.nameAr, category?.nameEn,
      ].filter(Boolean).join(" ");
      return normalizeSearchText(searchableText).includes(normalizedQuery);
    });
  }, [query]);

  const renderListing = (item: (typeof listings)[number]) => (
    <Pressable key={item.id} onPress={() => router.push({ pathname: "/listing/[id]", params: { id: item.id } })} style={uiStyles.card}>
      <View style={[styles.listingRow, { flexDirection: isAr ? "row-reverse" : "row" }]}>
        <View style={styles.listingIcon}><AppText style={{ fontSize: 28 }}>{item.icon}</AppText></View>
        <View style={{ flex: 1, gap: 5 }}>
          <AppText style={styles.listingName}>{isAr ? item.nameAr : item.nameEn}</AppText>
          <AppText style={uiStyles.muted}>{isAr ? item.categoryAr : item.categoryEn} · {isAr ? item.distanceAr : item.distanceEn}</AppText>
          <AppText style={styles.rating}>★ {item.rating} <AppText style={uiStyles.muted}>({item.reviews})</AppText></AppText>
          <AppText style={styles.price}>{isAr ? item.priceAr : item.priceEn}</AppText>
        </View>
        <Pressable accessibilityRole="button" accessibilityLabel={favorites.includes(item.id) ? (isAr ? "إزالة من المفضلة" : "Remove from favorites") : (isAr ? "إضافة للمفضلة" : "Save to favorites")} hitSlop={8} onPress={(event) => { event.stopPropagation(); toggleFavorite(item.id); }} style={styles.favoriteButton}>
          <AppText style={{ fontSize: 27, lineHeight: 31, color: favorites.includes(item.id) ? colors.danger : colors.muted }}>{favorites.includes(item.id) ? "♥" : "♡"}</AppText>
        </Pressable>
      </View>
    </Pressable>
  );

  return <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
    <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false} keyboardShouldPersistTaps="handled" keyboardDismissMode="on-drag" automaticallyAdjustKeyboardInsets>
      <View style={[styles.hero, { paddingTop: Math.max(20, insets.top + 12) }]}>
        <View style={[styles.topRow, { flexDirection: isAr ? "row-reverse" : "row" }]}>
          <View style={{ flex: 1, gap: 4 }}><AppText style={styles.brand}>{t.brand}</AppText></View>
          <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "حسابي" : "My account"} onPress={() => router.push("/profile")} style={styles.profileButton}><AppText style={styles.profileGlyph}>♙</AppText></Pressable>
          <Pressable onPress={() => setLanguage(isAr ? "en" : "ar")} accessibilityRole="button" style={styles.languageButton}><AppText style={styles.languageText}>{isAr ? "EN" : "عربي"}</AppText></Pressable>
        </View>
        <AppText style={styles.greeting}>{t.greeting}</AppText>
        <AppText style={styles.subtitle}>{t.subtitle}</AppText>
        <SearchInput value={query} onChangeText={setQuery} placeholder={t.search} returnKeyType="search" />
      </View>

      {query.trim().length > 0 ? (
        <View style={styles.section}>
          <SectionHeading title={t.searchResults} />
          <View style={{ gap: spacing.md }}>
            {filtered.map(renderListing)}
            {filtered.length === 0 ? <View style={uiStyles.card}><AppText>{t.noResults}</AppText><Pressable onPress={() => setQuery("")} style={styles.clearFilters}><AppText style={styles.clearFiltersText}>{isAr ? "مسح البحث" : "Clear search"}</AppText></Pressable></View> : null}
          </View>
        </View>
      ) : <>
      <View style={styles.demoNotice}><AppText style={styles.demoNoticeText}>ⓘ {demoNotice[language]}</AppText></View>

      <View style={styles.section}>
        <SectionHeading title={t.categories} action={t.all} onAction={() => router.push("/(tabs)/categories")} />
        <View style={styles.categoryGrid}>
          {categories.slice(0, 8).map((category) => (
            <Pressable key={category.id} accessibilityRole="button" onPress={() => router.push({ pathname: "/category/[id]", params: { id: category.id } })} style={({ pressed }) => [styles.categoryCard, pressed && { opacity: 0.75 }]}>
              <View style={styles.categoryIcon}><AppText style={{ fontSize: 25 }}>{category.icon}</AppText></View>
              <AppText style={styles.categoryName}>{isAr ? category.nameAr : category.nameEn}</AppText>
            </Pressable>
          ))}
        </View>
      </View>

      <View style={styles.section}>
        <SectionHeading title={t.featured} />
        <View style={{ gap: spacing.md }}>
          {listings.filter((item) => item.featured).map(renderListing)}

        </View>
      </View>
      <View style={styles.promoCard}><AppText style={styles.promoEyebrow}>{t.sponsored}</AppText><AppText style={styles.promoTitle}>{isAr ? "مساحة إعلانية لأصحاب الأنشطة" : "Promote your local business"}</AppText><AppText style={styles.promoBody}>{isAr ? "سيظهر هنا محتوى إعلاني واضح ومميز عند تفعيل الإعلانات." : "Clearly labeled local promotions will appear here when enabled."}</AppText></View>
      <View style={styles.section}><SectionHeading title={t.offers} /><View style={styles.editorialCard}><AppText style={styles.editorialIcon}>🏷️</AppText><View style={{flex:1,gap:4}}><AppText style={styles.editorialTitle}>{isAr ? "العروض قريبًا" : "Offers coming soon"}</AppText><AppText style={styles.editorialBody}>{isAr ? "هنعرض العروض الفعلية بعد اعتماد بيانات المتاجر." : "Verified store offers will appear here once available."}</AppText></View></View></View>
      <View style={styles.section}><SectionHeading title={t.news} /><View style={styles.editorialCard}><AppText style={styles.editorialIcon}>📰</AppText><View style={{flex:1,gap:4}}><AppText style={styles.editorialTitle}>{isAr ? "أخبار مجتمع بلدنا" : "Baladna community news"}</AppText><AppText style={styles.editorialBody}>{isAr ? "مساحة للأخبار المحلية والإعلانات المجتمعية المعتمدة." : "A place for verified local news and community updates."}</AppText></View></View></View>
      <AppText style={styles.footer}>{t.demo}</AppText>
      </>}
    </ScrollView>
  </Screen>;
}

const styles = StyleSheet.create({
  content: { paddingBottom: 24, gap: spacing.lg },
  clearFilters: { alignSelf: "flex-start", paddingTop: spacing.md },
  clearFiltersText: { color: colors.primary, fontWeight: "800", fontSize: 12 },
  hero: { backgroundColor: colors.primary, paddingHorizontal: spacing.lg, paddingTop: 20, paddingBottom: 24, borderBottomLeftRadius: 28, borderBottomRightRadius: 28, gap: spacing.md },
  topRow: { flexDirection: "row", alignItems: "center", gap: spacing.md, marginBottom: 8 },
  brand: { color: "#FFFFFF", fontWeight: "900", fontSize: 22 },
  profileButton: { width: 46, height: 46, borderRadius: 23, borderColor: "#83BFB5", borderWidth: 1, alignItems: "center", justifyContent: "center", backgroundColor: "#FFFFFF15" },
  profileGlyph: { color: "#FFFFFF", fontSize: 26, lineHeight: 30 },
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
  favoriteButton: { width: 46, height: 46, borderRadius: 23, alignItems: "center", justifyContent: "center", backgroundColor: colors.background, borderWidth: 1, borderColor: colors.border },
  promoCard: { marginHorizontal: spacing.lg, backgroundColor: colors.primaryDark, borderRadius: radii.lg, padding: spacing.lg, gap: spacing.sm },
  promoEyebrow: { color: "#BFE4DC", fontSize: 11, fontWeight: "800" },
  promoTitle: { color: "#FFFFFF", fontSize: 17, fontWeight: "900" },
  promoBody: { color: "#E0F2EE", fontSize: 12, lineHeight: 18 },
  editorialCard: { flexDirection: "row", alignItems: "center", gap: spacing.md, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radii.md, backgroundColor: colors.surface },
  editorialIcon: { fontSize: 30 },
  editorialTitle: { fontSize: 14, fontWeight: "800" },
  editorialBody: { color: colors.muted, fontSize: 12, lineHeight: 18 },
  footer: { color: colors.muted, textAlign: "center", fontSize: 10, paddingHorizontal: spacing.lg },
});
function normalizeSearchText(value: string): string {
  return value
    .normalize("NFKC")
    .replace(/[\u064B-\u065F\u0670\u0640]/g, "")
    .replace(/[أإآٱ]/g, "ا")
    .replace(/ى/g, "ي")
    .toLocaleLowerCase()
    .replace(/\s+/g, " ")
    .trim();
}
