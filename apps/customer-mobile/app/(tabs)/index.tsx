import { router } from "expo-router";
import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Animated, Modal, Pressable, RefreshControl, ScrollView, StyleSheet, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { AppText, Screen, SearchInput, SectionHeading, uiStyles } from "../../src/components/ui";
import { categories, demoNotice, listings, products, type Category, type LocalListing } from "../../src/data/demo";
import { useAppState } from "../../src/state/AppProvider";
import { colors, radii, spacing } from "../../src/theme/tokens";

export default function HomeScreen() {
  const { language, setLanguage, favorites, toggleFavorite, cart } = useAppState();
  const [query, setQuery] = useState("");
  const [refreshing, setRefreshing] = useState(false);
  const [adVisible, setAdVisible] = useState(true);
  const insets = useSafeAreaInsets();
  const isAr = language === "ar";
  const cartCount = Object.values(cart).reduce((sum, quantity) => sum + quantity, 0);
  const t = isAr
    ? {
        brand: "بلدنا", greeting: "أهلاً بيك في بلدنا 👋", subtitle: "اكتشف متاجر وخدمات قريبة منك",
        categories: "كل القطاعات", popularSectors: "أشهر القطاعات", all: "عرض الكل",
        featured: "اختيارات مميزة", search: "بتدور على إيه؟ متجر، خدمة، منتج...",
        demo: "بيانات تجريبية", searchResults: "نتائج البحث", offers: "عروض مميزة",
        news: "آخر الأخبار", sponsored: "مساحة إعلانية", noResults: "مفيش نتائج مطابقة لبحثك.",
        latest: "المضافة حديثًا", viewed: "الأكثر مشاهدة", rated: "الأعلى تقييمًا",
        members: "الأنشطة المشتركة المميزة", premium: "مشترك مميز", refresh: "تحديث الصفحة",
        empty: "لا توجد أنشطة في هذا القسم حاليًا", clear: "مسح البحث",
      }
    : {
        brand: "Baladna", greeting: "Welcome to Baladna 👋", subtitle: "Discover local shops and services",
        categories: "All sectors", popularSectors: "Popular sectors", all: "View all",
        featured: "Featured places", search: "Search shops, services, products...",
        demo: "Demo data", searchResults: "Search results", offers: "Featured offers",
        news: "Latest news", sponsored: "Ad space", noResults: "No results match your search.",
        latest: "Recently added", viewed: "Most viewed", rated: "Top rated",
        members: "Premium members", premium: "Premium member", refresh: "Refresh page",
        empty: "No listings in this section yet", clear: "Clear search",
      };

  const filtered = useMemo(() => {
    const normalizedQuery = normalizeSearchText(query);
    return listings.filter((item) => {
      const category = categories.find((entry) => entry.id === item.categoryId);
      const searchableText = [item.nameAr, item.nameEn, item.categoryAr, item.categoryEn, category?.nameAr, category?.nameEn]
        .filter(Boolean).join(" ");
      return normalizeSearchText(searchableText).includes(normalizedQuery);
    }).sort(compareMembership);
  }, [query]);

  const refreshHome = useCallback(() => {
    if (refreshing) return;
    setRefreshing(true);
    // The API is not connected yet. Keep the current search and provide native refresh feedback for the demo.
    setTimeout(() => setRefreshing(false), 450);
  }, [refreshing]);

  const openListing = (item: LocalListing) => {
    if (products.some((product) => product.storeId === item.id)) router.push({ pathname: "/store/[id]", params: { id: item.id } });
    else router.push({ pathname: "/listing/[id]", params: { id: item.id } });
  };
  const renderListing = (item: LocalListing) => (
    <Pressable key={item.id} onPress={() => openListing(item)} style={uiStyles.card}>
      <View style={[styles.listingRow, { flexDirection: isAr ? "row-reverse" : "row" }]}>
        <View style={styles.listingIcon}><AppText style={{ fontSize: 28 }}>{item.icon}</AppText></View>
        <View style={{ flex: 1, gap: 5 }}>
          <View style={[styles.titleLine, { flexDirection: isAr ? "row-reverse" : "row" }]}>
            <AppText style={styles.listingName}>{isAr ? item.nameAr : item.nameEn}</AppText>
            {item.membershipTier === "premium" ? <View style={styles.memberBadge}><AppText style={styles.memberBadgeText}>✦ {t.premium}</AppText></View> : null}
          </View>
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

  const renderHorizontalCard = (item: LocalListing) => (
    <Pressable key={item.id} onPress={() => openListing(item)} style={({ pressed }) => [styles.horizontalCard, pressed && styles.cardPressed]}>
      <View style={styles.horizontalCardTop}>
        <View style={styles.horizontalIcon}><AppText style={{ fontSize: 29 }}>{item.icon}</AppText></View>
        {item.membershipTier === "premium" ? <View style={styles.starBadge}><AppText style={styles.starBadgeText}>✦</AppText></View> : null}
      </View>
      <AppText numberOfLines={1} style={styles.horizontalName}>{isAr ? item.nameAr : item.nameEn}</AppText>
      <AppText numberOfLines={1} style={styles.horizontalCategory}>{isAr ? item.categoryAr : item.categoryEn}</AppText>
      <View style={styles.horizontalMeta}><AppText style={styles.rating}>★ {item.rating}</AppText><AppText style={styles.views}>◉ {item.views ?? 0}</AppText></View>
      {item.membershipTier === "premium" ? <AppText style={styles.premiumCaption}>{t.premium}</AppText> : null}
    </Pressable>
  );

  const renderListingSection = (title: string, items: LocalListing[], action?: boolean) => (
    <View style={styles.section} key={title}>
      <SectionHeading title={title} action={action ? t.all : undefined} onAction={action ? () => router.push("/(tabs)/categories") : undefined} />
      {items.length ? (
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.horizontalRow} decelerationRate="fast" directionalLockEnabled>
          {items.map(renderHorizontalCard)}
        </ScrollView>
      ) : <View style={styles.emptySection}><AppText style={styles.emptyText}>{t.empty}</AppText></View>}
    </View>
  );

  const popularCategories = useMemo(() => [...categories]
    .map((category) => ({
      category,
      views: listings.filter((item) => item.categoryId === category.id).reduce((total, item) => total + (item.views ?? 0), 0),
    }))
    .filter(({ views }) => views > 0)
    .sort((a, b) => b.views - a.views)
    .map(({ category }) => category), []);
  const latestListings = useMemo(() => [...listings].sort((a, b) => (b.createdAt ?? "").localeCompare(a.createdAt ?? "")), []);
  const mostViewed = useMemo(() => [...listings].sort((a, b) => (b.views ?? 0) - (a.views ?? 0)), []);
  const topRated = useMemo(() => [...listings].sort((a, b) => Number(b.rating) - Number(a.rating)), []);
  const premiumListings = useMemo(() => [...listings].filter((item) => item.membershipTier === "premium").sort(compareMembership), []);
  const featuredListings = useMemo(() => [...listings].filter((item) => item.featured).sort(compareMembership), []);

  return (
    <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
      <ScrollView
        contentContainerStyle={styles.content}
        showsVerticalScrollIndicator={false}
        keyboardShouldPersistTaps="handled"
        keyboardDismissMode="on-drag"
        automaticallyAdjustKeyboardInsets
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={refreshHome} tintColor={colors.primary} colors={[colors.primary]} />}
      >
        <View style={[styles.hero, { paddingTop: Math.max(20, insets.top + 12) }]}>
          <View style={[styles.topRow, { flexDirection: isAr ? "row-reverse" : "row" }]}>
            <View style={{ flex: 1, gap: 4 }}><AppText style={styles.brand}>{t.brand}</AppText></View>
            <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "تحديث الصفحة" : "Refresh page"} onPress={refreshHome} style={styles.utilityButton}><AppText style={styles.utilityGlyph}>↻</AppText></Pressable>
            <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "الإشعارات" : "Notifications"} onPress={() => router.push("/notifications")} style={styles.utilityButton}><AppText style={styles.profileGlyph}>🔔</AppText></Pressable>
            <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "حسابي" : "My account"} onPress={() => router.push("/profile")} style={styles.utilityButton}><AppText style={styles.profileGlyph}>♙</AppText></Pressable>
            <Pressable onPress={() => setLanguage(isAr ? "en" : "ar")} accessibilityRole="button" style={styles.languageButton}><AppText style={styles.languageText}>{isAr ? "EN" : "عربي"}</AppText></Pressable>
          </View>
          <AppText style={styles.greeting}>{t.greeting}</AppText>
          <AppText style={styles.subtitle}>{t.subtitle}</AppText>
          <SearchInput value={query} onChangeText={setQuery} placeholder={t.search} returnKeyType="search" />
          <Pressable accessibilityRole="button" onPress={() => router.push("/cart")} style={styles.cartShortcut}><AppText style={styles.cartShortcutText}>{(isAr ? "🛒 سلة التسوق (" : "🛒 Shopping cart (") + cartCount + ")"}</AppText></Pressable>
        </View>

        {query.trim().length > 0 ? (
          <View style={styles.section}>
            <SectionHeading title={t.searchResults} />
            <View style={{ gap: spacing.md }}>
              {filtered.map(renderListing)}
              {filtered.length === 0 ? <View style={uiStyles.card}><AppText>{t.noResults}</AppText><Pressable onPress={() => setQuery("")} style={styles.clearFilters}><AppText style={styles.clearFiltersText}>{t.clear}</AppText></Pressable></View> : null}
            </View>
          </View>
        ) : (
          <>
            <View style={styles.demoNotice}><AppText style={styles.demoNoticeText}>ⓘ {demoNotice[language]}</AppText></View>
            <View style={styles.section}>
              <SectionHeading title={t.categories} action={t.all} onAction={() => router.push("/(tabs)/categories")} />
              <MovingCategoryBar categories={categories} isAr={isAr} onSelect={(category) => router.push({ pathname: "/category/[id]", params: { id: category.id } })} />
            </View>
            <View style={styles.section}>
              <SectionHeading title={t.popularSectors} action={t.all} onAction={() => router.push("/(tabs)/categories")} />
              <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.horizontalRow} decelerationRate="fast" directionalLockEnabled>
                {popularCategories.map((category) => (
                  <Pressable key={category.id} onPress={() => router.push({ pathname: "/category/[id]", params: { id: category.id } })} style={({ pressed }) => [styles.sectorCard, pressed && styles.cardPressed]}>
                    <View style={styles.sectorIcon}><AppText style={{ fontSize: 28 }}>{category.icon}</AppText></View>
                    <AppText numberOfLines={2} style={styles.sectorName}>{isAr ? category.nameAr : category.nameEn}</AppText>
                    <AppText style={styles.sectorCaption}>{isAr ? "اكتشف الأنشطة" : "Explore listings"} ›</AppText>
                  </Pressable>
                ))}
              </ScrollView>
            </View>
            {renderListingSection(t.members, premiumListings)}
            {renderListingSection(t.featured, featuredListings)}
            {renderListingSection(t.latest, latestListings)}
            {renderListingSection(t.viewed, mostViewed)}
            {renderListingSection(t.rated, topRated)}
            <View style={styles.promoCard}>
              <AppText style={styles.promoEyebrow}>{t.sponsored}</AppText>
              <AppText style={styles.promoTitle}>{isAr ? "خلّي نشاطك يوصل لعملاء أكتر" : "Help more customers discover your business"}</AppText>
              <AppText style={styles.promoBody}>{isAr ? "هتظهر الإعلانات المدفوعة هنا بعلامة واضحة بعد تفعيل نظام الإعلانات." : "Paid promotions will appear here with clear labels once ads are enabled."}</AppText>
            </View>
            <View style={styles.section}>
              <SectionHeading title={t.offers} />
              <View style={styles.editorialCard}><AppText style={styles.editorialIcon}>🏷️</AppText><View style={{ flex: 1, gap: 4 }}><AppText style={styles.editorialTitle}>{isAr ? "العروض قريبًا" : "Offers coming soon"}</AppText><AppText style={styles.editorialBody}>{isAr ? "العروض الفعلية هتظهر عند ربط بيانات المتاجر." : "Live offers will appear when store data is connected."}</AppText></View></View>
            </View>
            <View style={styles.section}>
              <SectionHeading title={t.news} />
              <View style={styles.editorialCard}><AppText style={styles.editorialIcon}>📰</AppText><View style={{ flex: 1, gap: 4 }}><AppText style={styles.editorialTitle}>{isAr ? "أخبار مجتمع بلدنا" : "Baladna community news"}</AppText><AppText style={styles.editorialBody}>{isAr ? "أخبار محلية وتحديثات موثوقة من مجتمعك." : "Local news and trusted community updates."}</AppText></View></View>
            </View>
            <AppText style={styles.footer}>{t.demo}</AppText>
          </>
        )}
      </ScrollView>
      <Modal transparent visible={adVisible} animationType="fade" statusBarTranslucent onRequestClose={() => setAdVisible(false)}>
        <View style={styles.adBackdrop}>
          <View style={styles.adWindow}>
            <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "إغلاق الإعلان" : "Close advertisement"} onPress={() => setAdVisible(false)} style={styles.adClose}><AppText style={styles.adCloseText}>×</AppText></Pressable>
            <View style={styles.adLabel}><AppText style={styles.adLabelText}>{isAr ? "إعلان تجريبي" : "DEMO ADVERTISEMENT"}</AppText></View>
            <View style={styles.adArtwork}><AppText style={{ fontSize: 54 }}>🛍️</AppText><AppText style={styles.adArtworkTitle}>{isAr ? "اكتشف عروض بلدنا" : "Discover Baladna offers"}</AppText></View>
            <AppText style={styles.adTitle}>{isAr ? "كل اللي تحتاجه من متاجر قريبة منك" : "Everything you need from local stores"}</AppText>
            <AppText style={styles.adDescription}>{isAr ? "الإعلانات الفعلية هتظهر هنا بعد ربط نظام الحملات والإعلانات. المحتوى الحالي للتجربة فقط." : "Live campaigns will appear here after the ad system is connected. This content is for preview only."}</AppText>
            <Pressable accessibilityRole="button" onPress={() => { setAdVisible(false); router.push("/(tabs)/categories"); }} style={styles.adAction}><AppText style={styles.adActionText}>{isAr ? "اكتشف المتاجر" : "Explore stores"}</AppText></Pressable>
            <Pressable accessibilityRole="button" onPress={() => setAdVisible(false)} style={styles.adDismiss}><AppText style={styles.adDismissText}>{isAr ? "تخطي والدخول للتطبيق" : "Skip and enter app"}</AppText></Pressable>
          </View>
        </View>
      </Modal>
    </Screen>
  );
}

