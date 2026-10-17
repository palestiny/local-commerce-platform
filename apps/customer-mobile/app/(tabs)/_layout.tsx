import { Tabs } from "expo-router";
import { Text, type ColorValue } from "react-native";
import { useAppState } from "../../src/state/AppProvider";
import { colors } from "../../src/theme/tokens";

export default function TabLayout() {
  const { language } = useAppState();
  const labels = language === "ar"
    ? { home: "الرئيسية", categories: "الأقسام", favorites: "المفضلة", orders: "طلباتي" }
    : { home: "Home", categories: "Categories", favorites: "Saved", orders: "My orders" };

  return (
    <Tabs
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.muted,
        tabBarStyle: { backgroundColor: colors.surface, borderTopColor: colors.border, height: 66, paddingTop: 8, paddingBottom: 8 },
        tabBarLabelStyle: { fontSize: 11, fontWeight: "600" },
      }}
    >
      <Tabs.Screen name="index" options={{ title: labels.home, tabBarIcon: ({ color }) => <TabGlyph glyph="⌂" color={color} /> }} />
      <Tabs.Screen name="categories" options={{ title: labels.categories, tabBarIcon: ({ color }) => <TabGlyph glyph="▦" color={color} /> }} />
      <Tabs.Screen name="favorites" options={{ title: labels.favorites, tabBarIcon: ({ color }) => <TabGlyph glyph="♡" color={color} /> }} />
      <Tabs.Screen name="orders" options={{ title: labels.orders, tabBarIcon: ({ color }) => <TabGlyph glyph="▤" color={color} /> }} />
    </Tabs>
  );
}

function TabGlyph({ glyph, color }: { glyph: string; color: ColorValue }) {
  return <Text style={{ color, fontSize: 20, lineHeight: 23 }}>{glyph}</Text>;
}
