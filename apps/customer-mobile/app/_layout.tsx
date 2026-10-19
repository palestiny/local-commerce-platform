import { Stack } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { AppProvider } from "../src/state/AppProvider";
import { colors } from "../src/theme/tokens";

export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <AppProvider>
        <StatusBar style="dark" />
        <Stack
          screenOptions={{
            headerShown: false,
            contentStyle: { backgroundColor: colors.background },
          }}
        >
          <Stack.Screen name="(tabs)" />
          <Stack.Screen name="profile" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="listing/[id]" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="category/[id]" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="store/[id]" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="cart" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="auth" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="notifications" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="edit-profile" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="subscriptions" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="messages" options={{ animation: "slide_from_right" }} />
          <Stack.Screen name="settings" options={{ animation: "slide_from_right" }} />
        </Stack>
      </AppProvider>
    </SafeAreaProvider>
  );
}
