import React, { createContext, useContext, useMemo, useState } from "react";

export type AppLanguage = "ar" | "en";

type AppContextValue = {
  language: AppLanguage;
  setLanguage: (language: AppLanguage) => void;
  favorites: string[];
  toggleFavorite: (itemId: string) => void;
  cart: Record<string, number>;
  addToCart: (productId: string) => void;
  setCartQuantity: (productId: string, quantity: number) => void;
  removeFromCart: (productId: string) => void;
  clearCart: () => void;
};

const AppContext = createContext<AppContextValue | null>(null);

export function AppProvider({ children }: React.PropsWithChildren) {
  const [language, setLanguage] = useState<AppLanguage>("ar");
  const [favorites, setFavorites] = useState<string[]>([]);
  const [cart, setCart] = useState<Record<string, number>>({});

  const value = useMemo<AppContextValue>(() => ({
    language,
    setLanguage,
    favorites,
    toggleFavorite: (itemId: string) => {
      setFavorites((current) =>
        current.includes(itemId)
          ? current.filter((id) => id !== itemId)
          : [...current, itemId]
      );
    },
    cart,
    addToCart: (productId: string) => setCart((current) => ({ ...current, [productId]: (current[productId] ?? 0) + 1 })),
    setCartQuantity: (productId: string, quantity: number) => setCart((current) => {
      if (quantity <= 0) {
        const next = { ...current };
        delete next[productId];
        return next;
      }
      return { ...current, [productId]: quantity };
    }),
    removeFromCart: (productId: string) => setCart((current) => {
      const next = { ...current };
      delete next[productId];
      return next;
    }),
    clearCart: () => setCart({}),
  }), [language, favorites, cart]);

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useAppState() {
  const value = useContext(AppContext);
  if (!value) throw new Error("useAppState must be used inside AppProvider");
  return value;
}
