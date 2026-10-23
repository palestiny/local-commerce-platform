import { router } from "expo-router";
import React from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { AppText, PrimaryButton, Screen, uiStyles } from "../src/components/ui";
import { useAppState } from "../src/state/AppProvider";
import { colors, spacing } from "../src/theme/tokens";

export default function ProfileScreen() {
  const { language, setLanguage, favorites, cart } = useAppState();
  const insets = useSafeAreaInsets();
  const isAr = language === "ar";
  const cartCount = Object.values(cart).reduce((sum, quantity) => sum + quantity, 0);
  return <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
    <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
      <View style={[styles.header, { paddingTop: Math.max(58, insets.top + 24) }]}>
        <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.backButton}><AppText style={styles.backText}>‹</AppText></Pressable>
        <AppText style={styles.brand}>{isAr ? "حسابي" : "My account"}</AppText>
        <View style={styles.avatar}><AppText style={styles.avatarText}>♙</AppText></View>
        <AppText style={styles.welcome}>{isAr ? "أهلًا بيك في بلدنا" : "Welcome to Baladna"}</AppText>
        <AppText style={styles.subtitle}>{isAr ? "ادخل لحسابك علشان كل طلباتك ومفضلاتك ورسائلك تبقى في مكان واحد." : "Sign in to keep orders, favorites, and messages together."}</AppText>
        <PrimaryButton title={isAr ? "الدخول باستخدام Google أو البريد" : "Sign in with Google or email"} onPress={() => router.push("/auth")} />
        <AppText style={styles.note}>{isAr ? "الحساب يحتاج ربط خدمة تسجيل دخول آمنة قبل الاستخدام الحقيقي." : "Account access requires secure authentication integration before production."}</AppText>
      </View>
      <View style={styles.section}>
        <AppText style={styles.sectionTitle}>{isAr ? "التسوق" : "Shopping"}</AppText>
        <AccountRow icon="🛒" title={isAr ? "سلة التسوق" : "Shopping cart"} hint={(isAr ? "القطع الموجودة في السلة: " : "Items in cart: ") + cartCount} onPress={() => router.push("/cart")} />
        <AccountRow icon="♡" title={isAr ? "المفضلة" : "Favorites"} hint={(isAr ? "العناصر المحفوظة: " : "Saved items: ") + favorites.length} onPress={() => router.push("/(tabs)/favorites")} />
        <AccountRow icon="🧾" title={isAr ? "طلباتي" : "My orders"} hint={isAr ? "متابعة الطلبات والحجوزات" : "Orders and bookings"} onPress={() => router.push("/(tabs)/orders")} />
        <AccountRow icon="🔔" title={isAr ? "الإشعارات" : "Notifications"} hint={isAr ? "تحديثات الطلبات والرسائل والعروض" : "Orders, messages, and offers"} onPress={() => router.push("/notifications")} />
      </View>
      <View style={styles.section}>
        <AppText style={styles.sectionTitle}>{isAr ? "حسابك واشتراكاتك" : "Account and membership"}</AppText>
        <AccountRow icon="👤" title={isAr ? "تعديل الملف الشخصي" : "Edit profile"} hint={isAr ? "الاسم والهاتف والعنوان" : "Name, phone, and address"} onPress={() => router.push("/edit-profile")} />
        <AccountRow icon="✦" title={isAr ? "الاشتراكات والباقات" : "Subscriptions and plans"} hint={isAr ? "استعراض الباقات وحالة الاشتراك" : "Explore plans and membership status"} onPress={() => router.push("/subscriptions")} />
        <AccountRow icon="💬" title={isAr ? "المحادثات" : "Conversations"} hint={isAr ? "تواصل مع المتاجر" : "Chat with stores"} onPress={() => router.push("/messages")} />
      </View>
      <View style={styles.section}>
        <AppText style={styles.sectionTitle}>{isAr ? "إعدادات التطبيق" : "App settings"}</AppText>
        <AccountRow icon="⚙️" title={isAr ? "الإعدادات والإشعارات" : "Settings and notifications"} hint={isAr ? "اللغة وتفضيلات التنبيهات" : "Language and notification preferences"} onPress={() => router.push("/settings")} />
        <AccountRow icon="🌐" title={isAr ? "لغة التطبيق" : "App language"} hint={isAr ? "العربية" : "English"} onPress={() => setLanguage(isAr ? "en" : "ar")} />
      </View>
      <AppText style={styles.footer}>{isAr ? "بلدنا · نسخة تجريبية" : "Baladna · Prototype"}</AppText>
    </ScrollView>
  </Screen>;
}

function AccountRow({ icon, title, hint, onPress }: { icon: string; title: string; hint: string; onPress: () => void }) {
  return <Pressable accessibilityRole="button" onPress={onPress} style={({ pressed }) => [uiStyles.card, pressed && { opacity: 0.78 }]}>
    <View style={styles.row}><AppText style={styles.rowIcon}>{icon}</AppText><View style={{ flex: 1, gap: 3 }}><AppText style={styles.rowTitle}>{title}</AppText><AppText style={styles.rowHint}>{hint}</AppText></View><AppText style={styles.chevron}>‹</AppText></View>
  </Pressable>;
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
