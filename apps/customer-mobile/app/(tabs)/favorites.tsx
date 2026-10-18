import { router } from "expo-router";
import React from "react";
import { Pressable, ScrollView, StyleSheet, View } from "react-native";
import { AppText, Screen, uiStyles } from "../../src/components/ui";
import { listings } from "../../src/data/demo";
import { useAppState } from "../../src/state/AppProvider";
import { colors, spacing } from "../../src/theme/tokens";

export default function FavoritesScreen() {
  const { language, favorites, toggleFavorite } = useAppState();
  const isAr = language === "ar";
  const saved = listings.filter((item) => favorites.includes(item.id));
  return <Screen>
    <View style={{ gap: 5 }}>
      <AppText style={styles.title}>{isAr ? "المفضلة" : "Saved places"}</AppText>
      <AppText style={styles.subtitle}>{isAr ? "احتفظ بالأنشطة اللي تحب ترجع لها" : "Keep the places you want to revisit"}</AppText>
    </View>
    <ScrollView contentContainerStyle={{ gap: spacing.md, paddingBottom: 24 }}>
      {saved.map((item) => <Pressable key={item.id} onPress={() => router.push({ pathname: "/listing/[id]", params: { id: item.id } })} style={uiStyles.card}>
        <View style={[styles.row, { flexDirection: isAr ? "row-reverse" : "row" }]}>
          <View style={styles.icon}><AppText style={{ fontSize: 26 }}>{item.icon}</AppText></View>
          <View style={{ flex: 1, gap: 5 }}>
            <AppText style={styles.name}>{isAr ? item.nameAr : item.nameEn}</AppText>
            <AppText style={styles.subtitle}>{isAr ? item.categoryAr : item.categoryEn} · ★ {item.rating}</AppText>
          </View>
          <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "إزالة من المفضلة" : "Remove from favorites"} hitSlop={8} onPress={(event) => { event.stopPropagation(); toggleFavorite(item.id); }} style={styles.removeButton}><AppText style={{ color: colors.danger, fontSize: 27, lineHeight: 31 }}>♥</AppText></Pressable>
        </View>
      </Pressable>)}
      {saved.length === 0 ? <View style={[uiStyles.card, styles.empty]}>
        <AppText style={{ fontSize: 42 }}>♡</AppText>
        <AppText style={styles.name}>{isAr ? "لسه مفيش عناصر محفوظة" : "Nothing saved yet"}</AppText>
        <AppText style={[styles.subtitle, { textAlign: "center" }]}>{isAr ? "اضغط على القلب في أي نشاط علشان تضيفه هنا." : "Tap the heart on any listing to save it here."}</AppText>
        <Pressable onPress={() => router.push("/(tabs)/categories")} style={styles.cta}><AppText style={{ color: "#FFFFFF", fontWeight: "800" }}>{isAr ? "اكتشف الأقسام" : "Explore categories"}</AppText></Pressable>
      </View> : null}
    </ScrollView>
  </Screen>;
}

const styles = StyleSheet.create({
  title: { fontSize: 24, fontWeight: "900" },
  subtitle: { color: colors.muted, fontSize: 12, lineHeight: 18 },
  row: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  icon: { width: 56, height: 56, borderRadius: 16, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  name: { fontSize: 14, fontWeight: "800" },
  removeButton: { width: 46, height: 46, borderRadius: 23, alignItems: "center", justifyContent: "center", backgroundColor: colors.background, borderWidth: 1, borderColor: colors.border },
  empty: { alignItems: "center", gap: spacing.md, paddingVertical: 32 },
  cta: { backgroundColor: colors.primary, paddingVertical: 12, paddingHorizontal: 18, borderRadius: 12, marginTop: 4 },
});