import { router } from "expo-router";
import React, { useState } from "react";
import { Alert, Pressable, ScrollView, StyleSheet, TextInput, View } from "react-native";
import { AppText, PrimaryButton, Screen } from "../src/components/ui";
import { colors, radii, spacing } from "../src/theme/tokens";
import { useAppState } from "../src/state/AppProvider";

export default function AuthScreen() {
  const { language } = useAppState();
  const isAr = language === "ar";
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const notifyNotConnected = () => Alert.alert(
    isAr ? "تسجيل الدخول لم يُفعّل بعد" : "Authentication is not enabled yet",
    isAr ? "الواجهة جاهزة للتصميم، لكن تسجيل الدخول الحقيقي يتطلب إعداد مزود الهوية وربطه بالخلفية والتحقق من الجلسات." : "This is the UI prototype. Real sign-in requires an identity provider, backend integration, and secure session handling."
  );
  return <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <View style={styles.header}>
        <Pressable onPress={() => router.back()} style={styles.back}><AppText style={styles.backText}>‹</AppText></Pressable>
        <View style={styles.logo}><AppText style={{ fontSize: 34 }}>♙</AppText></View>
        <AppText style={styles.title}>{isAr ? "أهلاً بيك في بلدنا" : "Welcome to Baladna"}</AppText>
        <AppText style={styles.subtitle}>{isAr ? "سجّل دخولك لمتابعة طلباتك ومفضلاتك ومحادثاتك." : "Sign in to manage orders, favorites, and conversations."}</AppText>
      </View>
      <View style={styles.form}>
        <Pressable accessibilityRole="button" onPress={notifyNotConnected} style={styles.googleButton}><AppText style={styles.googleMark}>G</AppText><AppText style={styles.googleText}>{isAr ? "المتابعة باستخدام Google" : "Continue with Google"}</AppText></Pressable>
        <View style={styles.separator}><View style={styles.line} /><AppText style={styles.or}>{isAr ? "أو بالبريد الإلكتروني" : "or with email"}</AppText><View style={styles.line} /></View>
        <View style={styles.field}><AppText style={styles.label}>{isAr ? "البريد الإلكتروني" : "Email address"}</AppText><TextInput value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" autoComplete="email" placeholder="name@example.com" placeholderTextColor={colors.muted} style={styles.input} /></View>
        <View style={styles.field}><AppText style={styles.label}>{isAr ? "كلمة المرور" : "Password"}</AppText><TextInput value={password} onChangeText={setPassword} secureTextEntry autoComplete="password" placeholder={isAr ? "أدخل كلمة المرور" : "Enter your password"} placeholderTextColor={colors.muted} style={styles.input} /></View>
        <PrimaryButton title={isAr ? "تسجيل الدخول" : "Sign in"} onPress={notifyNotConnected} />
        <Pressable onPress={notifyNotConnected} style={styles.secondary}><AppText style={styles.secondaryText}>{isAr ? "إنشاء حساب جديد" : "Create a new account"}</AppText></Pressable>
        <AppText style={styles.disclaimer}>{isAr ? "تسجيل الدخول غير متصل بخدمة هوية حتى الآن؛ لن يتم إنشاء حساب أو إرسال بيانات." : "Authentication is not connected yet; no account will be created and no credentials will be sent."}</AppText>
      </View>
    </ScrollView>
  </Screen>;
}

const styles = StyleSheet.create({
  content: { paddingBottom: 32, gap: spacing.lg },
  header: { backgroundColor: colors.primary, padding: spacing.lg, paddingTop: 20, paddingBottom: 30, borderBottomLeftRadius: 28, borderBottomRightRadius: 28, gap: spacing.md, alignItems: "center" },
  back: { alignSelf: "flex-start", width: 42, height: 42, borderRadius: 21, backgroundColor: "#FFFFFF22", alignItems: "center", justifyContent: "center" },
  backText: { color: "#FFFFFF", fontSize: 28 },
  logo: { width: 68, height: 68, borderRadius: 22, backgroundColor: "#FFFFFF22", alignItems: "center", justifyContent: "center" },
  title: { color: "#FFFFFF", fontSize: 23, fontWeight: "900", textAlign: "center" },
  subtitle: { color: "#D9F0EB", fontSize: 12, lineHeight: 19, textAlign: "center" },
  form: { paddingHorizontal: spacing.lg, gap: spacing.md },
  googleButton: { minHeight: 52, flexDirection: "row", alignItems: "center", justifyContent: "center", gap: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radii.md, backgroundColor: colors.surface },
  googleMark: { color: "#4285F4", fontSize: 21, fontWeight: "900" },
  googleText: { fontSize: 13, fontWeight: "900" },
  separator: { flexDirection: "row", alignItems: "center", gap: spacing.sm },
  line: { flex: 1, height: 1, backgroundColor: colors.border },
  or: { color: colors.muted, fontSize: 11 },
  field: { gap: 7 },
  label: { fontSize: 12, fontWeight: "800" },
  input: { minHeight: 50, borderWidth: 1, borderColor: colors.border, borderRadius: radii.md, backgroundColor: colors.surface, paddingHorizontal: spacing.md, color: colors.text },
  secondary: { padding: spacing.md, alignItems: "center" },
  secondaryText: { color: colors.primary, fontWeight: "900" },
  disclaimer: { color: colors.muted, fontSize: 10, lineHeight: 16, textAlign: "center" },
});