function compareMembership(a: LocalListing, b: LocalListing) {
  const tier = (item: LocalListing) => item.membershipTier === "premium" ? 1 : 0;
  return tier(b) - tier(a) || (b.views ?? 0) - (a.views ?? 0);
}

function MovingCategoryBar({ categories: items, isAr, onSelect }: { categories: Category[]; isAr: boolean; onSelect: (category: Category) => void }) {
  const translateX = useRef(new Animated.Value(0)).current;
  const [itemRowWidth, setItemRowWidth] = useState(0);

  useEffect(() => {
    if (!itemRowWidth) return;
    const animation = Animated.loop(Animated.timing(translateX, {
      toValue: -itemRowWidth,
      duration: Math.max(26000, itemRowWidth * 32),
      useNativeDriver: true,
      isInteraction: false,
    }));
    animation.start();
    return () => animation.stop();
  }, [itemRowWidth, translateX]);

  const renderItems = (suffix: string) => (
    <View
      key={suffix}
      style={styles.tickerRow}
      onLayout={suffix === "first" ? (event) => setItemRowWidth(event.nativeEvent.layout.width) : undefined}
    >
      {items.map((category) => (
        <Pressable key={category.id + suffix} onPress={() => onSelect(category)} style={({ pressed }) => [styles.tickerItem, pressed && styles.cardPressed]}>
          <AppText style={styles.tickerIcon}>{category.icon}</AppText>
          <AppText numberOfLines={1} style={styles.tickerText}>{isAr ? category.nameAr : category.nameEn}</AppText>
          <AppText style={styles.tickerArrow}>›</AppText>
        </Pressable>
      ))}
    </View>
  );

  return (
    <View style={styles.tickerViewport}>
      <Animated.View style={[styles.tickerTrack, { transform: [{ translateX }] }]}>
        {renderItems("first")}
        {renderItems("second")}
      </Animated.View>
    </View>
  );
}

