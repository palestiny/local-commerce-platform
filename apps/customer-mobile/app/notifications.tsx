import { router } from "expo-router";
import React, { useState } from "react";
import { Pressable, ScrollView, StyleSheet, Switch, View } from "react-native";
import { AppText, Screen, uiStyles } from "../src/components/ui";
import { colors, radii, spacing } from "../src/theme/tokens";
import { useAppState } from "../src/state/AppProvider";

export default function NotificationsScreen() {
  const { language } = useAppState();
  const isAr = language === "ar";
  const [ordersEnabled, setOrdersEnabled] = useState(true);
  const [messagesEnabled, setMessagesEnabled] = useState(true);
  const [offersEnabled, setOffersEnabled] = useState(false);
  const notifications = isAr ? [
    { icon: "🛍️", title: "مرحبًا بك في بلدنا", body: "هنا هتلاقي تحديثات الطلبات والرسائل والعروض المهمة.", time: "رسالة ترحيبية تجريبية" },
    { icon: "📦", title: "تحديثات الطلبات", body: "عند ربط الطلبات، هتظهر هنا حالة التأكيد والتجهيز والتوصيل.", time: "معاينة" },
    { icon: "💬", title: "رسائل المتاجر", body: "هتقدر تتابع رسائلك مع المتاجر من مركز الإشعارات.", time: "معاينة" },
  ] : [
    { icon: "🛍️", title: "Welcome to Baladna", body: "Order updates, messages, and important offers will appear here.", time: "Demo welcome message" },
    { icon: "📦", title: "Order updates", body: "Order confirmation, preparation, and delivery updates will appear here after integration.", time: "Preview" },
    { icon: "💬", title: "Store messages", body: "You will be able to follow conversations with stores from notifications.", time: "Preview" },
  ];
  return <Screen>
    <View style={styles.heading}>
      <Pressable onPress={() => router.back()} style={styles.back}><AppText style={styles.backText}>‹</AppText></Pressable>
      <AppText style={styles.title}>{isAr ? "الإشعارات" : "Notifications"}</AppText>
      <AppText style={styles.subtitle}>{isAr ? "كل تحديثات حسابك في مكان واحد" : "Your account updates in one place"}</AppText>
    </View>
    <ScrollView contentContainerStyle={styles.content} showsVerticalScrollIndicator={false}>
      <View style={styles.notice}><AppText style={styles.noticeText}>{isAr ? "دي معاينة لمركز الإشعارات. الإشعارات الفورية على الهاتف تحتاج إعداد خدمة الإشعارات وربطها بالخلفية." : "This is a notification-center preview. Push notifications require a configured notification service and backend integration."}</AppText></View>
      <View style={styles.section}>
        <AppText style={styles.sectionTitle}>{isAr ? "تفضيلات الإشعارات" : "Notification preferences"}</AppText>
        <PreferenceRow icon="📦" title={isAr ? "الطلبات والتوصيل" : "Orders and delivery"} enabled={ordersEnabled} onToggle={setOrdersEnabled} />
        <PreferenceRow icon="💬" title={isAr ? "الرسائل والمحادثات" : "Messages and chats"} enabled={messagesEnabled} onToggle={setMessagesEnabled} />
        <PreferenceRow icon="🏷️" title={isAr ? "العروض والخصومات" : "Offers and discounts"} enabled={offersEnabled} onToggle={setOffersEnabled} />
      </View>
      <View style={styles.section}>
        <AppText style={styles.sectionTitle}>{isAr ? "آخر التنبيهات" : "Recent notifications"}</AppText>
        {notifications.map((item, index) => <View key={item.title} style={uiStyles.card}><View style={styles.notificationRow}><View style={styles.notificationIcon}><AppText style={{ fontSize: 24 }}>{item.icon}</AppText></View><View style={{ flex: 1, gap: 5 }}><AppText style={styles.notificationTitle}>{item.title}</AppText><AppText style={styles.notificationBody}>{item.body}</AppText><AppText style={styles.time}>{item.time}</AppText></View>{index === 0 ? <View style={styles.unread} /> : null}</View></View>)}
      </View>
    </ScrollView>
  </Screen>;
}

function PreferenceRow({ icon, title, enabled, onToggle }: { icon: string; title: string; enabled: boolean; onToggle: (value: boolean) => void }) {
  return <View style={styles.preferenceRow}><AppText style={styles.preferenceIcon}>{icon}</AppText><AppText style={styles.preferenceTitle}>{title}</AppText><Switch value={enabled} onValueChange={onToggle} trackColor={{ false: colors.border, true: colors.primary }} thumbColor="#FFFFFF" /></View>;
}

const styles = StyleSheet.create({
  heading: { gap: spacing.sm },
  back: { width: 42, height: 42, borderRadius: 21, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  backText: { color: colors.primary, fontSize: 28 },
  title: { fontSize: 24, fontWeight: "900" },
  subtitle: { color: colors.muted, fontSize: 12 },
  content: { paddingBottom: 28, gap: spacing.lg },
  notice: { padding: spacing.md, backgroundColor: "#FFF4E4", borderRadius: radii.md },
  noticeText: { color: "#79521F", fontSize: 11, lineHeight: 17 },
  section: { gap: spacing.md },
  sectionTitle: { fontSize: 17, fontWeight: "900" },
  preferenceRow: { flexDirection: "row", alignItems: "center", gap: spacing.md, backgroundColor: colors.surface, borderColor: colors.border, borderWidth: 1, borderRadius: radii.md, padding: spacing.md },
  preferenceIcon: { fontSize: 22 },
  preferenceTitle: { flex: 1, fontSize: 12, fontWeight: "800" },
  notificationRow: { flexDirection: "row", alignItems: "flex-start", gap: spacing.md },
  notificationIcon: { width: 46, height: 46, borderRadius: 15, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  notificationTitle: { fontSize: 13, fontWeight: "900" },
  notificationBody: { color: colors.muted, fontSize: 11, lineHeight: 17 },
  time: { color: colors.muted, fontSize: 10 },
  unread: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.primary, marginTop: 4 },
});
