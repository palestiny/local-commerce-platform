import { router } from "expo-router";
import React, { useMemo, useState } from "react";
import { Alert, Pressable, ScrollView, StyleSheet, TextInput, View } from "react-native";
import { AppText, PrimaryButton, Screen, uiStyles } from "../src/components/ui";
import { listings, products } from "../src/data/demo";
import { useAppState } from "../src/state/AppProvider";
import { colors, radii, spacing } from "../src/theme/tokens";

type PaymentMethod = "card" | "vodafone";

export default function CartScreen() {
  const { language, cart, setCartQuantity, removeFromCart } = useAppState();
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("card");
  const [address, setAddress] = useState("");
  const isAr = language === "ar";
  const cartItems = useMemo(() => products.filter((product) => (cart[product.id] ?? 0) > 0).map((product) => ({ product, quantity: cart[product.id] ?? 0 })), [cart]);
  const total = cartItems.reduce((sum, item) => sum + item.product.price * item.quantity, 0);
  const storeIds = [...new Set(cartItems.map((item) => item.product.storeId))];
  const currency = isAr ? "ج.م" : "EGP";

  const checkout = () => {
    if (!cartItems.length) {
      Alert.alert(isAr ? "السلة فارغة" : "Your cart is empty", isAr ? "أضف منتجات من أحد المتاجر أولًا." : "Add products from a store first.");
      return;
    }
    if (!address.trim()) {
      Alert.alert(isAr ? "عنوان التوصيل مطلوب" : "Delivery address required", isAr ? "اكتب عنوان التوصيل قبل متابعة الطلب." : "Enter a delivery address before continuing.");
      return;
    }
    Alert.alert(
      isAr ? "الدفع غير متصل بعد" : "Payments are not connected yet",
      isAr ? "اختيار وسيلة الدفع محفوظ في الواجهة فقط. لن يتم خصم أي أموال أو إنشاء طلب حقيقي حتى ربط بوابة الدفع والـ API الآمن." : "Payment selection is UI-only. No money will be charged and no real order will be created until a secure payment gateway and API are connected."
    );
  };

  return <Screen style={{ paddingHorizontal: 0, paddingTop: 0 }}>
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled" showsVerticalScrollIndicator={false}>
      <View style={styles.header}>
        <Pressable onPress={() => router.back()} style={styles.back}><AppText style={styles.backText}>‹</AppText></Pressable>
        <AppText style={styles.title}>{isAr ? "سلة التسوق" : "Shopping cart"}</AppText>
        <AppText style={styles.subtitle}>{isAr ? "راجع المنتجات والكميات قبل تأكيد الطلب" : "Review products and quantities before checkout"}</AppText>
      </View>
      <View style={styles.body}>
        {cartItems.map(({ product, quantity }) => {
          const store = listings.find((item) => item.id === product.storeId);
          return <View key={product.id} style={uiStyles.card}>
            <View style={styles.productRow}>
              <View style={styles.productIcon}><AppText style={{ fontSize: 29 }}>{product.icon}</AppText></View>
              <View style={{ flex: 1, gap: 4 }}>
                <AppText style={styles.productName}>{isAr ? product.nameAr : product.nameEn}</AppText>
                <AppText style={styles.muted}>{isAr ? "المتجر: " : "Store: "}{store ? (isAr ? store.nameAr : store.nameEn) : ""}</AppText>
                <AppText style={styles.price}>{(product.price * quantity).toLocaleString(isAr ? "ar-EG" : "en-US")} {currency}</AppText>
              </View>
              <Pressable onPress={() => removeFromCart(product.id)} style={styles.remove}><AppText style={styles.removeText}>×</AppText></Pressable>
            </View>
            <View style={styles.quantityRow}>
              <AppText style={styles.muted}>{isAr ? "الكمية" : "Quantity"}</AppText>
              <View style={styles.quantityControl}>
                <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "زيادة الكمية" : "Increase quantity"} onPress={() => setCartQuantity(product.id, quantity + 1)} style={styles.quantityButton}><AppText style={styles.quantityText}>＋</AppText></Pressable>
                <AppText style={styles.quantityNumber}>{quantity}</AppText>
                <Pressable accessibilityRole="button" accessibilityLabel={isAr ? "تقليل الكمية" : "Decrease quantity"} onPress={() => setCartQuantity(product.id, quantity - 1)} style={styles.quantityButton}><AppText style={styles.quantityText}>−</AppText></Pressable>
              </View>
            </View>
          </View>;
        })}
        {!cartItems.length ? <View style={[uiStyles.card, styles.empty]}><AppText style={{ fontSize: 42 }}>🛒</AppText><AppText style={styles.productName}>{isAr ? "سلتك لسه فاضية" : "Your cart is empty"}</AppText><AppText style={styles.muted}>{isAr ? "ادخل أي متجر واضغط على علامة + بجوار المنتج." : "Open a store and tap + next to a product."}</AppText><PrimaryButton title={isAr ? "اكتشف المتاجر" : "Explore stores"} onPress={() => router.replace("/(tabs)")} /></View> : <>
          {storeIds.length > 1 ? <View style={styles.notice}><AppText style={styles.noticeText}>{isAr ? "السلة تحتوي على منتجات من أكثر من متجر. في النسخة النهائية قد يتم تقسيمها لطلبات منفصلة حسب سياسة التوصيل." : "Your cart has items from multiple stores. Production checkout may split these into separate orders depending on delivery rules."}</AppText></View> : null}
          <View style={uiStyles.card}>
            <AppText style={styles.sectionTitle}>{isAr ? "عنوان التوصيل" : "Delivery address"}</AppText>
            <TextInput value={address} onChangeText={setAddress} placeholder={isAr ? "المدينة، المنطقة، الشارع، رقم المبنى..." : "City, area, street, building..."} placeholderTextColor={colors.muted} multiline style={[styles.addressInput, { textAlign: isAr ? "right" : "left" }]} />
          </View>
          <View style={uiStyles.card}>
            <AppText style={styles.sectionTitle}>{isAr ? "طريقة الدفع" : "Payment method"}</AppText>
            <Pressable onPress={() => setPaymentMethod("card")} style={[styles.paymentOption, paymentMethod === "card" && styles.paymentSelected]}>
              <AppText style={styles.paymentIcon}>💳</AppText><View style={{ flex: 1, gap: 3 }}><AppText style={styles.paymentTitle}>{isAr ? "بطاقة بنكية (Visa / Mastercard)" : "Bank card (Visa / Mastercard)"}</AppText><AppText style={styles.muted}>{isAr ? "الدفع الآمن بعد تفعيل بوابة الدفع" : "Secure payment when gateway is enabled"}</AppText></View><AppText style={styles.radio}>{paymentMethod === "card" ? "●" : "○"}</AppText>
            </Pressable>
            <Pressable onPress={() => setPaymentMethod("vodafone")} style={[styles.paymentOption, paymentMethod === "vodafone" && styles.paymentSelected]}>
              <AppText style={styles.paymentIcon}>📱</AppText><View style={{ flex: 1, gap: 3 }}><AppText style={styles.paymentTitle}>{isAr ? "فودافون كاش" : "Vodafone Cash"}</AppText><AppText style={styles.muted}>{isAr ? "سيتم تأكيد طريقة التحويل بعد الربط الرسمي" : "Transfer instructions require official integration"}</AppText></View><AppText style={styles.radio}>{paymentMethod === "vodafone" ? "●" : "○"}</AppText>
            </Pressable>
          </View>
          <View style={uiStyles.card}>
            <View style={styles.totalRow}><AppText style={styles.muted}>{isAr ? "عدد القطع" : "Items"}</AppText><AppText style={styles.totalValue}>{cartItems.reduce((sum, item) => sum + item.quantity, 0)}</AppText></View>
            <View style={styles.totalRow}><AppText style={styles.muted}>{isAr ? "الإجمالي قبل التوصيل" : "Subtotal (before delivery)"}</AppText><AppText style={styles.totalValue}>{total.toLocaleString(isAr ? "ar-EG" : "en-US")} {currency}</AppText></View>
            <AppText style={styles.muted}>{isAr ? "رسوم التوصيل والضرائب النهائية تظهر بعد ربط المتجر والعنوان." : "Final delivery fees and taxes will be calculated after store and address integration."}</AppText>
          </View>
          <PrimaryButton title={isAr ? "متابعة لإتمام الطلب" : "Continue to checkout"} onPress={checkout} />
          <AppText style={styles.disclaimer}>{isAr ? "المنتجات والأسعار تجريبية. لن يتم تنفيذ دفع حقيقي في هذه النسخة." : "Products and prices are demo data. No real payment is processed in this prototype."}</AppText>
        </>}
      </View>
    </ScrollView>
  </Screen>;
}

