import { router } from "expo-router";
import React from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, PrimaryButton, Screen, uiStyles } from "../src/components/ui";
import { colors, spacing } from "../src/theme/tokens";
import { useAppState } from "../src/state/AppProvider";

export default function MessagesScreen() {
  const { language } = useAppState();
  const isAr = language === "ar";
  return <Screen>
    <View style={styles.heading}>
      <Pressable onPress={() => router.back()} style={styles.back}><AppText style={styles.backText}>‹</AppText></Pressable>
      <AppText style={styles.title}>{isAr ? "المحادثات" : "Conversations"}</AppText>
      <AppText style={styles.subtitle}>{isAr ? "كل رسائلك مع المتاجر في مكان واحد" : "Your conversations with stores in one place"}</AppText>
    </View>
    <ScrollView contentContainerStyle={styles.content}>
      <View style={[uiStyles.card, styles.empty]}>
        <AppText style={{ fontSize: 42 }}>💬</AppText>
        <AppText style={styles.emptyTitle}>{isAr ? "لسه مفيش محادثات" : "No conversations yet"}</AppText>
        <AppText style={styles.subtitle}>{isAr ? "عند ربط نظام الرسائل، هتقدر تسأل المتجر عن المنتج أو الطلب وتتابع الردود هنا." : "Once messaging is connected, ask stores about products and orders and follow replies here."}</AppText>
        <PrimaryButton title={isAr ? "اكتشف المتاجر" : "Explore stores"} onPress={() => router.replace("/(tabs)/categories")} />
      </View>
      <AppText style={styles.note}>{isAr ? "المحادثات الفعلية تحتاج تسجيل دخول وخدمة رسائل آمنة." : "Live conversations require sign-in and a secure messaging service."}</AppText>
    </ScrollView>
  </Screen>;
}
const styles = StyleSheet.create({
  heading: { gap: spacing.sm },
  back: { width: 42, height: 42, borderRadius: 21, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  backText: { color: colors.primary, fontSize: 28 },
  title: { fontSize: 24, fontWeight: "900" },
  subtitle: { color: colors.muted, fontSize: 12, lineHeight: 18 },
  content: { paddingBottom: 28, gap: spacing.md },
  empty: { alignItems: "center", gap: spacing.md, paddingVertical: 32 },
  emptyTitle: { fontSize: 16, fontWeight: "900" },
  note: { color: colors.muted, fontSize: 10, textAlign: "center" },
});