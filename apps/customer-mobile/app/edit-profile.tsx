import { router } from "expo-router";
import React, { useState } from "react";
import { Alert, Pressable, ScrollView, StyleSheet, TextInput, View } from "react-native";
import { AppText, PrimaryButton, Screen } from "../src/components/ui";
import { colors, radii, spacing } from "../src/theme/tokens";
import { useAppState } from "../src/state/AppProvider";

export default function EditProfileScreen() {
  const { language } = useAppState();
  const isAr = language === "ar";
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [address, setAddress] = useState("");
  const save = () => Alert.alert(isAr ? "الحفظ غير متصل" : "Saving is not connected", isAr ? "البيانات دي محلية للمعاينة فقط. حفظ الملف الشخصي الحقيقي يحتاج حسابًا مسجلًا وAPI." : "These fields are preview-only. Saving a real profile requires an authenticated account and API.");
  return <Screen>
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <Pressable onPress={() => router.back()} style={styles.back}><AppText style={styles.backText}>‹</AppText></Pressable>
      <AppText style={styles.title}>{isAr ? "تعديل الملف الشخصي" : "Edit profile"}</AppText>
      <AppText style={styles.subtitle}>{isAr ? "حدّث بياناتك الأساسية وعنوان التوصيل." : "Update your basic details and delivery address."}</AppText>
      <Field label={isAr ? "الاسم الكامل" : "Full name"} value={name} onChangeText={setName} placeholder={isAr ? "اكتب اسمك" : "Your name"} isAr={isAr} />
      <Field label={isAr ? "رقم الهاتف" : "Phone number"} value={phone} onChangeText={setPhone} placeholder="01xxxxxxxxx" isAr={isAr} keyboardType="phone-pad" />
      <Field label={isAr ? "عنوان التوصيل الافتراضي" : "Default delivery address"} value={address} onChangeText={setAddress} placeholder={isAr ? "المدينة والمنطقة والشارع" : "City, area, street"} isAr={isAr} multiline />
      <PrimaryButton title={isAr ? "حفظ التغييرات" : "Save changes"} onPress={save} />
      <AppText style={styles.note}>{isAr ? "لن يتم حفظ البيانات أو إرسالها في هذه النسخة التجريبية." : "This prototype will not save or submit your information."}</AppText>
    </ScrollView>
  </Screen>;
}
function Field({ label, value, onChangeText, placeholder, isAr, keyboardType, multiline }: { label: string; value: string; onChangeText: (value: string) => void; placeholder: string; isAr: boolean; keyboardType?: "default" | "phone-pad"; multiline?: boolean }) {
  return <View style={styles.field}><AppText style={styles.label}>{label}</AppText><TextInput value={value} onChangeText={onChangeText} placeholder={placeholder} placeholderTextColor={colors.muted} keyboardType={keyboardType} multiline={multiline} style={[styles.input, { textAlign: isAr ? "right" : "left" }, multiline && styles.multiline]} /></View>;
}
const styles = StyleSheet.create({
  content: { paddingBottom: 30, gap: spacing.lg },
  back: { width: 42, height: 42, borderRadius: 21, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  backText: { color: colors.primary, fontSize: 28 },
  title: { fontSize: 24, fontWeight: "900" },
  subtitle: { color: colors.muted, fontSize: 12, lineHeight: 18 },
  field: { gap: 7 },
  label: { fontSize: 12, fontWeight: "800" },
  input: { minHeight: 50, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radii.md, color: colors.text, backgroundColor: colors.surface },
  multiline: { minHeight: 90, paddingTop: spacing.md, textAlignVertical: "top" },
  note: { color: colors.muted, fontSize: 10, lineHeight: 16, textAlign: "center" },
});