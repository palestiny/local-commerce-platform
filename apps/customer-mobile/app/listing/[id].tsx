import { router, useLocalSearchParams } from "expo-router";
import React from "react";
import { Alert, Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, PrimaryButton, Screen, uiStyles } from "../../src/components/ui";
import { listings } from "../../src/data/demo";
import { useAppState } from "../../src/state/AppProvider";
import { colors, radii, spacing } from "../../src/theme/tokens";

export default function ListingDetailsScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const { language, favorites, toggleFavorite } = useAppState();
  const item = listings.find((listing) => listing.id === id);
  const isAr = language === "ar";
  if (!item) return <Screen><AppText>{isAr ? "النشاط غير موجود ضمن البيانات التجريبية." : "Listing not found in demo data."}</AppText><PrimaryButton title={isAr ? "رجوع" : "Go back"} onPress={() => router.back()} /></Screen>;

  const showDemoMessage = () => Alert.alert(
    isAr ? "لسه بنجهز الوظيفة دي" : "Feature in progress",
    isAr ? "دي نسخة واجهة تجريبية. إنشاء الطلبات الحقيقية محتاج ربط آمن بالـ API المعتمد." : "This is a UI prototype. Real requests require integration with the approved secure API."
  );

  return <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
    <ScrollView contentContainerStyle={{ paddingBottom: 28, gap: spacing.lg }}>
      <View style={styles.hero}>
        <Pressable onPress={() => router.back()} style={styles.back}><AppText style={{ color: "#FFFFFF", fontSize: 24 }}>‹</AppText></Pressable>
        <AppText style={{ fontSize: 70 }}>{item.icon}</AppText>
        <AppText style={styles.heroName}>{isAr ? item.nameAr : item.nameEn}</AppText>
        <AppText style={styles.heroMeta}>{isAr ? item.categoryAr : item.categoryEn} · {isAr ? item.distanceAr : item.distanceEn}</AppText>
      </View>
      <View style={styles.body}>
        <View style={[styles.ratingCard, { flexDirection: isAr ? "row-reverse" : "row" }]}>
          <View style={{ flex: 1, gap: 4 }}>
            <AppText style={styles.rating}>★ {item.rating} <AppText style={styles.muted}>({item.reviews} {isAr ? "تقييم" : "reviews"})</AppText></AppText>
            <AppText style={styles.price}>{isAr ? item.priceAr : item.priceEn}</AppText>
          </View>
          <Pressable onPress={() => toggleFavorite(item.id)} style={styles.saveButton}>
            <AppText style={{ color: favorites.includes(item.id) ? colors.danger : colors.primary, fontWeight: "800" }}>{favorites.includes(item.id) ? "♥" : "♡"} {isAr ? "حفظ" : "Save"}</AppText>
          </Pressable>
        </View>
        <View style={uiStyles.card}>
          <AppText style={styles.sectionTitle}>{isAr ? "عن النشاط" : "About this listing"}</AppText>
          <AppText style={styles.muted}>{isAr ? "تفاصيل النشاط وساعات العمل والتوافر الفعلي ستظهر بعد ربط بيانات مقدمي الخدمات المعتمدة. المعلومات الحالية توضيحية فقط." : "Verified details, opening hours, and availability will be shown after approved provider data is connected. Current content is illustrative only."}</AppText>
        </View>
        <View style={uiStyles.card}>
          <AppText style={styles.sectionTitle}>{isAr ? "الموقع والتواصل" : "Location & contact"}</AppText>
          <AppText style={styles.muted}>{isAr ? "دكرنس، الدقهلية (موقع تجريبي)" : "Dekernes, Dakahlia (demo location)"}</AppText>
        </View>
        <PrimaryButton title={isAr ? "اطلب الخدمة / تواصل" : "Request service / Contact"} onPress={showDemoMessage} />
        <AppText style={styles.disclaimer}>{isAr ? "تنبيه: لا يتم إرسال طلب أو دفع أموال من هذه الشاشة حاليًا." : "Note: this screen does not submit a real request or payment."}</AppText>
      </View>
    </ScrollView>
  </Screen>;
}

const styles = StyleSheet.create({
  hero: { minHeight: 245, backgroundColor: colors.primary, borderBottomLeftRadius: 28, borderBottomRightRadius: 28, alignItems: "center", justifyContent: "center", padding: spacing.xl, gap: spacing.sm },
  back: { position: "absolute", top: 22, left: 18, width: 42, height: 42, borderRadius: 21, backgroundColor: "#FFFFFF25", alignItems: "center", justifyContent: "center" },
  heroName: { color: "#FFFFFF", fontSize: 22, fontWeight: "900", textAlign: "center" },
  heroMeta: { color: "#D9F0EB", fontSize: 12, textAlign: "center" },
  body: { paddingHorizontal: spacing.lg, gap: spacing.md },
  ratingCard: { flexDirection: "row", alignItems: "center", gap: spacing.md, backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: radii.md, padding: spacing.md },
  rating: { color: "#D88A20", fontSize: 14, fontWeight: "800" },
  price: { color: colors.primaryDark, fontWeight: "800", fontSize: 13 },
  muted: { color: colors.muted, fontSize: 12, lineHeight: 19 },
  sectionTitle: { fontSize: 15, fontWeight: "800", marginBottom: 8 },
  saveButton: { padding: 10, borderRadius: 12, backgroundColor: colors.primarySoft },
  disclaimer: { color: colors.muted, fontSize: 10, textAlign: "center", lineHeight: 16 },
});