function normalizeSearchText(value: string): string {
  return value.normalize("NFKC").replace(/[\u064B-\u065F\u0670\u0640]/g, "").replace(/[أإآٱ]/g, "ا").replace(/ى/g, "ي").toLocaleLowerCase().replace(/\s+/g, " ").trim();
}

const styles = StyleSheet.create({
  content: { paddingBottom: 28, gap: spacing.lg },
  clearFilters: { alignSelf: "flex-start", paddingTop: spacing.md },
  clearFiltersText: { color: colors.primary, fontWeight: "800", fontSize: 12 },
  hero: { backgroundColor: colors.primary, paddingHorizontal: spacing.lg, paddingBottom: 24, borderBottomLeftRadius: 28, borderBottomRightRadius: 28, gap: spacing.md },
  topRow: { flexDirection: "row", alignItems: "center", gap: spacing.sm, marginBottom: 8 },
  brand: { color: "#FFFFFF", fontWeight: "900", fontSize: 22 },
  utilityButton: { width: 42, height: 42, borderRadius: 21, borderColor: "#83BFB5", borderWidth: 1, alignItems: "center", justifyContent: "center", backgroundColor: "#FFFFFF15" },
  utilityGlyph: { color: "#FFFFFF", fontSize: 28, lineHeight: 31 },
  profileGlyph: { color: "#FFFFFF", fontSize: 24, lineHeight: 28 },
  languageButton: { paddingHorizontal: 12, paddingVertical: 8, borderRadius: radii.pill, borderColor: "#83BFB5", borderWidth: 1 },
  languageText: { color: "#FFFFFF", fontWeight: "800", fontSize: 12 },
  greeting: { color: "#FFFFFF", fontSize: 24, lineHeight: 33, fontWeight: "900" },
  cartShortcut: { alignSelf: "flex-start", backgroundColor: "#FFFFFF18", borderColor: "#83BFB5", borderWidth: 1, borderRadius: radii.pill, paddingHorizontal: 13, paddingVertical: 9 },
  cartShortcutText: { color: "#FFFFFF", fontSize: 12, fontWeight: "800" },
  subtitle: { color: "#D9F0EB", fontSize: 13, lineHeight: 20 },
  demoNotice: { marginHorizontal: spacing.lg, padding: spacing.md, backgroundColor: "#FFF4E4", borderRadius: radii.sm },
  demoNoticeText: { color: "#79521F", fontSize: 11, lineHeight: 17 },
  section: { paddingHorizontal: spacing.lg, gap: spacing.md },
  tickerViewport: { overflow: "hidden", paddingVertical: 2 },
  tickerTrack: { flexDirection: "row", alignSelf: "flex-start" },
  tickerRow: { flexDirection: "row", alignItems: "center", gap: spacing.sm, paddingEnd: spacing.sm },
  tickerItem: { minWidth: 132, minHeight: 64, flexDirection: "row", alignItems: "center", gap: 7, paddingHorizontal: 11, paddingVertical: 10, borderRadius: radii.md, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
  tickerIcon: { fontSize: 22 },
  tickerText: { flexShrink: 1, color: colors.text, fontSize: 11, fontWeight: "800" },
  tickerArrow: { color: colors.primary, fontSize: 20, fontWeight: "900" },
  horizontalRow: { gap: spacing.md, paddingEnd: spacing.lg },
  horizontalCard: { width: 174, minHeight: 190, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radii.lg, backgroundColor: colors.surface, gap: 7 },
  sectorCard: { width: 140, minHeight: 154, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radii.lg, backgroundColor: colors.surface, gap: 9 },
  sectorIcon: { width: 58, height: 58, borderRadius: 18, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  sectorName: { fontSize: 12, fontWeight: "900", color: colors.text, lineHeight: 17 },
  sectorCaption: { color: colors.primary, fontSize: 10, fontWeight: "800" },
  cardPressed: { opacity: 0.78, transform: [{ scale: 0.985 }] },
  horizontalCardTop: { flexDirection: "row", alignItems: "flex-start", justifyContent: "space-between" },
  horizontalIcon: { width: 58, height: 58, borderRadius: 18, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  starBadge: { width: 28, height: 28, borderRadius: 14, backgroundColor: "#FFF0CF", alignItems: "center", justifyContent: "center" },
  starBadgeText: { color: "#96620A", fontSize: 16, fontWeight: "900" },
  horizontalName: { fontSize: 13, fontWeight: "900", color: colors.text },
  horizontalCategory: { color: colors.muted, fontSize: 11 },
  horizontalMeta: { flexDirection: "row", alignItems: "center", justifyContent: "space-between" },
  views: { color: colors.muted, fontSize: 11, fontWeight: "700" },
  premiumCaption: { color: "#96620A", fontSize: 10, fontWeight: "900" },
  titleLine: { alignItems: "center", flexWrap: "wrap", gap: 5 },
  memberBadge: { alignSelf: "flex-start", paddingHorizontal: 7, paddingVertical: 3, borderRadius: radii.pill, backgroundColor: "#FFF0CF" },
  memberBadgeText: { color: "#805400", fontSize: 9, fontWeight: "900" },
  listingRow: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  listingIcon: { width: 62, height: 62, borderRadius: 18, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  listingName: { fontSize: 14, fontWeight: "800", flexShrink: 1 },
  rating: { color: "#D88A20", fontWeight: "800", fontSize: 12 },
  price: { color: colors.primaryDark, fontWeight: "700", fontSize: 12 },
  favoriteButton: { width: 46, height: 46, borderRadius: 23, alignItems: "center", justifyContent: "center", backgroundColor: colors.background, borderWidth: 1, borderColor: colors.border },
  emptySection: { borderRadius: radii.md, borderColor: colors.border, borderWidth: 1, padding: spacing.md },
  emptyText: { color: colors.muted, fontSize: 12 },
  promoCard: { marginHorizontal: spacing.lg, backgroundColor: colors.primaryDark, borderRadius: radii.lg, padding: spacing.lg, gap: spacing.sm },
  promoEyebrow: { color: "#BFE4DC", fontSize: 11, fontWeight: "800" },
  promoTitle: { color: "#FFFFFF", fontSize: 17, fontWeight: "900" },
  promoBody: { color: "#E0F2EE", fontSize: 12, lineHeight: 18 },
  editorialCard: { flexDirection: "row", alignItems: "center", gap: spacing.md, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radii.md, backgroundColor: colors.surface },
  editorialIcon: { fontSize: 30 },
  editorialTitle: { fontSize: 14, fontWeight: "800" },
  editorialBody: { color: colors.muted, fontSize: 12, lineHeight: 18 },
  footer: { color: colors.muted, textAlign: "center", fontSize: 10, paddingHorizontal: spacing.lg },
  adBackdrop: { flex: 1, backgroundColor: "#10201DB8", justifyContent: "center", alignItems: "center", padding: 22 },
  adWindow: { width: "100%", maxWidth: 390, backgroundColor: colors.surface, borderRadius: 28, padding: spacing.lg, gap: spacing.md, elevation: 16, shadowColor: "#000000", shadowOpacity: 0.22, shadowRadius: 24, shadowOffset: { width: 0, height: 12 } },
  adClose: { alignSelf: "flex-end", width: 38, height: 38, borderRadius: 19, backgroundColor: colors.background, alignItems: "center", justifyContent: "center" },
  adCloseText: { fontSize: 28, lineHeight: 30, color: colors.text },
  adLabel: { alignSelf: "flex-start", borderRadius: radii.pill, backgroundColor: "#FFF0CF", paddingHorizontal: 10, paddingVertical: 5 },
  adLabelText: { color: "#805400", fontSize: 10, fontWeight: "900" },
  adArtwork: { minHeight: 150, borderRadius: 22, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center", gap: spacing.sm },
  adArtworkTitle: { color: colors.primaryDark, fontSize: 17, fontWeight: "900" },
  adTitle: { fontSize: 19, fontWeight: "900", lineHeight: 27, textAlign: "center" },
  adDescription: { color: colors.muted, fontSize: 12, lineHeight: 19, textAlign: "center" },
  adAction: { backgroundColor: colors.primary, borderRadius: radii.md, padding: spacing.md, alignItems: "center" },
  adActionText: { color: "#FFFFFF", fontWeight: "900" },
  adDismiss: { paddingVertical: 8, alignItems: "center" },
  adDismissText: { color: colors.muted, fontSize: 12, fontWeight: "800" },
});
