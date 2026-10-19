import { router } from "expo-router";
import React, { useState } from "react";
import { Pressable, ScrollView, StyleSheet, Switch, View } from "react-native";
import { AppText, Screen } from "../src/components/ui";
import { colors, spacing } from "../src/theme/tokens";
import { useAppState } from "../src/state/AppProvider";

export default function SettingsScreen() {
  const { language, setLanguage } = useAppState();
  const isAr = language === "ar";
  const [marketing, setMarketing] = useState(false);
  const [orderUpdates, setOrderUpdates] = useState(true);
  const [messages, setMessages] = useState(true);
  return <Screen>
    <ScrollView contentContainerStyle={styles.content}>
      <Pressable onPress={() => router.back()} style={styles.back}><AppText style={styles.backText}>‹</AppText></Pressable>
      <AppText style={styles.title}>{isAr ? "الإعدادات" : "Settings"}</AppText>
      <AppText style={styles.subtitle}>{isAr ? "خصص تجربة بلدنا حسب تفضيلاتك." : "Customize your Baladna experience."}</AppText>
      <View style={styles.group}>
        <AppText style={styles.groupTitle}>{isAr ? "اللغة" : "Language"}</AppText>
        <Pressable onPress={() => setLanguage(isAr ? "en" : "ar")} style={styles.row}><AppText style={styles.rowTitle}>{isAr ? "العربية" : "English"}</AppText><AppText style={styles.value}>{isAr ? "التبديل إلى English" : "Switch to العربية"} ⇄</AppText></Pressable>
      </View>
      <View style={styles.group}>
        <AppText style={styles.groupTitle}>{isAr ? "الإشعارات" : "Notifications"}</AppText>
        <ToggleRow title={isAr ? "تحديثات الطلبات" : "Order updates"} value={orderUpdates} onChange={setOrderUpdates} />
        <ToggleRow title={isAr ? "الرسائل والمحادثات" : "Messages and conversations"} value={messages} onChange={setMessages} />
        <ToggleRow title={isAr ? "العروض والتسويق" : "Offers and marketing"} value={marketing} onChange={setMarketing} />
        <Pressable onPress={() => router.push("/notifications")} style={styles.row}><AppText style={styles.rowTitle}>{isAr ? "فتح مركز الإشعارات" : "Open notification center"}</AppText><AppText style={styles.value}>‹</AppText></Pressable>
      </View>
      <AppText style={styles.note}>{isAr ? "تفضيلات الإشعارات محفوظة داخل هذه الشاشة فقط حاليًا؛ ربطها بالخدمة الفعلية قادم مع إعداد الإشعارات." : "These preferences are currently local to this screen; they will connect to the notification service later."}</AppText>
    </ScrollView>
  </Screen>;
}
function ToggleRow({ title, value, onChange }: { title: string; value: boolean; onChange: (value: boolean) => void }) {
  return <View style={styles.row}><AppText style={styles.rowTitle}>{title}</AppText><Switch value={value} onValueChange={onChange} trackColor={{ false: colors.border, true: colors.primary }} thumbColor="#FFFFFF" /></View>;
}
const styles = StyleSheet.create({
  content: { paddingBottom: 28, gap: spacing.lg },
  back: { width: 42, height: 42, borderRadius: 21, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  backText: { color: colors.primary, fontSize: 28 },
  title: { fontSize: 24, fontWeight: "900" },
  subtitle: { color: colors.muted, fontSize: 12, lineHeight: 18 },
  group: { backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border, borderRadius: 16, padding: spacing.md, gap: spacing.md },
  groupTitle: { fontSize: 15, fontWeight: "900" },
  row: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", gap: spacing.md, minHeight: 44 },
  rowTitle: { flex: 1, fontSize: 12, fontWeight: "800" },
  value: { color: colors.primary, fontSize: 11, fontWeight: "800" },
  note: { color: colors.muted, fontSize: 10, lineHeight: 16, textAlign: "center" },
});