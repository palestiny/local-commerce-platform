import { router, useLocalSearchParams } from "expo-router";
import React from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, PrimaryButton, Screen, uiStyles } from "../../src/components/ui";
import { listings, products } from "../../src/data/demo";
import { useAppState } from "../../src/state/AppProvider";
import { colors, radii, spacing } from "../../src/theme/tokens";

export default function StoreScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { language, favorites, toggleFavorite, addToCart, cart } = useAppState();
  const isAr = language === "ar";
  const store = listings.find((item) => item.id === id);
  const storeProducts = products.filter((product) => product.storeId === id);
  const cartCount = Object.values(cart).reduce((sum, quantity) => sum + quantity, 0);

  if (!store) return <Screen><AppText>{isAr ? "المتجر غير موجود في البيانات التجريبية." : "Store not found in demo data."}</AppText><PrimaryButton title={isAr ? "رجوع" : "Go back"} onPress={() => router.back()} /></Screen>;

  const addProduct = (productId: string) => addToCart(productId);

  return <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
    <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
      <View style={styles.hero}>
        <View style={styles.topRow}>
          <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.iconButton}><AppText style={styles.iconButtonText}>‹</AppText></Pressable>
          <View style={{ flex: 1 }} />
          <Pressable accessibilityRole="button" onPress={() => router.push("/cart")} style={styles.cartButton}><AppText style={styles.cartButtonText}>🛒 {cartCount}</AppText></Pressable>
        </View>
        <View style={styles.storeIcon}><AppText style={{ fontSize: 48 }}>{store.icon}</AppText></View>
        <AppText style={styles.storeName}>{isAr ? store.nameAr : store.nameEn}</AppText>
        <AppText style={styles.storeMeta}>{isAr ? store.categoryAr : store.categoryEn} · {isAr ? store.distanceAr : store.distanceEn}</AppText>
        <View style={styles.storeRating}><AppText style={styles.rating}>★ {store.rating}</AppText><AppText style={styles.storeMeta}>({store.reviews} {isAr ? "تقييم" : "reviews"})</AppText></View>
      </View>
      <View style={styles.body}>
        <View style={styles.sectionHeading}>
          <View style={{ flex: 1, gap: 4 }}><AppText style={styles.sectionTitle}>{isAr ? "منتجات المتجر" : "Store products"}</AppText><AppText style={styles.muted}>{isAr ? "الأسعار المعروضة تجريبية لحين ربط بيانات المتجر." : "Prices are demo values until live store inventory is connected."}</AppText></View>
          <Pressable onPress={() => router.push("/cart")} style={styles.smallCart}><AppText style={styles.smallCartText}>{isAr ? "السلة" : "Cart"} ({cartCount})</AppText></Pressable>
        </View>
        {storeProducts.map((product) => <View key={product.id} style={uiStyles.card}>
          <View style={styles.productRow}>
            <View style={styles.productIcon}><AppText style={{ fontSize: 30 }}>{product.icon}</AppText></View>
            <View style={{ flex: 1, gap: 5 }}>
              <AppText style={styles.productName}>{isAr ? product.nameAr : product.nameEn}</AppText>
              <AppText style={styles.muted}>{isAr ? product.descriptionAr : product.descriptionEn}</AppText>
              <AppText style={styles.price}>{product.price.toLocaleString(isAr ? "ar-EG" : "en-US")} {isAr ? "ج.م" : "EGP"}</AppText>
            </View>
            <View style={styles.actions}>
              <Pressable accessibilityRole="button" accessibilityLabel={favorites.includes(product.id) ? (isAr ? "إزالة من المفضلة" : "Remove from favorites") : (isAr ? "إضافة للمفضلة" : "Add to favorites")} onPress={() => toggleFavorite(product.id)} style={styles.favoriteButton}><AppText style={{ fontSize: 23, color: favorites.includes(product.id) ? colors.danger : colors.muted }}>{favorites.includes(product.id) ? "♥" : "♡"}</AppText></Pressable>
              <Pressable accessibilityRole="button" disabled={!product.available} onPress={() => addProduct(product.id)} style={styles.addButton}><AppText style={styles.addButtonText}>＋</AppText></Pressable>
            </View>
          </View>
        </View>)}
        {storeProducts.length === 0 ? <View style={uiStyles.card}><AppText style={styles.muted}>{isAr ? "المتجر ده مفيش له منتجات تجريبية لسه." : "No demo products have been added for this store yet."}</AppText></View> : null}
        <PrimaryButton title={(isAr ? "عرض السلة (" : "View cart (") + cartCount + ")"} onPress={() => router.push("/cart")} />
      </View>
    </ScrollView>
  </Screen>;
}

const styles = StyleSheet.create({
  content: { paddingBottom: 30, gap: spacing.lg },
  hero: { backgroundColor: colors.primary, borderBottomLeftRadius: 28, borderBottomRightRadius: 28, padding: spacing.lg, paddingTop: 18, alignItems: "center", gap: spacing.sm },
  topRow: { width: "100%", flexDirection: "row", alignItems: "center", gap: spacing.sm },
  iconButton: { width: 42, height: 42, borderRadius: 21, backgroundColor: "#FFFFFF22", alignItems: "center", justifyContent: "center" },
  iconButtonText: { color: "#FFFFFF", fontSize: 28 },
  cartButton: { paddingHorizontal: 14, paddingVertical: 10, borderRadius: radii.pill, backgroundColor: "#FFFFFF22" },
  cartButtonText: { color: "#FFFFFF", fontWeight: "900" },
  storeIcon: { width: 88, height: 88, borderRadius: 26, backgroundColor: "#FFFFFF22", alignItems: "center", justifyContent: "center", marginTop: 8 },
  storeName: { color: "#FFFFFF", fontSize: 22, fontWeight: "900", textAlign: "center" },
  storeMeta: { color: "#D9F0EB", fontSize: 12 },
  storeRating: { flexDirection: "row", alignItems: "center", gap: 8 },
  rating: { color: "#FFD28B", fontWeight: "900" },
  body: { paddingHorizontal: spacing.lg, gap: spacing.md },
  sectionHeading: { flexDirection: "row", alignItems: "center", gap: spacing.md, marginBottom: spacing.xs },
  sectionTitle: { fontSize: 18, fontWeight: "900" },
  muted: { color: colors.muted, fontSize: 11, lineHeight: 17 },
  smallCart: { backgroundColor: colors.primarySoft, paddingHorizontal: 12, paddingVertical: 9, borderRadius: radii.pill },
  smallCartText: { color: colors.primaryDark, fontWeight: "900", fontSize: 12 },
  productRow: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  productIcon: { width: 64, height: 64, borderRadius: 18, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  productName: { fontSize: 14, fontWeight: "900" },
  price: { color: colors.primaryDark, fontSize: 14, fontWeight: "900" },
  actions: { alignItems: "center", gap: 8 },
  favoriteButton: { width: 42, height: 42, borderRadius: 21, borderWidth: 1, borderColor: colors.border, alignItems: "center", justifyContent: "center" },
  addButton: { width: 42, height: 42, borderRadius: 14, backgroundColor: colors.primary, alignItems: "center", justifyContent: "center" },
  addButtonText: { color: "#FFFFFF", fontSize: 26, lineHeight: 28, fontWeight: "900" },
});