const styles = StyleSheet.create({
  content: { paddingBottom: 32, gap: spacing.lg },
  header: { backgroundColor: colors.primary, paddingTop: 20, paddingHorizontal: spacing.lg, paddingBottom: spacing.xl, borderBottomLeftRadius: 26, borderBottomRightRadius: 26, gap: spacing.sm },
  back: { width: 42, height: 42, borderRadius: 21, backgroundColor: "#FFFFFF22", alignItems: "center", justifyContent: "center" },
  backText: { color: "#FFFFFF", fontSize: 28 },
  title: { color: "#FFFFFF", fontSize: 24, fontWeight: "900" },
  subtitle: { color: "#D9F0EB", fontSize: 12, lineHeight: 18 },
  body: { paddingHorizontal: spacing.lg, gap: spacing.md },
  productRow: { flexDirection: "row", alignItems: "center", gap: spacing.md },
  productIcon: { width: 60, height: 60, borderRadius: 16, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  productName: { fontSize: 14, fontWeight: "900" },
  muted: { color: colors.muted, fontSize: 11, lineHeight: 17 },
  price: { color: colors.primaryDark, fontSize: 13, fontWeight: "900" },
  remove: { width: 34, height: 34, borderRadius: 17, backgroundColor: "#FCECEC", alignItems: "center", justifyContent: "center" },
  removeText: { color: colors.danger, fontSize: 25, lineHeight: 28 },
  quantityRow: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", marginTop: spacing.md },
  quantityControl: { flexDirection: "row", alignItems: "center", gap: 14 },
  quantityButton: { width: 34, height: 34, borderRadius: 12, backgroundColor: colors.primarySoft, alignItems: "center", justifyContent: "center" },
  quantityText: { color: colors.primaryDark, fontSize: 20, fontWeight: "900" },
  quantityNumber: { fontSize: 15, fontWeight: "900" },
  empty: { alignItems: "center", gap: spacing.md, paddingVertical: 30 },
  notice: { backgroundColor: "#FFF4E4", padding: spacing.md, borderRadius: radii.md },
  noticeText: { color: "#79521F", fontSize: 11, lineHeight: 17 },
  sectionTitle: { fontSize: 15, fontWeight: "900", marginBottom: spacing.sm },
  addressInput: { minHeight: 84, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radii.md, color: colors.text, textAlignVertical: "top" },
  paymentOption: { flexDirection: "row", alignItems: "center", gap: spacing.md, padding: spacing.md, borderRadius: radii.md, borderWidth: 1, borderColor: colors.border, marginTop: spacing.sm },
  paymentSelected: { borderColor: colors.primary, backgroundColor: colors.primarySoft },
  paymentIcon: { fontSize: 24 },
  paymentTitle: { fontSize: 12, fontWeight: "900" },
  radio: { color: colors.primary, fontSize: 20, fontWeight: "900" },
  totalRow: { flexDirection: "row", justifyContent: "space-between", alignItems: "center", gap: spacing.md, marginBottom: spacing.sm },
  totalValue: { fontSize: 14, fontWeight: "900" },
  disclaimer: { color: colors.muted, fontSize: 10, lineHeight: 16, textAlign: "center" },
});
