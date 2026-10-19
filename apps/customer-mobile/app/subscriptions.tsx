import { router } from "expo-router";
import React from "react";
import { Alert, Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, PrimaryButton, Screen, uiStyles } from "../src/components/ui";
import { colors, radii, spacing } from "../src/theme/tokens";
import { useAppState } from "../src/state/AppProvider";

export default function SubscriptionsScreen() {
  const { language } = useAppState();
  const isAr = language === "ar";
  const choose = () => Alert.alert(isAr ? "الباقات غير مفعلة" : "Plans are not active", isAr ? "الباقات المعروضة تصور مبدئي. تفعيل الاشتراك والدفع يحتاج نظام اشتراكات وفواتير في الخلفية." : "These are illustrative plans. Activation and payment require a subscription and billing backend.");
  return <Screen>
    <ScrollView contentContainerStyle={styles.content}>
      <Pressable onPress={() => router.back()} style={styles.back}><AppText style={styles.backText}>‹</AppText></Pressable>
      <AppText style={styles.title}>{isAr ? "الاشتراكات والباقات" : "Subscriptions and plans"}</AppText>
      <AppText style={styles.subtitle}>{isAr ? "استكشف مزايا بلدنا. الأسعار التالية توضيحية وليست عرضًا للبيع." : "Explore Baladna membership. These prices are illustrative, not a live offer."}</AppText>
      <View style={uiStyles.card}>
        <View style={styles.planTop}><AppText style={styles.planIcon}>🌱</AppText><AppText style={styles.planName}>{isAr ? "الأساسية" : "Basic"}</AppText></View>
        <AppText style={styles.price}>{isAr ? "مجانية" : "Free"}</AppText>
        <AppText style={styles.description}>{isAr ? "اكتشاف المتاجر، المفضلة، وإدارة الطلبات." : "Discover stores, save favorites, and manage orders."}</AppText>
        <View style={styles.current}><AppText style={styles.currentText}>{isAr ? "الخطة الافتراضية" : "Default plan"}</AppText></View>
      </View>
      <View style={[uiStyles.card, styles.premium]}>
        <View style={styles.planTop}><AppText style={styles.planIcon}>✦</AppText><AppText style={styles.planName}>{isAr ? "بلدنا بلس" : "Baladna Plus"}</AppText></View>
        <AppText style={styles.price}>{isAr ? "السعر يحدد لاحقًا" : "Price to be determined"}</AppText>
        <AppText style={styles.description}>{isAr ? "مزايا إضافية وعروض خاصة حسب سياسة المنصة." : "Additional benefits and special offers, subject to platform policy."}</AppText>
        <PrimaryButton title={isAr ? "معرفة التفاصيل" : "Learn more"} onPress={choose} />
      </View>
      <AppText style={styles.note}>{isAr ? "اشتراكات أصحاب المتاجر وباقات ظهور الأنشطة ستكون منفصلة عن اشتراك العميل." : "Merchant subscriptions and business promotion plans will be separate from customer membership."}</AppText>
    </ScrollView>
  </Screen>;
}
const styles = StyleSheet.create({
  content: { paddingBottom: 30, gap: spacing.lg },
  back: { width: 42, height: 42, borderRadius: 21, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  backText: { color: colors.primary, fontSize: 28 },
  title: { fontSize: 24, fontWeight: "900" },
  subtitle: { color: colors.muted, fontSize: 12, lineHeight: 18 },
  planTop: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  planIcon: { fontSize: 28 },
  planName: { fontSize: 17, fontWeight: "900" },
  price: { color: colors.primaryDark, fontSize: 18, fontWeight: "900", marginTop: spacing.md },
  description: { color: colors.muted, fontSize: 12, lineHeight: 19, marginVertical: spacing.md },
  current: { alignSelf: "flex-start", paddingHorizontal: 10, paddingVertical: 6, borderRadius: radii.pill, backgroundColor: colors.primarySoft },
  currentText: { color: colors.primaryDark, fontSize: 11, fontWeight: "900" },
  premium: { borderColor: colors.primary, borderWidth: 1 },
  note: { color: colors.muted, fontSize: 10, lineHeight: 16, textAlign: "center" },
});