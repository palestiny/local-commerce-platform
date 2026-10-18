import { router } from "expo-router";
import React from "react";
import { Alert, Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, PrimaryButton, Screen, uiStyles } from "../src/components/ui";
import { useAppState } from "../src/state/AppProvider";
import { colors, radii, spacing } from "../src/theme/tokens";

export default function ProfileScreen() {
  const { language, setLanguage } = useAppState();
  const isAr = language === "ar";
  const showDemoNotice = () => Alert.alert(
    isAr ? "تسجيل الدخول قريبًا" : "Sign-in coming soon",
    isAr ? "صفحة الحساب جاهزة مبدئيًا، لكن إنشاء الحساب وتسجيل الدخول يحتاجان ربطًا آمنًا بالخلفية." : "The profile screen is a prototype. Account creation and sign-in require secure backend integration."
  );
  return (
    <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
      <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
        <View style={styles.header}>
          <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.backButton}><AppText style={styles.backText}>‹</AppText></Pressable>
          <AppText style={styles.brand}>{isAr ? "حسابي" : "My account"}</AppText>
          <View style={styles.avatar}><AppText style={styles.avatarText}>♙</AppText></View>
          <AppText style={styles.welcome}>{isAr ? "أهلًا بيك في بلدنا" : "Welcome to Baladna"}</AppText>
          <AppText style={styles.subtitle}>{isAr ? "سجّل دخولك علشان تتابع طلباتك وتحفظ أنشطتك المفضلة." : "Sign in to track orders and keep your favorite places."}</AppText>
          <PrimaryButton title={isAr ? "تسجيل الدخول / إنشاء حساب" : "Sign in / Create account"} onPress={showDemoNotice} />
          <AppText style={styles.note}>{isAr ? "تسجيل الدخول غير متصل بالخلفية في النسخة الحالية." : "Sign-in is not connected to a backend yet."}</AppText>
        </View>
        <View style={styles.section}>
          <AppText style={styles.sectionTitle}>{isAr ? "حسابك" : "Your account"}</AppText>
          <Pressable accessibilityRole="button" onPress={() => router.push("/(tabs)/orders")} style={uiStyles.card}><View style={styles.row}><AppText style={styles.rowIcon}>🧾</AppText><View style={{ flex: 1, gap: 3 }}><AppText style={styles.rowTitle}>{isAr ? "طلباتي" : "My orders"}</AppText><AppText style={styles.rowHint}>{isAr ? "متابعة الطلبات والحجوزات" : "Orders and bookings"}</AppText></View><AppText style={styles.chevron}>‹</AppText></View></Pressable>
          <Pressable accessibilityRole="button" onPress={() => router.push("/(tabs)/favorites")} style={uiStyles.card}><View style={styles.row}><AppText style={styles.rowIcon}>♡</AppText><View style={{ flex: 1, gap: 3 }}><AppText style={styles.rowTitle}>{isAr ? "المفضلة" : "Favorites"}</AppText><AppText style={styles.rowHint}>{isAr ? "الأنشطة اللي حفظتها" : "Places you saved"}</AppText></View><AppText style={styles.chevron}>‹</AppText></View></Pressable>
        </View>
        <View style={styles.section}>
          <AppText style={styles.sectionTitle}>{isAr ? "تفضيلات التطبيق" : "App preferences"}</AppText>
          <Pressable accessibilityRole="button" onPress={() => setLanguage(isAr ? "en" : "ar")} style={uiStyles.card}><View style={styles.row}><AppText style={styles.rowIcon}>🌐</AppText><View style={{ flex: 1, gap: 3 }}><AppText style={styles.rowTitle}>{isAr ? "لغة التطبيق" : "App language"}</AppText><AppText style={styles.rowHint}>{isAr ? "العربية" : "English"}</AppText></View><AppText style={styles.chevron}>⇄</AppText></View></Pressable>
        </View>
        <AppText style={styles.footer}>{isAr ? "بلدنا · نسخة تجريبية" : "Baladna · Prototype"}</AppText>
      </ScrollView>
    </Screen>
  );
}
const styles = StyleSheet.create({
  content: { paddingBottom: 28, gap: spacing.lg },
  header: { backgroundColor: colors.primary, paddingTop: 58, paddingHorizontal: spacing.lg, paddingBottom: spacing.xl, borderBottomLeftRadius: 28, borderBottomRightRadius: 28, alignItems: "center", gap: spacing.md },
  backButton: { position: "absolute", top: 12, left: 16, width: 44, height: 44, borderRadius: 22, backgroundColor: "#FFFFFF20", alignItems: "center", justifyContent: "center" },
  backText: { color: "#FFFFFF", fontSize: 28 },
  brand: { color: "#FFFFFF", fontSize: 22, fontWeight: "900" },
  avatar: { width: 76, height: 76, borderRadius: 38, backgroundColor: "#FFFFFF20", alignItems: "center", justifyContent: "center", borderWidth: 1, borderColor: "#FFFFFF60" },
  avatarText: { color: "#FFFFFF", fontSize: 42 },
  welcome: { color: "#FFFFFF", fontSize: 20, fontWeight: "900", textAlign: "center" },
  subtitle: { color: "#D9F0EB", fontSize: 13, lineHeight: 20, textAlign: "center" },
  note: { color: "#D9F0EB", fontSize: 11, textAlign: "center", lineHeight: 17 },
  section: { paddingHorizontal: spacing.lg, gap: spacing.md },
  sectionTitle: { fontSize: 17, fontWeight: "900" },
  row: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  rowIcon: { fontSize: 25, width: 38, textAlign: "center" },
  rowTitle: { fontSize: 14, fontWeight: "800" },
  rowHint: { color: colors.muted, fontSize: 12 },
  chevron: { color: colors.primary, fontSize: 24 },
  footer: { color: colors.muted, textAlign: "center", fontSize: 11, paddingHorizontal: spacing.lg }
});
