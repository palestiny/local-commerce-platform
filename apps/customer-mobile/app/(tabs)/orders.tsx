import React from "react";
import { StyleSheet, View } from "react-native";
import { AppText, PrimaryButton, Screen, uiStyles } from "../../src/components/ui";
import { useAppState } from "../../src/state/AppProvider";
import { colors, spacing } from "../../src/theme/tokens";
import { router } from "expo-router";

export default function OrdersScreen() {
  const { language } = useAppState();
  const isAr = language === "ar";
  return <Screen>
    <View style={{ gap: 5 }}>
      <AppText style={styles.title}>{isAr ? "طلباتي" : "My orders"}</AppText>
      <AppText style={styles.subtitle}>{isAr ? "تابع طلباتك وحجوزاتك من مكان واحد" : "Track your orders and bookings in one place"}</AppText>
    </View>
    <View style={[uiStyles.card, styles.empty]}>
      <AppText style={{ fontSize: 42 }}>🧾</AppText>
      <AppText style={styles.heading}>{isAr ? "مفيش طلبات لحد دلوقتي" : "No orders yet"}</AppText>
      <AppText style={[styles.subtitle, { textAlign: "center" }]}>{isAr ? "لما تنشئ طلبًا حقيقيًا هتقدر تتابع حالته هنا. إنشاء الطلبات غير متصل بالخلفية في النسخة الحالية." : "Real order tracking will appear here once connected. Orders are not connected to the backend in this version."}</AppText>
      <PrimaryButton title={isAr ? "استكشف الأنشطة" : "Explore listings"} onPress={() => router.push("/(tabs)/categories")} />
    </View>
  </Screen>;
}

const styles = StyleSheet.create({
  title: { fontSize: 24, fontWeight: "900" },
  subtitle: { color: colors.muted, fontSize: 12, lineHeight: 18 },
  empty: { alignItems: "center", gap: spacing.md, paddingVertical: 34, marginTop: 12 },
  heading: { fontSize: 16, fontWeight: "800" },
});
