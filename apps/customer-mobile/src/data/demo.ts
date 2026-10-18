export type Category = {
  id: string;
  nameAr: string;
  nameEn: string;
  icon: string;
  descriptionAr: string;
  descriptionEn: string;
};

export type LocalListing = {
  id: string;
  nameAr: string;
  nameEn: string;
  categoryId: string;
  categoryAr: string;
  categoryEn: string;
  rating: string;
  reviews: number;
  distanceAr: string;
  distanceEn: string;
  priceAr: string;
  priceEn: string;
  icon: string;
  featured?: boolean;
};

export const categories: Category[] = [
  { id: "food", nameAr: "مطاعم وأكل", nameEn: "Food & restaurants", icon: "🍲", descriptionAr: "وجبات ومطاعم قريبة منك", descriptionEn: "Meals and nearby restaurants" },
  { id: "groceries", nameAr: "بقالة وسوبر ماركت", nameEn: "Groceries", icon: "🛒", descriptionAr: "احتياجات البيت اليومية", descriptionEn: "Everyday household essentials" },
  { id: "home", nameAr: "صيانة المنزل", nameEn: "Home services", icon: "🧰", descriptionAr: "خدمات وإصلاحات منزلية", descriptionEn: "Home repairs and services" },
  { id: "health", nameAr: "صحة ورعاية", nameEn: "Health & care", icon: "🩺", descriptionAr: "خدمات الرعاية والصحة", descriptionEn: "Care and health services" },
  { id: "education", nameAr: "تعليم ودروس", nameEn: "Education", icon: "📚", descriptionAr: "تعلم ودروس ومراكز", descriptionEn: "Lessons and learning centers" },
  { id: "shopping", nameAr: "ملابس وتسوق", nameEn: "Shopping", icon: "🛍️", descriptionAr: "متاجر ومنتجات متنوعة", descriptionEn: "Shops and everyday products" },
  { id: "transport", nameAr: "نقل ومشاوير", nameEn: "Transport", icon: "🚗", descriptionAr: "تنقلات وخدمات توصيل", descriptionEn: "Rides and delivery services" },
  { id: "beauty", nameAr: "جمال وعناية", nameEn: "Beauty & care", icon: "✨", descriptionAr: "صالونات وخدمات شخصية", descriptionEn: "Salons and personal care" },
  { id: "bakery", nameAr: "مخابز وحلويات", nameEn: "Bakeries & sweets", icon: "🥐", descriptionAr: "مخبوزات وحلويات طازجة", descriptionEn: "Fresh bakery and sweets" },
  { id: "produce", nameAr: "خضار وفاكهة", nameEn: "Fruit & vegetables", icon: "🥬", descriptionAr: "خضروات وفاكهة واحتياجات طازجة", descriptionEn: "Fresh produce and essentials" },
  { id: "electronics", nameAr: "إلكترونيات وموبايلات", nameEn: "Electronics & phones", icon: "📱", descriptionAr: "أجهزة وموبايلات وإكسسوارات", descriptionEn: "Devices, phones, and accessories" },
  { id: "automotive", nameAr: "سيارات وورش", nameEn: "Auto & workshops", icon: "🚗", descriptionAr: "صيانة وقطع غيار وخدمات سيارات", descriptionEn: "Vehicle repair and parts" },
  { id: "household", nameAr: "أدوات منزلية", nameEn: "Home & kitchen", icon: "🏠", descriptionAr: "أدوات ومستلزمات المنزل", descriptionEn: "Home and kitchen essentials" },
  { id: "professional", nameAr: "خدمات مهنية", nameEn: "Professional services", icon: "🧾", descriptionAr: "خدمات مهنية وأعمال متخصصة", descriptionEn: "Professional and specialist services" },
  { id: "sports", nameAr: "رياضة ولياقة", nameEn: "Sports & fitness", icon: "🏃", descriptionAr: "أنشطة رياضية ولياقة بدنية", descriptionEn: "Sports and fitness activities" },
];

export const listings: LocalListing[] = [
  { id: "l1", nameAr: "مطعم البيت البلدي", nameEn: "El Beit El Balady", categoryId: "food", categoryAr: "مطاعم", categoryEn: "Restaurant", rating: "4.8", reviews: 126, distanceAr: "وسط دكرنس", distanceEn: "Central Dekernes", priceAr: "تفاصيل الأسعار داخل المتجر", priceEn: "View menu for prices", icon: "🍽️", featured: true },
  { id: "l2", nameAr: "ماركت الأسرة", nameEn: "Family Market", categoryId: "groceries", categoryAr: "بقالة", categoryEn: "Groceries", rating: "4.7", reviews: 84, distanceAr: "قريب منك", distanceEn: "Near you", priceAr: "منتجات يومية", priceEn: "Daily essentials", icon: "🛒", featured: true },
  { id: "l3", nameAr: "خدمات البيت", nameEn: "HomeCare Services", categoryId: "home", categoryAr: "صيانة منزلية", categoryEn: "Home services", rating: "4.9", reviews: 42, distanceAr: "دكرنس", distanceEn: "Dekernes", priceAr: "السعر حسب الخدمة", priceEn: "Price depends on service", icon: "🧰", featured: true },
  { id: "l4", nameAr: "مركز خطوة للتعليم", nameEn: "Khotwa Learning Center", categoryId: "education", categoryAr: "تعليم", categoryEn: "Education", rating: "4.6", reviews: 58, distanceAr: "دكرنس", distanceEn: "Dekernes", priceAr: "حسب البرنامج", priceEn: "Varies by program", icon: "📚" },
  { id: "l5", nameAr: "صيدلية العافية", nameEn: "Al Afia Pharmacy", categoryId: "health", categoryAr: "صحة", categoryEn: "Health", rating: "4.8", reviews: 93, distanceAr: "قريب منك", distanceEn: "Near you", priceAr: "تواصل لمعرفة التوافر", priceEn: "Contact for availability", icon: "🩺" },
  { id: "l6", nameAr: "أناقة للملابس", nameEn: "Anaqa Fashion", categoryId: "shopping", categoryAr: "ملابس", categoryEn: "Fashion", rating: "4.5", reviews: 37, distanceAr: "دكرنس", distanceEn: "Dekernes", priceAr: "تشكيلات متنوعة", priceEn: "Various collections", icon: "🛍️" },
];

export const demoNotice = {
  ar: "بيانات تجريبية للعرض فقط — الأنشطة والأسعار والتقييمات ليست بيانات حقيقية مؤكدة.",
  en: "Demo content only — listings, prices, and ratings are not verified real-world data.",
};