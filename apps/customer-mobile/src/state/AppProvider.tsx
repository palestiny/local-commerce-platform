import React, { createContext, useContext, useMemo, useState } from "react";

export type AppLanguage = "ar" | "en";

type AppContextValue = {
  language: AppLanguage;
  setLanguage: (language: AppLanguage) => void;
  favorites: string[];
  toggleFavorite: (listingId: string) => void;
};

const AppContext = createContext<AppContextValue | null>(null);

export function AppProvider({ children }: React.PropsWithChildren) {
  const [language, setLanguage] = useState<AppLanguage>("ar");
  const [favorites, setFavorites] = useState<string[]>([]);

  const value = useMemo<AppContextValue>(() => ({
    language,
    setLanguage,
    favorites,
    toggleFavorite: (listingId: string) => {
      setFavorites((current) =>
        current.includes(listingId)
          ? current.filter((id) => id !== listingId)
          : [...current, listingId]
      );
    },
  }), [language, favorites]);

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useAppState() {
  const value = useContext(AppContext);
  if (!value) throw new Error("useAppState must be used inside AppProvider");
  return value;
}